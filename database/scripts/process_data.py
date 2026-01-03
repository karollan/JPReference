#!/usr/bin/env python3
"""
Memory-Optimized Data Processor for JLPT Reference Database

Optimizations:
1. Stream JSON files using ijson instead of loading entire files
2. Commit periodically (every BATCH_SIZE rows) to avoid memory buildup
3. Batch inserts using executemany() where appropriate
"""

import os
import sys
import time
import psycopg2
from pathlib import Path
import ijson

# Batch size for commits and bulk inserts
BATCH_SIZE = 500

def safe_print(text):
    """Safely print text that may contain Unicode characters."""
    try:
        print(text)
    except UnicodeEncodeError:
        print(str(text).encode('ascii', 'replace').decode('ascii'))

class JLPTDataProcessor:
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.source_dir = self.project_root / "source"
        
        # Database connection parameters
        self.db_params = {
            'host': os.getenv('POSTGRES_HOST', 'localhost'),
            'port': os.getenv('POSTGRES_PORT', '5432'),
            'database': os.getenv('POSTGRES_DB', 'jlptreference'),
            'user': os.getenv('POSTGRES_USER', 'jlptuser'),
            'password': os.getenv('POSTGRES_PASSWORD', 'jlptpassword')
        }
        
        # JLPT level mappings
        self.kanji_jlpt_mapping = {}
        self.vocabulary_jlpt_mapping = {}
        
        # Caches for relationships
        self.kanji_cache = {}  # character -> kanji_id
        self.radical_cache = {}  # literal -> radical_id
        self.tag_cache = {}  # code -> tag exists
        self.vocabulary_cache = {}  # jmdict_id -> vocabulary_id
        
        # Statistics
        self.processed_count = 0
        self.commit_count = 0

        # Vocab relations
        self.pending_vocab_relations = []
        self.pending_proper_noun_relations = []

        self.vocab_term_cache = {}
        self.proper_noun_term_cache = {}

    def wait_for_database(self, max_retries=30, retry_delay=2):
        """Wait for the database to be ready."""
        print("Waiting for database to be ready...")
        
        for attempt in range(max_retries):
            try:
                conn = psycopg2.connect(**self.db_params)
                conn.close()
                print("Database is ready!")
                return True
            except psycopg2.OperationalError as e:
                print(f"Attempt {attempt + 1}/{max_retries}: Database not ready yet - {e}")
                time.sleep(retry_delay)
        
        print("Failed to connect to database after maximum retries")
        return False

    def periodic_commit(self, conn, cursor, force=False):
        """Commit periodically to avoid memory buildup."""
        self.processed_count += 1
        
        if force or self.processed_count % BATCH_SIZE == 0:
            conn.commit()
            self.commit_count += 1
            if self.processed_count % (BATCH_SIZE * 10) == 0:
                print(f"Processed {self.processed_count} items, {self.commit_count} commits...")

    def load_jlpt_mappings(self):
        """Load JLPT level mappings from reference files using streaming."""
        print("Loading JLPT level mappings...")
        
        # Load kanji JLPT mapping
        kanji_ref_path = self.source_dir / "kanji" / "reference.json"
        if kanji_ref_path.exists():
            with open(kanji_ref_path, 'rb') as f:
                # Stream the JSON file
                parser = ijson.kvitems(f, '')
                for character, data in parser:
                    self.kanji_jlpt_mapping[character] = {
                        'jlpt_old': data.get('jlpt_old'),
                        'jlpt_new': data.get('jlpt_new')
                    }
            print(f"Loaded JLPT mapping for {len(self.kanji_jlpt_mapping)} kanji")
        
        # Load vocabulary JLPT mapping
        vocab_ref_path = self.source_dir / "vocabulary" / "reference.json"
        if vocab_ref_path.exists():
            with open(vocab_ref_path, 'rb') as f:
                parser = ijson.kvitems(f, '')
                for word, jlpt_level in parser:
                    jlpt_numeric = None
                    if isinstance(jlpt_level, str) and jlpt_level.startswith('N'):
                        jlpt_numeric = int(jlpt_level[1:])
                    self.vocabulary_jlpt_mapping[word] = jlpt_numeric
            print(f"Loaded JLPT mapping for {len(self.vocabulary_jlpt_mapping)} vocabulary entries")

    def process_kanji_data(self, conn, cursor):
        """Process kanji data from kanjidic2 source using streaming."""
        print("Processing kanji data...")
        
        kanji_source_path = self.source_dir / "kanji" / "source.json"
        if not kanji_source_path.exists():
            print(f"Kanji source file not found: {kanji_source_path}")
            return
        
        # Batch buffers
        kanji_batch = []
        codepoint_batch = []
        dict_ref_batch = []
        query_code_batch = []
        reading_batch = []
        meaning_batch = []
        nanori_batch = []
        
        with open(kanji_source_path, 'rb') as f:
            # Stream characters array
            characters = ijson.items(f, 'characters.item')
            
            for character_data in characters:
                character = character_data.get('literal', '')
                if not character:
                    continue
                
                # Extract basic information
                misc = character_data.get('misc', {})
                
                # Get JLPT levels from mapping
                jlpt_old = None
                jlpt_new = None
                if character in self.kanji_jlpt_mapping:
                    jlpt_old = self.kanji_jlpt_mapping[character]['jlpt_old']
                    jlpt_new = self.kanji_jlpt_mapping[character]['jlpt_new']
                
                # Add to batch
                stroke_count = misc.get('strokeCounts', [0])[0] if misc.get('strokeCounts') else 0
                kanji_batch.append((
                    character,
                    misc.get('grade'),
                    stroke_count,
                    misc.get('frequency'),
                    jlpt_old,
                    jlpt_new
                ))
                
                # Process batch if it's full
                if len(kanji_batch) >= BATCH_SIZE:
                    self._flush_kanji_batch(cursor, kanji_batch, codepoint_batch, 
                                           dict_ref_batch, query_code_batch,
                                           reading_batch, meaning_batch, nanori_batch,
                                           character_data)
                    kanji_batch.clear()
                    self.periodic_commit(conn, cursor)
                
                # Store for processing sub-items
                self._queue_kanji_subitems(character_data, character, 
                                          codepoint_batch, dict_ref_batch,
                                          query_code_batch, reading_batch,
                                          meaning_batch, nanori_batch)
            
            # Flush remaining batches
            if kanji_batch:
                self._flush_kanji_batch(cursor, kanji_batch, codepoint_batch,
                                       dict_ref_batch, query_code_batch,
                                       reading_batch, meaning_batch, nanori_batch,
                                       None)
                self.periodic_commit(conn, cursor, force=True)

    def _queue_kanji_subitems(self, character_data, character, codepoint_batch,
                              dict_ref_batch, query_code_batch, reading_batch,
                              meaning_batch, nanori_batch):
        """Queue kanji sub-items for batch processing."""
        # Queue codepoints
        for codepoint in character_data.get('codepoints', []):
            codepoint_batch.append((
                character,
                codepoint.get('type', ''),
                codepoint.get('value', '')
            ))
        
        # Queue dictionary references
        for ref in character_data.get('dictionaryReferences', []):
            morohashi = ref.get('morohashi') or {}
            dict_ref_batch.append((
                character,
                ref.get('type', ''),
                ref.get('value', ''),
                morohashi.get('volume') if morohashi else None,
                morohashi.get('page') if morohashi else None
            ))
        
        # Queue query codes
        for qc in character_data.get('queryCodes', []):
            query_code_batch.append((
                character,
                qc.get('type', ''),
                qc.get('value', ''),
                qc.get('skipMisclassification')
            ))
        
        # Queue readings and meanings
        reading_meaning = character_data.get('readingMeaning', {})
        if reading_meaning and 'groups' in reading_meaning:
            for group in reading_meaning['groups']:
                for reading in group.get('readings', []):
                    reading_batch.append((
                        character,
                        reading.get('type', ''),
                        reading.get('value', ''),
                        reading.get('status'),
                        reading.get('onType')
                    ))
                
                for meaning in group.get('meanings', []):
                    meaning_batch.append((
                        character,
                        meaning.get('lang', ''),
                        meaning.get('value', '')
                    ))
        
        # Queue nanori
        for nanori in character_data.get('nanori', []):
            nanori_batch.append((character, nanori))

    def _flush_kanji_batch(self, cursor, kanji_batch, codepoint_batch,
                          dict_ref_batch, query_code_batch, reading_batch,
                          meaning_batch, nanori_batch, current_data):
        """Flush kanji batch to database."""
        if not kanji_batch:
            return
        
        # Insert kanji one by one to get RETURNING values
        # executemany doesn't work with RETURNING clause in psycopg2
        insert_kanji_sql = """
            INSERT INTO jlpt.kanji (
                literal, grade, stroke_count, frequency, jlpt_level_old, jlpt_level_new
            ) VALUES (%s, %s, %s, %s, %s, %s)
            ON CONFLICT (literal) DO UPDATE SET
                grade = EXCLUDED.grade,
                stroke_count = EXCLUDED.stroke_count,
                frequency = EXCLUDED.frequency,
                jlpt_level_old = EXCLUDED.jlpt_level_old,
                jlpt_level_new = EXCLUDED.jlpt_level_new,
                updated_at = CURRENT_TIMESTAMP
            RETURNING id, literal
        """
        
        # Insert each kanji and cache the ID
        for kanji_data in kanji_batch:
            cursor.execute(insert_kanji_sql, kanji_data)
            result = cursor.fetchone()
            if result:
                kanji_id, character = result
                self.kanji_cache[character] = kanji_id
        
        # Flush sub-item batches
        self._flush_kanji_subitems(cursor, codepoint_batch, dict_ref_batch,
                                   query_code_batch, reading_batch,
                                   meaning_batch, nanori_batch)

    def _flush_kanji_subitems(self, cursor, codepoint_batch, dict_ref_batch,
                             query_code_batch, reading_batch, meaning_batch,
                             nanori_batch):
        """Flush kanji sub-items to database."""
        # Codepoints
        if codepoint_batch:
            codepoint_data = [
                (self.kanji_cache.get(char), type_, value)
                for char, type_, value in codepoint_batch
                if char in self.kanji_cache
            ]
            if codepoint_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_codepoint (kanji_id, type, value)
                    VALUES (%s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, codepoint_data)
            codepoint_batch.clear()
        
        # Dictionary references
        if dict_ref_batch:
            dict_data = [
                (self.kanji_cache.get(char), type_, value, vol, page)
                for char, type_, value, vol, page in dict_ref_batch
                if char in self.kanji_cache
            ]
            if dict_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_dictionary_reference (
                        kanji_id, type, value, morohashi_volume, morohashi_page
                    ) VALUES (%s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, dict_data)
            dict_ref_batch.clear()
        
        # Query codes
        if query_code_batch:
            query_data = [
                (self.kanji_cache.get(char), type_, value, skip)
                for char, type_, value, skip in query_code_batch
                if char in self.kanji_cache
            ]
            if query_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_query_code (
                        kanji_id, type, value, skip_missclassification
                    ) VALUES (%s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, query_data)
            query_code_batch.clear()
        
        # Readings
        if reading_batch:
            reading_data = [
                (self.kanji_cache.get(char), type_, value, status, on_type)
                for char, type_, value, status, on_type in reading_batch
                if char in self.kanji_cache
            ]
            if reading_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_reading (
                        kanji_id, type, value, status, on_type
                    ) VALUES (%s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, reading_data)
            reading_batch.clear()
        
        # Meanings
        if meaning_batch:
            meaning_data = [
                (self.kanji_cache.get(char), lang, value)
                for char, lang, value in meaning_batch
                if char in self.kanji_cache
            ]
            if meaning_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_meaning (kanji_id, lang, value)
                    VALUES (%s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, meaning_data)
            meaning_batch.clear()
        
        # Nanori
        if nanori_batch:
            nanori_data = [
                (self.kanji_cache.get(char), nanori)
                for char, nanori in nanori_batch
                if char in self.kanji_cache
            ]
            if nanori_data:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_nanori (kanji_id, value)
                    VALUES (%s, %s)
                    ON CONFLICT DO NOTHING
                """, nanori_data)
            nanori_batch.clear()

    def process_vocabulary_data(self, conn, cursor):
        """Process vocabulary data from JMdict source using streaming."""
        print("Processing vocabulary data...")
        
        vocab_source_path = self.source_dir / "vocabulary" / "source.json"
        if not vocab_source_path.exists():
            print(f"Vocabulary source file not found: {vocab_source_path}")
            return
        
        with open(vocab_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for word_data in words:
                jmdict_id = word_data.get('id', '')
                if not jmdict_id:
                    continue
                
                # Get JLPT level from mapping
                jlpt_new = self._get_vocab_jlpt_level(word_data)
                
                # Insert vocabulary
                cursor.execute("""
                    INSERT INTO jlpt.vocabulary (jmdict_id, jlpt_level_new)
                    VALUES (%s, %s)
                    ON CONFLICT (jmdict_id) DO UPDATE SET
                        jlpt_level_new = EXCLUDED.jlpt_level_new,
                        updated_at = CURRENT_TIMESTAMP
                    RETURNING id
                """, (jmdict_id, jlpt_new,))
                
                vocabulary_id = cursor.fetchone()[0]
                self.vocabulary_cache[jmdict_id] = vocabulary_id
                
                # Process kanji, kana, and senses
                self._process_vocab_forms(cursor, vocabulary_id, word_data)
                self._process_vocab_senses(cursor, vocabulary_id, word_data)
                
                self.periodic_commit(conn, cursor)
        
        self.periodic_commit(conn, cursor, force=True)

    def _get_vocab_jlpt_level(self, word_data):
        """Get JLPT level for vocabulary from mapping."""
        jlpt_new = None
        
        for kanji in word_data.get('kanji', []):
            kanji_text = kanji.get('text', '')
            if kanji_text in self.vocabulary_jlpt_mapping:
                jlpt_new = self.vocabulary_jlpt_mapping[kanji_text]
                break
        
        if not jlpt_new:
            for kana in word_data.get('kana', []):
                kana_text = kana.get('text', '')
                if kana_text in self.vocabulary_jlpt_mapping:
                    jlpt_new = self.vocabulary_jlpt_mapping[kana_text]
                    break
        
        return jlpt_new

    def _process_vocab_forms(self, cursor, vocabulary_id, word_data):
        """Process kanji and kana forms for vocabulary."""
        # Process kanji forms
        kanji_batch = []
        kanji_tag_batch = []
        
        for kanji in word_data.get('kanji', []):
            kanji_text = kanji.get('text', '')
            is_common = kanji.get('common', False)
            kanji_batch.append((vocabulary_id, kanji_text, is_common))
            
            for tag in kanji.get('tags', []):
                self.ensure_tag_exists(cursor, tag, 'kanji')
                kanji_tag_batch.append((vocabulary_id, kanji_text, tag))
        
        if kanji_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_kanji (vocabulary_id, text, is_common)
                VALUES (%s, %s, %s)
                ON CONFLICT DO NOTHING
            """, kanji_batch)
        
        if kanji_tag_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_kanji_tag (vocabulary_kanji_id, tag_code)
                SELECT vk.id, %s FROM jlpt.vocabulary_kanji vk
                WHERE vk.vocabulary_id = %s AND vk.text = %s
                ON CONFLICT DO NOTHING
            """, [(tag, vid, text) for vid, text, tag in kanji_tag_batch])
        
        # Process kana forms
        kana_batch = []
        kana_tag_batch = []
        
        for kana in word_data.get('kana', []):
            kana_text = kana.get('text', '')
            is_common = kana.get('common', False)
            applies_to_kanji = kana.get('appliesToKanji', [])
            kana_batch.append((vocabulary_id, kana_text, applies_to_kanji, is_common))
            
            for tag in kana.get('tags', []):
                self.ensure_tag_exists(cursor, tag, 'kana')
                kana_tag_batch.append((vocabulary_id, kana_text, tag))
        
        if kana_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_kana (vocabulary_id, text, applies_to_kanji, is_common)
                VALUES (%s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, kana_batch)
        
        if kana_tag_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_kana_tag (vocabulary_kana_id, tag_code)
                SELECT vka.id, %s FROM jlpt.vocabulary_kana vka
                WHERE vka.vocabulary_id = %s AND vka.text = %s
                ON CONFLICT DO NOTHING
            """, [(tag, vid, text) for vid, text, tag in kana_tag_batch])

    def _process_vocab_senses(self, cursor, vocabulary_id, word_data):
        """Process senses for vocabulary."""
        for sense in word_data.get('sense', []):
            applies_to_kanji = sense.get('appliesToKanji', [])
            applies_to_kana = sense.get('appliesToKana', [])
            info = sense.get('info', [])
            
            cursor.execute("""
                INSERT INTO jlpt.vocabulary_sense (vocabulary_id, applies_to_kanji, applies_to_kana, info)
                VALUES (%s, %s, %s, %s)
                RETURNING id
            """, (vocabulary_id, applies_to_kanji, applies_to_kana, info))
            
            sense_id = cursor.fetchone()[0]
            
            # Process sense attributes in batches
            self._process_sense_attributes(cursor, sense_id, sense)

    def _process_sense_attributes(self, cursor, sense_id, sense):
        """Process attributes for a sense."""
        # Unified tag processing
        tag_batch = []
        
        for pos in sense.get('partOfSpeech', []):
            self.ensure_tag_exists(cursor, pos, 'part_of_speech')
            tag_batch.append((sense_id, pos, 'pos'))
        
        for field in sense.get('field', []):
            self.ensure_tag_exists(cursor, field, 'field')
            tag_batch.append((sense_id, field, 'field'))
        
        for dialect in sense.get('dialect', []):
            self.ensure_tag_exists(cursor, dialect, 'dialect')
            tag_batch.append((sense_id, dialect, 'dialect'))
        
        for misc in sense.get('misc', []):
            self.ensure_tag_exists(cursor, misc, 'misc')
            tag_batch.append((sense_id, misc, 'misc'))
        
        if tag_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_tag (sense_id, tag_code, tag_type)
                VALUES (%s, %s, %s)
                ON CONFLICT DO NOTHING
            """, tag_batch)
        
        # Language sources
        lang_batch = [
            (sense_id, ls.get('lang'), ls.get('text'), ls.get('full'), ls.get('wasei'))
            for ls in sense.get('languageSource', [])
        ]
        if lang_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_language_source (
                    sense_id, lang, text, "full", wasei
                ) VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, lang_batch)
        
        # Glosses
        gloss_batch = [
            (sense_id, g.get('lang'), g.get('text'), g.get('gender'), g.get('type'))
            for g in sense.get('gloss', [])
        ]
        if gloss_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_gloss (
                    sense_id, lang, text, gender, type
                ) VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, gloss_batch)

        # Related terms - store for later resolution
        for related in sense.get('related', []):
            if isinstance(related, list) and len(related) > 0:
                term = related[0] if len(related) > 0 else None
                reading = related[1] if len(related) > 1 else None
                sense_index = related[2] if len(related) > 2 else None
                if term:
                    self.pending_vocab_relations.append((
                        sense_id, term, reading, sense_index, 'related'
                    ))
            elif isinstance(related, dict):
                term = related.get('term')
                if term:
                    reading = related.get('reading')
                    sense_index = related.get('sense')
                    self.pending_vocab_relations.append((
                        sense_id, term, reading, sense_index, 'related'
                    ))

        # Antonyms - store for later resolution
        for antonym in sense.get('antonym', []):
            if isinstance(antonym, list) and len(antonym) > 0:
                term = antonym[0] if len(antonym) > 0 else None
                reading = antonym[1] if len(antonym) > 1 else None
                sense_index = antonym[2] if len(antonym) > 2 else None
                if term:
                    self.pending_vocab_relations.append((
                        sense_id, term, reading, sense_index, 'antonym'
                    ))
            elif isinstance(antonym, dict):
                term = antonym.get('term')
                if term:
                    reading = antonym.get('reading')
                    sense_index = antonym.get('sense')
                    self.pending_vocab_relations.append((
                        sense_id, term, reading, sense_index, 'antonym'
                    ))

    def ensure_tag_exists(self, cursor, tag_code: str, category: str):
        """Ensure a tag exists in the database."""
        if tag_code in self.tag_cache:
            return
        
        if not hasattr(self, '_tag_descriptions'):
            self._load_tag_descriptions()
        
        description = self._tag_descriptions.get(tag_code, f'{category} tag')
        
        cursor.execute("""
            INSERT INTO jlpt.tag (code, description, category)
            VALUES (%s, %s, %s)
            ON CONFLICT (code) DO NOTHING
        """, (tag_code, description, category))
        
        self.tag_cache[tag_code] = True

    def _load_tag_descriptions(self):
        """Load tag descriptions from JSON source files using streaming."""
        self._tag_descriptions = {}
        
        json_files = [
            self.source_dir / 'vocabulary' / 'source.json',
            self.source_dir / 'vocabulary' / 'vocabularyWithExamples' / 'source.json',
            self.source_dir / 'names' / 'source.json'
        ]
        
        for json_file in json_files:
            if json_file.exists():
                try:
                    with open(json_file, 'rb') as f:
                        # Stream only the tags section
                        tags = ijson.kvitems(f, 'tags')
                        for tag_code, description in tags:
                            self._tag_descriptions[tag_code] = description
                    print(f"Loaded tags from {json_file.name}")
                except Exception as e:
                    print(f"Error loading tags from {json_file}: {e}")
        
        print(f"Total unique tags loaded: {len(self._tag_descriptions)}")

    def _normalize_radical_char(self, char):
        """Normalize look-alike radical characters (Katakana/Full-width to CJK Ideographs)."""
        norm_map = {
            '｜': '丨', '|': '丨',
            'ノ': '丿',
            'ハ': '八',
            'ト': '卜',
            'ヨ': '彐', 'ユ': '彐',
            'マ': '龴',
            'ム': '厶'
        }
        return norm_map.get(char, char)

    def _get_radical_group_maps(self):
        """
        Load radical grouping maps from reference.txt.
        Returns (char_to_leader, leader_to_ref_data).
        """
        ref_path = self.source_dir / "radfile" / "reference.txt"
        char_to_leader = {}
        leader_to_ref_data = {}
        
        if not ref_path.exists():
            print(f"Radical reference file not found: {ref_path}")
            return char_to_leader, leader_to_ref_data
        
        print(f"Building radical group maps from {ref_path}...")
        with open(ref_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line: continue
                
                parts = line.split('\t')
                if len(parts) < 7: parts += [''] * (7 - len(parts))
                
                leader = self._normalize_radical_char(parts[0])
                variant = self._normalize_radical_char(parts[1]) if parts[1] else None
                
                if variant and char_to_leader.get(variant):
                    current_leader = char_to_leader[variant]
                else:
                    current_leader = char_to_leader.get(leader, leader)
                
                char_to_leader[leader] = current_leader
                if variant:
                    char_to_leader[variant] = current_leader
                
                # Also record the original (un-normalized) characters as mapping to the leader
                char_to_leader[parts[0]] = current_leader
                if parts[1]:
                    char_to_leader[parts[1]] = current_leader

                if current_leader not in leader_to_ref_data:
                    try:
                        kxn = int(parts[2]) if parts[2] else None
                    except ValueError:
                        kxn = None
                        
                    leader_to_ref_data[current_leader] = {
                        'meanings': [m.strip() for m in parts[5].split(',') if m.strip()],
                        'readings': [r.strip() for r in parts[4].split('・') if r.strip()],
                        'variants': {variant} if variant else set(),
                        'notes': [parts[6]] if parts[6] else [],
                        'kang_xi_number': kxn
                    }
                else:
                    if variant:
                        leader_to_ref_data[current_leader]['variants'].add(variant)
                    if parts[6] and parts[6] not in leader_to_ref_data[current_leader]['notes']:
                        leader_to_ref_data[current_leader]['notes'].append(parts[6])

        for char, leader in char_to_leader.items():
            if char != leader:
                leader_to_ref_data[leader]['variants'].add(char)
                
        return char_to_leader, leader_to_ref_data

    def process_radical_data(self, conn, cursor):
        """Process radical data using the new three-table model."""
        print("Processing radical data...")
        
        # === Phase 1: Populate radical_group from reference.txt ===
        print("Radical Phase 1: Populating radical_group...")
        ref_path = self.source_dir / "radfile" / "reference.txt"
        if not ref_path.exists():
            print(f"Reference file not found: {ref_path}")
            return
        
        # Parse reference.txt and insert into radical_group
        radical_group_cache = {}  # canonical_literal -> group_id
        
        with open(ref_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line: continue
                
                parts = line.split('\t')
                if len(parts) < 7: parts += [''] * (7 - len(parts))
                
                canonical = parts[0]
                norm_canonical = self._normalize_radical_char(canonical)
                variant = parts[1] if parts[1] else None
                
                try:
                    kxn = int(parts[2]) if parts[2] else None
                except ValueError:
                    kxn = None
                    
                readings = [r.strip() for r in parts[4].split('・') if r.strip()]
                meanings = [m.strip() for m in parts[5].split(',') if m.strip()]
                notes = [parts[6]] if parts[6] else []
                
                if norm_canonical not in radical_group_cache:
                    cursor.execute("""
                        INSERT INTO jlpt.radical_group (canonical_literal, kang_xi_number, meanings, readings, notes)
                        VALUES (%s, %s, %s, %s, %s)
                        ON CONFLICT (canonical_literal) DO UPDATE SET
                            kang_xi_number = COALESCE(EXCLUDED.kang_xi_number, jlpt.radical_group.kang_xi_number),
                            meanings = CASE WHEN array_length(EXCLUDED.meanings, 1) > 0 THEN EXCLUDED.meanings ELSE jlpt.radical_group.meanings END,
                            readings = CASE WHEN array_length(EXCLUDED.readings, 1) > 0 THEN EXCLUDED.readings ELSE jlpt.radical_group.readings END,
                            notes = jlpt.radical_group.notes || EXCLUDED.notes,
                            updated_at = CURRENT_TIMESTAMP
                        RETURNING id
                    """, (norm_canonical, kxn, meanings, readings, notes))
                    result = cursor.fetchone()
                    if result:
                        radical_group_cache[norm_canonical] = result[0]
                        radical_group_cache[canonical] = result[0]
        
        self.periodic_commit(conn, cursor)
        print(f"Created {len(set(radical_group_cache.values()))} radical groups.")
        
        # === Phase 2: Populate radical_group_member from reference.txt ===
        print("Radical Phase 2: Populating radical_group_member...")
        member_count = 0
        
        with open(ref_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line: continue
                
                parts = line.split('\t')
                if len(parts) < 7: parts += [''] * (7 - len(parts))
                
                canonical = parts[0]
                variant = parts[1] if parts[1] else None
                norm_canonical = self._normalize_radical_char(canonical)
                group_id = radical_group_cache.get(norm_canonical)
                if not group_id: continue
                
                # Insert canonical (and normalized canonical) as members
                for lit, is_canon in [(canonical, True), (variant, False)]:
                    if not lit: continue
                    norm_lit = self._normalize_radical_char(lit)
                    cursor.execute("""
                        INSERT INTO jlpt.radical_group_member (group_id, literal, is_canonical)
                        VALUES (%s, %s, %s)
                        ON CONFLICT (group_id, literal) DO NOTHING
                    """, (group_id, lit, is_canon))
                    if lit != norm_lit:
                        cursor.execute("""
                            INSERT INTO jlpt.radical_group_member (group_id, literal, is_canonical)
                            VALUES (%s, %s, %s)
                            ON CONFLICT (group_id, literal) DO NOTHING
                        """, (group_id, norm_lit, False))
                    member_count += 1
        self.periodic_commit(conn, cursor)
        print(f"Inserted {member_count} radical_group_member entries.")
        
        # Build member -> group_id lookup for source.json linking
        cursor.execute("SELECT group_id, literal FROM jlpt.radical_group_member")
        member_to_group = {row[1]: row[0] for row in cursor.fetchall()}
        
        # === Phase 3: Populate radical from source.json ===
        print("Radical Phase 3: Processing source.json...")
        radfile_path = self.source_dir / "radfile" / "source.json"
        if not radfile_path.exists():
            print(f"Radfile source not found: {radfile_path}")
            return

        radical_batch = []
        with open(radfile_path, 'rb') as f:
            radicals_src = ijson.kvitems(f, 'radicals')
            for char, data in radicals_src:
                norm_char = self._normalize_radical_char(char)
                group_id = member_to_group.get(norm_char) or member_to_group.get(char)
                
                radical_batch.append((
                    char,
                    data.get('strokeCount', 0),
                    data.get('code'),
                    group_id
                ))
                
                if len(radical_batch) >= BATCH_SIZE:
                    self._flush_radical_batch(cursor, radical_batch)
                    radical_batch.clear()
                    self.periodic_commit(conn, cursor)
        
        if radical_batch:
            self._flush_radical_batch(cursor, radical_batch)
            self.periodic_commit(conn, cursor, force=True)
        
        print("Radical Phase 3 complete.")
        
        # === Phase 4: Process kradfile ===
        kradfile_path = self.source_dir / "kradfile" / "source.json"
        if kradfile_path.exists():
            print("Radical Phase 4: Processing kradfile...")
            with open(kradfile_path, 'rb') as f:
                kanji_items = ijson.kvitems(f, 'kanji')
                relationship_batch = []
                
                for kanji_char, components in kanji_items:
                    if kanji_char not in self.kanji_cache:
                        continue
                    
                    kanji_id = self.kanji_cache[kanji_char]
                    
                    for component in components:
                        if component in self.radical_cache:
                            radical_id = self.radical_cache[component]
                            relationship_batch.append((kanji_id, radical_id))
                    
                    if len(relationship_batch) >= BATCH_SIZE:
                        cursor.executemany("""
                            INSERT INTO jlpt.kanji_radical (kanji_id, radical_id)
                            VALUES (%s, %s)
                            ON CONFLICT DO NOTHING
                        """, relationship_batch)
                        relationship_batch.clear()
                        self.periodic_commit(conn, cursor)
                
                if relationship_batch:
                    cursor.executemany("""
                        INSERT INTO jlpt.kanji_radical (kanji_id, radical_id)
                        VALUES (%s, %s)
                        ON CONFLICT DO NOTHING
                    """, relationship_batch)
                    self.periodic_commit(conn, cursor, force=True)
            print("Radical Phase 4 complete.")

    def _flush_radical_batch(self, cursor, radical_batch):
        """Flush radical batch to database."""
        insert_sql = """
            INSERT INTO jlpt.radical (literal, stroke_count, code, group_id)
            VALUES (%s, %s, %s, %s)
            ON CONFLICT (literal) DO UPDATE SET
                stroke_count = EXCLUDED.stroke_count,
                code = EXCLUDED.code,
                group_id = EXCLUDED.group_id,
                updated_at = CURRENT_TIMESTAMP
            RETURNING id
        """
        
        for radical_data in radical_batch:
            # radical_data: (literal, stroke_count, code, group_id)
            cursor.execute(insert_sql, radical_data)
            result = cursor.fetchone()
            if result:
                radical_id = result[0]
                self.radical_cache[radical_data[0]] = radical_id

    def process_kanji_vocabulary_relationships(self, conn, cursor):
        """Process relationships between kanji and vocabulary."""
        print("Processing kanji-vocabulary relationships...")
        
        # Use server-side cursor for large result sets
        cursor.execute("""
            SELECT v.id, vk.text 
            FROM jlpt.vocabulary v
            JOIN jlpt.vocabulary_kanji vk ON vk.vocabulary_id = v.id
        """)
        
        relationship_batch = []
        processed = 0
        
        while True:
            rows = cursor.fetchmany(BATCH_SIZE)
            if not rows:
                break
            
            for vocab_id, kanji_text in rows:
                # Find kanji characters in the text
                for char in kanji_text:
                    if char in self.kanji_cache:
                        kanji_id = self.kanji_cache[char]
                        relationship_batch.append((vocab_id, kanji_id))
            
            processed += len(rows)
            if processed % (BATCH_SIZE * 10) == 0:
                print(f"Processed {processed} vocabulary entries for relationships...")
        
        # Insert relationships in batches
        print(f"Inserting {len(relationship_batch)} kanji-vocabulary relationships...")
        for i in range(0, len(relationship_batch), BATCH_SIZE):
            batch = relationship_batch[i:i + BATCH_SIZE]
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_uses_kanji (vocabulary_id, kanji_id)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, batch)
            self.periodic_commit(conn, cursor)
        
        self.periodic_commit(conn, cursor, force=True)

    def process_proper_nouns(self, conn, cursor):
        """Process proper nouns from JMnedict using streaming."""
        print("Processing proper nouns...")
        
        names_source_path = self.source_dir / "names" / "source.json"
        if not names_source_path.exists():
            print(f"Names source file not found: {names_source_path}")
            return
        
        with open(names_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for name_data in words:
                jmnedict_id = name_data.get('id', '')
                if not jmnedict_id:
                    continue
                
                # Insert proper noun
                cursor.execute("""
                    INSERT INTO jlpt.proper_noun (jmnedict_id)
                    VALUES (%s)
                    ON CONFLICT (jmnedict_id) DO NOTHING
                    RETURNING id
                """, (jmnedict_id,))
                proper_noun_id = cursor.fetchone()[0]
                
                # Process kanji forms
                self._process_proper_noun_forms(cursor, proper_noun_id, name_data)
                
                # Process translations
                self._process_proper_noun_translations(cursor, proper_noun_id, name_data)
                
                # Process kanji relationships
                self._process_proper_noun_kanji_relationships(cursor, proper_noun_id, name_data)
                
                self.periodic_commit(conn, cursor)
        
        self.periodic_commit(conn, cursor, force=True)

    def _process_proper_noun_forms(self, cursor, proper_noun_id, name_data):
        """Process kanji and kana forms for proper nouns."""
        # Process kanji forms
        kanji_batch = []
        kanji_tag_batch = []
        
        for kanji in name_data.get('kanji', []):
            kanji_text = kanji.get('text', '')
            kanji_batch.append((proper_noun_id, kanji_text))
            
            for tag in kanji.get('tags', []):
                self.ensure_tag_exists(cursor, tag, 'proper_noun')
                kanji_tag_batch.append((proper_noun_id, kanji_text, tag))
        
        if kanji_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_kanji (proper_noun_id, text)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, kanji_batch)
        
        if kanji_tag_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_kanji_tag (proper_noun_kanji_id, tag_code)
                SELECT pnk.id, %s FROM jlpt.proper_noun_kanji pnk
                WHERE pnk.proper_noun_id = %s AND pnk.text = %s
                ON CONFLICT DO NOTHING
            """, [(tag, pid, text) for pid, text, tag in kanji_tag_batch])
        
        # Process kana forms
        kana_batch = []
        kana_tag_batch = []
        
        for kana in name_data.get('kana', []):
            kana_text = kana.get('text', '')
            applies_to_kanji = kana.get('appliesToKanji', [])
            kana_batch.append((proper_noun_id, kana_text, applies_to_kanji))
            
            for tag in kana.get('tags', []):
                self.ensure_tag_exists(cursor, tag, 'proper_noun')
                kana_tag_batch.append((proper_noun_id, kana_text, tag))
        
        if kana_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_kana (proper_noun_id, text, applies_to_kanji)
                VALUES (%s, %s, %s)
                ON CONFLICT DO NOTHING
            """, kana_batch)
        
        if kana_tag_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_kana_tag (proper_noun_kana_id, tag_code)
                SELECT pnk.id, %s FROM jlpt.proper_noun_kana pnk
                WHERE pnk.proper_noun_id = %s AND pnk.text = %s
                ON CONFLICT DO NOTHING
            """, [(tag, pid, text) for pid, text, tag in kana_tag_batch])

    def _process_proper_noun_translations(self, cursor, proper_noun_id, name_data):
        """Process translations for proper nouns."""
        for trans in name_data.get('translation', []):
            cursor.execute("""
                INSERT INTO jlpt.proper_noun_translation (proper_noun_id)
                VALUES (%s)
                RETURNING id
            """, (proper_noun_id,))
            translation_id = cursor.fetchone()[0]
            
            # Process translation types
            type_batch = []
            for trans_type in trans.get('type', []):
                self.ensure_tag_exists(cursor, trans_type, 'translation_type')
                type_batch.append((translation_id, trans_type))
            
            if type_batch:
                cursor.executemany("""
                    INSERT INTO jlpt.proper_noun_translation_type (translation_id, tag_code)
                    VALUES (%s, %s)
                    ON CONFLICT DO NOTHING
                """, type_batch)
            
            # Store related terms for later resolution
            # New jmdict-simplified format: related is a list of arrays
            for related in trans.get('related', []):
                if isinstance(related, list) and len(related) > 0:
                    term = related[0] if len(related) > 0 else None
                    reading = related[1] if len(related) > 1 else None
                    sense_index = related[2] if len(related) > 2 else None
                    if term:
                        self.pending_proper_noun_relations.append((
                            translation_id, term, reading, sense_index
                        ))
                elif isinstance(related, dict):
                    # Legacy format support
                    term = related.get('term')
                    if term:
                        reading = related.get('reading')
                        sense_index = related.get('sense')
                        self.pending_proper_noun_relations.append((
                            translation_id, term, reading, sense_index
                        ))

            # # Process related terms
            # related_batch = [(translation_id, None, None, r.get('term'), r.get('reading')) for r in trans.get('related', [])]
            # if related_batch:
            #     cursor.executemany("""
            #         INSERT INTO jlpt.proper_noun_translation_related (translation_id, related_term, related_reading, reference_proper_noun_id, reference_proper_noun_translation_id)
            #         VALUES (%s, %s, %s, %s, %s)
            #         ON CONFLICT DO NOTHING
            #     """, related_batch)
            
            # Process translation text
            text_batch = [
                (translation_id, t.get('lang'), t.get('text'))
                for t in trans.get('translation', [])
            ]
            if text_batch:
                cursor.executemany("""
                    INSERT INTO jlpt.proper_noun_translation_text (translation_id, lang, text)
                    VALUES (%s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, text_batch)

    def _process_proper_noun_kanji_relationships(self, cursor, proper_noun_id, name_data):
        """Process kanji relationships for proper nouns."""
        relationship_batch = []
        
        for kanji in name_data.get('kanji', []):
            kanji_text = kanji.get('text', '')
            for char in kanji_text:
                if char in self.kanji_cache:
                    kanji_id = self.kanji_cache[char]
                    relationship_batch.append((proper_noun_id, kanji_id))
        
        if relationship_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_uses_kanji (proper_noun_id, kanji_id)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, relationship_batch)

    def process_vocabulary_examples(self, conn, cursor):
        """Process vocabulary examples data using streaming."""
        print("Processing vocabulary examples...")
        
        examples_source_path = self.source_dir / "vocabulary" / "vocabularyWithExamples" / "source.json"
        if not examples_source_path.exists():
            print(f"Vocabulary examples source file not found: {examples_source_path}")
            return
        
        with open(examples_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for word_data in words:
                jmdict_id = word_data.get('id', '')
                if not jmdict_id or jmdict_id not in self.vocabulary_cache:
                    continue
                
                vocab_id = self.vocabulary_cache[jmdict_id]
                
                # Process examples for each sense
                for sense in word_data.get('sense', []):
                    for example in sense.get('examples', []):
                        source_type = example.get('source', {}).get('type')
                        source_value = example.get('source', {}).get('value')
                        text = example.get('text', '')
                        
                        cursor.execute("""
                            INSERT INTO jlpt.vocabulary_sense_example (
                                sense_id, source_type, source_value, text
                            ) VALUES (
                                (SELECT id FROM jlpt.vocabulary_sense WHERE vocabulary_id = %s LIMIT 1),
                                %s, %s, %s
                            )
                            RETURNING id
                        """, (vocab_id, source_type, source_value, text))
                        
                        result = cursor.fetchone()
                        if result:
                            example_id = result[0]
                            
                            # Process example sentences
                            sentence_batch = [
                                (example_id, s.get('lang'), s.get('text'))
                                for s in example.get('sentences', [])
                            ]
                            
                            if sentence_batch:
                                cursor.executemany("""
                                    INSERT INTO jlpt.vocabulary_sense_example_sentence (
                                        example_id, lang, text
                                    ) VALUES (%s, %s, %s)
                                    ON CONFLICT DO NOTHING
                                """, sentence_batch)
                
                self.periodic_commit(conn, cursor)
        
        self.periodic_commit(conn, cursor, force=True)

    def build_vocabulary_term_cache(self, cursor):
        """Build a cache of vocabulary terms for fast lookup."""
        print("Building vocabulary term lookup cache...")
        
        cursor.execute("""
            SELECT 
                v.id,
                vk.text as kanji_text,
                vka.text as kana_text
            FROM jlpt.vocabulary v
            LEFT JOIN jlpt.vocabulary_kanji vk ON vk.vocabulary_id = v.id
            LEFT JOIN jlpt.vocabulary_kana vka ON vka.vocabulary_id = v.id
        """)
        
        count = 0
        while True:
            rows = cursor.fetchmany(BATCH_SIZE)
            if not rows:
                break
            
            for vocab_id, kanji_text, kana_text in rows:
                # Cache by kanji form
                if kanji_text:
                    key = (kanji_text, None)
                    if key not in self.vocab_term_cache:
                        self.vocab_term_cache[key] = vocab_id
                    
                    # Cache with reading if available
                    if kana_text:
                        key = (kanji_text, kana_text)
                        if key not in self.vocab_term_cache:
                            self.vocab_term_cache[key] = vocab_id
                
                # Cache by kana form only
                if kana_text:
                    key = (kana_text, None)
                    if key not in self.vocab_term_cache:
                        self.vocab_term_cache[key] = vocab_id
            
            count += len(rows)
            if count % (BATCH_SIZE * 10) == 0:
                print(f"Cached {count} vocabulary term mappings...")
        
        print(f"Vocabulary term cache built: {len(self.vocab_term_cache)} unique term mappings")

    def build_proper_noun_term_cache(self, cursor):
        """Build a cache of proper noun terms for fast lookup."""
        print("Building proper noun term lookup cache...")
        
        cursor.execute("""
            SELECT 
                pn.id,
                pnk.text as kanji_text,
                pnka.text as kana_text
            FROM jlpt.proper_noun pn
            LEFT JOIN jlpt.proper_noun_kanji pnk ON pnk.proper_noun_id = pn.id
            LEFT JOIN jlpt.proper_noun_kana pnka ON pnka.proper_noun_id = pn.id
        """)
        
        count = 0
        while True:
            rows = cursor.fetchmany(BATCH_SIZE)
            if not rows:
                break
            
            for proper_noun_id, kanji_text, kana_text in rows:
                # Cache by kanji form
                if kanji_text:
                    key = (kanji_text, None)
                    if key not in self.proper_noun_term_cache:
                        self.proper_noun_term_cache[key] = proper_noun_id
                    
                    # Cache with reading if available
                    if kana_text:
                        key = (kanji_text, kana_text)
                        if key not in self.proper_noun_term_cache:
                            self.proper_noun_term_cache[key] = proper_noun_id
                
                # Cache by kana form only
                if kana_text:
                    key = (kana_text, None)
                    if key not in self.proper_noun_term_cache:
                        self.proper_noun_term_cache[key] = proper_noun_id
            
            count += len(rows)
            if count % (BATCH_SIZE * 10) == 0:
                print(f"Cached {count} proper noun term mappings...")
        
        print(f"Proper noun term cache built: {len(self.proper_noun_term_cache)} unique term mappings")    

    def resolve_vocabulary_relations(self, conn, cursor):
        """Resolve pending vocabulary relationships."""
        print(f"Resolving {len(self.pending_vocab_relations)} vocabulary relationships...")
        
        resolved_batch = []
        unresolved_count = 0
        
        for source_sense_id, target_term, target_reading, target_sense_index, relation_type in self.pending_vocab_relations:
            # Try to find target vocabulary
            target_vocab_id = None
            target_sense_id = None
            
            # First try with reading if available
            if target_reading:
                target_vocab_id = self.vocab_term_cache.get((target_term, target_reading))
            
            # Fall back to term only
            if not target_vocab_id:
                target_vocab_id = self.vocab_term_cache.get((target_term, None))
            
            # If we found the vocabulary and a specific sense was requested
            if target_vocab_id and target_sense_index is not None:
                # Get the specific sense (convert from 1-based to 0-based index)
                cursor.execute("""
                    SELECT id 
                    FROM jlpt.vocabulary_sense 
                    WHERE vocabulary_id = %s 
                    ORDER BY id 
                    LIMIT 1 OFFSET %s
                """, (target_vocab_id, target_sense_index - 1))
                
                result = cursor.fetchone()
                if result:
                    target_sense_id = result[0]
            
            # Add to batch even if not fully resolved (target_vocab_id and target_sense_id can be NULL)
            resolved_batch.append((
                source_sense_id,
                target_vocab_id,
                target_sense_id,
                target_term,
                target_reading,
                relation_type
            ))
            
            if not target_vocab_id:
                unresolved_count += 1
            
            # Insert in batches
            if len(resolved_batch) >= BATCH_SIZE:
                cursor.executemany("""
                    INSERT INTO jlpt.vocabulary_sense_relation (
                        source_sense_id, target_vocab_id, target_sense_id, 
                        target_term, target_reading, relation_type
                    ) VALUES (%s, %s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, resolved_batch)
                resolved_batch.clear()
                self.periodic_commit(conn, cursor)
        
        # Insert remaining batch
        if resolved_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_relation (
                    source_sense_id, target_vocab_id, target_sense_id, 
                    target_term, target_reading, relation_type
                ) VALUES (%s, %s, %s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, resolved_batch)
            self.periodic_commit(conn, cursor, force=True)
        
        print(f"Vocabulary relationships resolved: {len(self.pending_vocab_relations) - unresolved_count} resolved, {unresolved_count} unresolved")
        
        # Clear pending relations
        self.pending_vocab_relations.clear()

    def resolve_proper_noun_relations(self, conn, cursor):
        """Resolve pending proper noun relationships."""
        print(f"Resolving {len(self.pending_proper_noun_relations)} proper noun relationships...")
        
        resolved_batch = []
        unresolved_count = 0
        
        for translation_id, related_term, related_reading, related_sense_index in self.pending_proper_noun_relations:
            # Try to find target proper noun
            reference_proper_noun_id = None
            reference_translation_id = None
            
            # First try with reading if available
            if related_reading:
                reference_proper_noun_id = self.proper_noun_term_cache.get((related_term, related_reading))
            
            # Fall back to term only
            if not reference_proper_noun_id:
                reference_proper_noun_id = self.proper_noun_term_cache.get((related_term, None))
            
            # If we found the proper noun and a specific translation was requested
            if reference_proper_noun_id and related_sense_index is not None:
                # Get the specific translation (convert from 1-based to 0-based index)
                cursor.execute("""
                    SELECT id 
                    FROM jlpt.proper_noun_translation 
                    WHERE proper_noun_id = %s 
                    ORDER BY id 
                    LIMIT 1 OFFSET %s
                """, (reference_proper_noun_id, related_sense_index - 1))
                
                result = cursor.fetchone()
                if result:
                    reference_translation_id = result[0]
            
            # Add to batch
            resolved_batch.append((
                translation_id,
                related_term,
                related_reading,
                reference_proper_noun_id,
                reference_translation_id
            ))
            
            if not reference_proper_noun_id:
                unresolved_count += 1
            
            # Insert in batches
            if len(resolved_batch) >= BATCH_SIZE:
                cursor.executemany("""
                    INSERT INTO jlpt.proper_noun_translation_related (
                        translation_id, related_term, related_reading,
                        reference_proper_noun_id, reference_proper_noun_translation_id
                    ) VALUES (%s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, resolved_batch)
                resolved_batch.clear()
                self.periodic_commit(conn, cursor)
        
        # Insert remaining batch
        if resolved_batch:
            cursor.executemany("""
                INSERT INTO jlpt.proper_noun_translation_related (
                    translation_id, related_term, related_reading,
                    reference_proper_noun_id, reference_proper_noun_translation_id
                ) VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, resolved_batch)
            self.periodic_commit(conn, cursor, force=True)
        
        print(f"Proper noun relationships resolved: {len(self.pending_proper_noun_relations) - unresolved_count} resolved, {unresolved_count} unresolved")
        
        # Clear pending relations
        self.pending_proper_noun_relations.clear()


    def refresh_materialized_views(self, cursor):
        """Refresh materialized views after data processing."""
        print("Refreshing materialized views...")
        
        try:
            cursor.execute("SELECT refresh_dictionary_views()")
            print("Materialized views refreshed successfully!")
        except Exception as e:
            print(f"Warning: Could not refresh materialized views: {e}")

    def process_all_data(self):
        """Process all data sources into normalized schema."""
        print("Starting comprehensive data processing...")
        print(f"Batch size: {BATCH_SIZE}")
        
        # Wait for database
        if not self.wait_for_database():
            return False
        
        # Load JLPT mappings
        self.load_jlpt_mappings()
        
        # Connect to database
        try:
            conn = psycopg2.connect(**self.db_params)
            conn.autocommit = False
            cursor = conn.cursor()
        except Exception as e:
            print(f"Error connecting to database: {e}")
            return False
        
        try:
            # Process all data sources in order
            print("\n=== Step 1: Processing kanji data ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_kanji_data(conn, cursor)
            print(f"Kanji processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 2: Processing vocabulary data ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_vocabulary_data(conn, cursor)
            print(f"Vocabulary processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 3: Processing vocabulary examples ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_vocabulary_examples(conn, cursor)
            print(f"Examples processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 4: Processing radical data ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_radical_data(conn, cursor)
            print(f"Radical processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 5: Processing proper nouns ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_proper_nouns(conn, cursor)
            print(f"Proper nouns processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 6: Processing kanji-vocabulary relationships ===")
            self.processed_count = 0
            self.commit_count = 0
            self.process_kanji_vocabulary_relationships(conn, cursor)
            print(f"Relationships processing complete: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 7: Building term lookup caches ===")
            self.build_vocabulary_term_cache(cursor)
            self.build_proper_noun_term_cache(cursor)
            
            print("\n=== Step 8: Resolving vocabulary relationships ===")
            self.processed_count = 0
            self.commit_count = 0
            self.resolve_vocabulary_relations(conn, cursor)
            print(f"Vocabulary relationships resolved: {self.processed_count} items, {self.commit_count} commits")
            
            print("\n=== Step 9: Resolving proper noun relationships ===")
            self.processed_count = 0
            self.commit_count = 0
            self.resolve_proper_noun_relations(conn, cursor)
            print(f"Proper noun relationships resolved: {self.processed_count} items, {self.commit_count} commits")

            # Final commit
            conn.commit()
            print("\n=== All data processing completed successfully! ===")
                        
            # Print statistics
            print("\n=== Database Statistics ===")
            cursor.execute("SELECT COUNT(*) FROM jlpt.kanji")
            kanji_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.vocabulary")
            vocab_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.vocabulary_sense_example")
            examples_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.radical")
            radicals_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.proper_noun")
            proper_nouns_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.vocabulary_uses_kanji")
            relationships_count = cursor.fetchone()[0]
            
            print(f"- Kanji entries: {kanji_count:,}")
            print(f"- Vocabulary entries: {vocab_count:,}")
            print(f"- Vocabulary examples: {examples_count:,}")
            print(f"- Radicals: {radicals_count:,}")
            print(f"- Proper nouns: {proper_nouns_count:,}")
            print(f"- Kanji-vocabulary relationships: {relationships_count:,}")
            print("=" * 40)
            
            return True
            
        except Exception as e:
            print(f"\nError during processing: {e}")
            import traceback
            traceback.print_exc()
            conn.rollback()
            return False
        finally:
            cursor.close()
            conn.close()

def main():
    """Main function."""
    print("=" * 60)
    print("JLPT Reference Database - Data Processor")
    print("=" * 60)
    
    processor = JLPTDataProcessor()
    
    if not processor.process_all_data():
        sys.exit(1)
    
    print("\nData processing completed successfully!")

if __name__ == "__main__":
    main()