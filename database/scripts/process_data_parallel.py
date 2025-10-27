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
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading

# Batch size for commits and bulk inserts
BATCH_SIZE = 500
NUM_WORKERS = int(os.getenv('NUM_WORKERS', '4'))

def safe_print(text):
    """Safely print text that may contain Unicode characters."""
    try:
        print(text)
    except UnicodeEncodeError:
        print(str(text).encode('ascii', 'replace').decode('ascii'))

class ParallelJLPTDataProcessor:
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

        # Locks for shared cache
        self.kanji_cache_lock = threading.Lock()
        self.vocab_cache_lock = threading.Lock()
        self.radical_cache_lock = threading.Lock()
        self.tag_cache_lock = threading.Lock()

        # Connection pool
        self.connection_pool = []

    def get_db_connection(self):
        """Get a database connection from the pool."""
        conn = psycopg2.connect(**self.db_params)
        conn.autocommit = False
        return conn

    def safe_cache_update(self, cache_dict, key, value, lock):
        """Thread-safe cache update."""
        with lock:
            if key not in cache_dict:
                cache_dict[key] = value

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

    def process_kanji_batch_parallel(self, kanji_batch_data):
        """Process a batch of kanji in parallel."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        try:
            processed = 0
            for character_data in kanji_batch_data:
                character = character_data.get('literal', '')
                if not character:
                    continue
                
                misc = character_data.get('misc', {})
                
                # Get JLPT levels
                jlpt_old = None
                jlpt_new = None
                if character in self.kanji_jlpt_mapping:
                    jlpt_old = self.kanji_jlpt_mapping[character]['jlpt_old']
                    jlpt_new = self.kanji_jlpt_mapping[character]['jlpt_new']
                
                stroke_count = misc.get('strokeCounts', [0])[0] if misc.get('strokeCounts') else 0
                
                # Insert kanji
                cursor.execute("""
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
                """, (character, misc.get('grade'), stroke_count, misc.get('frequency'), jlpt_old, jlpt_new))
                
                result = cursor.fetchone()
                if result:
                    kanji_id, _ = result
                    self.safe_cache_update(self.kanji_cache, character, kanji_id, self.kanji_cache_lock)
                
                # Process sub-items
                self._process_kanji_subitems_inline(cursor, character_data, character, kanji_id)
                
                processed += 1
                if processed % 100 == 0:
                    conn.commit()
                    sys.stdout.flush()
            
            conn.commit()
            return len(kanji_batch_data)
            
        except Exception as e:
            print(f"Error in kanji batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()
    
    def _process_kanji_subitems_inline(self, cursor, character_data, character, kanji_id):
        """Process kanji sub-items inline (not batched, for parallel processing)."""
        # Codepoints
        for codepoint in character_data.get('codepoints', []):
            cursor.execute("""
                INSERT INTO jlpt.kanji_codepoint (kanji_id, type, value)
                VALUES (%s, %s, %s)
                ON CONFLICT DO NOTHING
            """, (kanji_id, codepoint.get('type', ''), codepoint.get('value', '')))
        
        # Dictionary references
        for ref in character_data.get('dictionaryReferences', []):
            morohashi = ref.get('morohashi') or {}
            cursor.execute("""
                INSERT INTO jlpt.kanji_dictionary_reference (
                    kanji_id, type, value, morohashi_volume, morohashi_page
                ) VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, (kanji_id, ref.get('type', ''), ref.get('value', ''),
                  morohashi.get('volume') if morohashi else None,
                  morohashi.get('page') if morohashi else None))
        
        # Query codes
        for qc in character_data.get('queryCodes', []):
            cursor.execute("""
                INSERT INTO jlpt.kanji_query_code (
                    kanji_id, type, value, skip_missclassification
                ) VALUES (%s, %s, %s, %s)
                ON CONFLICT DO NOTHING
            """, (kanji_id, qc.get('type', ''), qc.get('value', ''),
                  qc.get('skipMisclassification')))
        
        # Readings and meanings
        reading_meaning = character_data.get('readingMeaning', {})
        if reading_meaning and 'groups' in reading_meaning:
            for group in reading_meaning['groups']:
                for reading in group.get('readings', []):
                    cursor.execute("""
                        INSERT INTO jlpt.kanji_reading (
                            kanji_id, type, value, status, on_type
                        ) VALUES (%s, %s, %s, %s, %s)
                        ON CONFLICT DO NOTHING
                    """, (kanji_id, reading.get('type', ''), reading.get('value', ''),
                          reading.get('status'), reading.get('onType')))
                
                for meaning in group.get('meanings', []):
                    cursor.execute("""
                        INSERT INTO jlpt.kanji_meaning (kanji_id, lang, value)
                        VALUES (%s, %s, %s)
                        ON CONFLICT DO NOTHING
                    """, (kanji_id, meaning.get('lang', ''), meaning.get('value', '')))
        
        # Nanori
        for nanori in character_data.get('nanori', []):
            cursor.execute("""
                INSERT INTO jlpt.kanji_nanori (kanji_id, value)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, (kanji_id, nanori))
    
    def process_kanji_data_parallel(self):
        """Process kanji data using parallel workers."""
        print(f"Processing kanji data with {NUM_WORKERS} workers...", flush=True)
        
        kanji_source_path = self.source_dir / "kanji" / "source.json"
        if not kanji_source_path.exists():
            print(f"Kanji source file not found: {kanji_source_path}", flush=True)
            return
        
        # Read all kanji into batches for parallel processing
        kanji_batches = []
        current_batch = []
        
        with open(kanji_source_path, 'rb') as f:
            characters = ijson.items(f, 'characters.item')
            
            for character_data in characters:
                current_batch.append(character_data)
                
                if len(current_batch) >= BATCH_SIZE:
                    kanji_batches.append(current_batch)
                    current_batch = []
            
            if current_batch:
                kanji_batches.append(current_batch)
        
        print(f"Split {sum(len(b) for b in kanji_batches)} kanji into {len(kanji_batches)} batches", flush=True)
        
        # Process batches in parallel
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = []
            for batch in kanji_batches:
                future = executor.submit(self.process_kanji_batch_parallel, batch)
                futures.append(future)
            
            completed = 0
            for future in as_completed(futures):
                try:
                    count = future.result()
                    completed += count
                    print(f"Progress: {completed}/{sum(len(b) for b in kanji_batches)} kanji processed", flush=True)
                except Exception as e:
                    print(f"Batch processing error: {e}", flush=True)
        
        print(f"Kanji processing complete: {completed} total", flush=True)
    
    def process_vocabulary_batch_parallel(self, vocab_batch_data):
        """Process a batch of vocabulary in parallel."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        local_pending_vocab_relations = []

        try:
            processed = 0
            for word_data in vocab_batch_data:
                jmdict_id = word_data.get('id', '')
                if not jmdict_id:
                    continue
                
                # Get JLPT level
                jlpt_new = self._get_vocab_jlpt_level(word_data)
                
                # Insert vocabulary
                cursor.execute("""
                    INSERT INTO jlpt.vocabulary (jlpt_level_new)
                    VALUES (%s)
                    RETURNING id
                """, (jlpt_new,))
                
                vocabulary_id = cursor.fetchone()[0]
                self.safe_cache_update(self.vocabulary_cache, jmdict_id, vocabulary_id, self.vocab_cache_lock)
                
                # Process forms and senses
                self._process_vocab_forms(cursor, vocabulary_id, word_data)
                self._process_vocab_senses(cursor, vocabulary_id, word_data, local_pending_vocab_relations)
                
                processed += 1
                if processed % 100 == 0:
                    conn.commit()
                    sys.stdout.flush()
            
            conn.commit()
            return len(vocab_batch_data), local_pending_vocab_relations
            
        except Exception as e:
            print(f"Error in vocabulary batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()
    
    def process_vocabulary_data_parallel(self):
        """Process vocabulary data using parallel workers."""
        print(f"Processing vocabulary data with {NUM_WORKERS} workers...", flush=True)
        
        vocab_source_path = self.source_dir / "vocabulary" / "source.json"
        if not vocab_source_path.exists():
            print(f"Vocabulary source file not found: {vocab_source_path}", flush=True)
            return
        
        # Read all vocabulary into batches
        vocab_batches = []
        current_batch = []
        
        with open(vocab_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for word_data in words:
                current_batch.append(word_data)
                
                if len(current_batch) >= BATCH_SIZE:
                    vocab_batches.append(current_batch)
                    current_batch = []
            
            if current_batch:
                vocab_batches.append(current_batch)
        
        print(f"Split {sum(len(b) for b in vocab_batches)} vocabulary into {len(vocab_batches)} batches", flush=True)
        
        self.pending_vocab_relations.clear()

        # Process batches in parallel
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = []
            for batch in vocab_batches:
                future = executor.submit(self.process_vocabulary_batch_parallel, batch)
                futures.append(future)
            
            completed = 0
            for future in as_completed(futures):
                try:
                    count, local_pending_relations = future.result()
                    completed += count

                    if local_pending_relations:
                        self.pending_vocab_relations.extend(local_pending_relations)

                    print(f"Progress: {completed}/{sum(len(b) for b in vocab_batches)} vocabulary processed", flush=True)
                except Exception as e:
                    print(f"Batch processing error: {e}", flush=True)
        
        print(f"Vocabulary processing complete: {completed} total", flush=True)
        print(f"Collected {len(self.pending_vocab_relations)} pending vocab relations", flush=True)

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

    def _process_vocab_senses(self, cursor, vocabulary_id, word_data, pending_relations_list):
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
            self._process_sense_attributes(cursor, sense_id, sense, pending_relations_list)

    def _process_sense_attributes(self, cursor, sense_id, sense, pending_relations_list):
        """Process attributes for a sense."""
        # Part of speech
        pos_batch = []
        for pos in sense.get('partOfSpeech', []):
            self.ensure_tag_exists(cursor, pos, 'part_of_speech')
            pos_batch.append((sense_id, pos))
        if pos_batch:
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_pos (sense_id, tag_code)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, pos_batch)
        
        # Field tags
        field_batch = [(sense_id, field) for field in sense.get('field', [])]
        if field_batch:
            for _, field in field_batch:
                self.ensure_tag_exists(cursor, field, 'field')
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_field (sense_id, tag_code)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, field_batch)
        
        # Dialect tags
        dialect_batch = [(sense_id, d) for d in sense.get('dialect', [])]
        if dialect_batch:
            for _, d in dialect_batch:
                self.ensure_tag_exists(cursor, d, 'dialect')
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_dialect (sense_id, tag_code)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, dialect_batch)
        
        # Misc tags
        misc_batch = [(sense_id, m) for m in sense.get('misc', [])]
        if misc_batch:
            for _, m in misc_batch:
                self.ensure_tag_exists(cursor, m, 'misc')
            cursor.executemany("""
                INSERT INTO jlpt.vocabulary_sense_misc (sense_id, tag_code)
                VALUES (%s, %s)
                ON CONFLICT DO NOTHING
            """, misc_batch)
        
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
            term = related.get('term')
            if term:
                reading = related.get('reading')
                sense_index = related.get('sense')  # 1-based index from JSON
                pending_relations_list.append((
                    sense_id, term, reading, sense_index, 'related'
                ))

        # Antonyms - store for later resolution
        for antonym in sense.get('antonym', []):
            term = antonym.get('term')
            if term:
                reading = antonym.get('reading')
                sense_index = antonym.get('sense')  # 1-based index from JSON
                pending_relations_list.append((
                    sense_id, term, reading, sense_index, 'antonym'
                ))

    def pre_populate_tags(self):
        """
        Scans all source files, extracts all unique tags,
        and inserts them into the database in a single batch.
        This prevents race conditions during parallel processing.
        """
        print("Pre-populating tag table...", flush=True)
        
        # 1. Load descriptions first
        if not hasattr(self, '_tag_descriptions'):
            self._load_tag_descriptions()
            
        all_tags = set() # Set of (tag_code, category)
        
        # 2. Scan vocabulary file
        vocab_source_path = self.source_dir / "vocabulary" / "source.json"
        if vocab_source_path.exists():
            print(f"Scanning {vocab_source_path.name} for tags...", flush=True)
            with open(vocab_source_path, 'rb') as f:
                words = ijson.items(f, 'words.item')
                for word_data in words:
                    for kanji in word_data.get('kanji', []):
                        for tag in kanji.get('tags', []):
                            all_tags.add((tag, 'kanji'))
                    for kana in word_data.get('kana', []):
                        for tag in kana.get('tags', []):
                            all_tags.add((tag, 'kana'))
                    for sense in word_data.get('sense', []):
                        for tag in sense.get('partOfSpeech', []):
                            all_tags.add((tag, 'part_of_speech'))
                        for tag in sense.get('field', []):
                            all_tags.add((tag, 'field'))
                        for tag in sense.get('dialect', []):
                            all_tags.add((tag, 'dialect'))
                        for tag in sense.get('misc', []):
                            all_tags.add((tag, 'misc'))
        
        # 3. Scan names file (for proper_noun tags)
        names_source_path = self.source_dir / "names" / "source.json"
        if names_source_path.exists():
            print(f"Scanning {names_source_path.name} for tags...", flush=True)
            with open(names_source_path, 'rb') as f:
                words = ijson.items(f, 'words.item')
                for name_data in words:
                    for kanji in name_data.get('kanji', []):
                        for tag in kanji.get('tags', []):
                            all_tags.add((tag, 'proper_noun'))
                    for kana in name_data.get('kana', []):
                        for tag in kana.get('tags', []):
                            all_tags.add((tag, 'proper_noun'))
                    for trans in name_data.get('translation', []):
                        for tag in trans.get('type', []):
                            all_tags.add((tag, 'translation_type'))
                            
        print(f"Found {len(all_tags)} unique tags.", flush=True)

        # 4. Insert all tags into the database
        conn = None
        try:
            conn = self.get_db_connection()
            cursor = conn.cursor()
            
            tag_batch = []
            for tag_code, category in all_tags:
                description = self._tag_descriptions.get(tag_code, f'{category} tag')
                tag_batch.append((tag_code, description, category))
                
                # Also populate the cache so workers don't need to check DB
                self.tag_cache[tag_code] = True

            if tag_batch:
                print(f"Inserting {len(tag_batch)} tags into jlpt.tag...", flush=True)
                cursor.executemany("""
                    INSERT INTO jlpt.tag (code, description, category)
                    VALUES (%s, %s, %s)
                    ON CONFLICT (code) DO NOTHING
                """, tag_batch)
            
            conn.commit()
            print("Tag pre-population complete.", flush=True)
            
        except Exception as e:
            print(f"Error during tag pre-population: {e}", flush=True)
            if conn:
                conn.rollback()
            raise # This is a critical failure, so re-raise
        finally:
            if conn:
                cursor.close()
                conn.close()

    def ensure_tag_exists(self, cursor, tag_code: str, category: str):
        """
        Ensure a tag exists in the database.
        
        NOTE: With the pre-population step, this method
        is effectively a no-op. It's left in place
        to avoid changing all the worker methods.
        We can add a warning if a tag is missing,
        which indicates a flaw in the pre-population logic.
        """
        if tag_code not in self.tag_cache:
            # This should not happen if pre-population is correct
            print(f"WARNING: Tag '{tag_code}' (category: {category}) "
                  f"was not found in the pre-populated cache!", flush=True)
            # You could add a fallback insert here, but it's better
            # to fix the pre-population logic if this warning appears.
        
        return

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

    def process_radical_batch_parallel(self, radical_batch):
        """Process a batch of radicals from radfile."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        try:
            for radical_data in radical_batch:
                cursor.execute("""
                    INSERT INTO jlpt.radical (literal, stroke_count, code)
                    VALUES (%s, %s, %s)
                    ON CONFLICT (literal) DO UPDATE SET
                        stroke_count = EXCLUDED.stroke_count,
                        code = EXCLUDED.code,
                        updated_at = CURRENT_TIMESTAMP
                    RETURNING id, literal
                """, radical_data)
                result = cursor.fetchone()
                if result:
                    radical_id, radical_char = result
                    with self.radical_cache_lock:
                        self.radical_cache[radical_char] = radical_id
            
            conn.commit()
            return len(radical_batch)
        
        except Exception as e:
            print(f"Error in radical batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()

    def process_krad_batch_parallel(self, krad_batch):
        """Process a batch of kanji-radical relationships from kradfile."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        relationship_batch = []
        
        try:
            for kanji_char, components in krad_batch:
                kanji_id = self.kanji_cache.get(kanji_char)
                if not kanji_id:
                    continue
                
                for component in components:
                    radical_id = self.radical_cache.get(component)
                    if radical_id:
                        relationship_batch.append((kanji_id, radical_id))
            
            if relationship_batch:
                cursor.executemany("""
                    INSERT INTO jlpt.kanji_radical (kanji_id, radical_id)
                    VALUES (%s, %s)
                    ON CONFLICT DO NOTHING
                """, relationship_batch)
            
            conn.commit()
            return len(krad_batch)

        except Exception as e:
            print(f"Error in krad batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()    

    def process_radical_data_parallel(self):
        """Process radical data from radfile and kradfile."""
        print(f"Processing radical data with {NUM_WORKERS} workers...", flush=True)
        
        # === Phase 1: Process radfile (populate radical table and cache) ===
        print("Radical Phase 1: Processing radfile...", flush=True)
        radfile_path = self.source_dir / "radfile" / "source.json"
        if not radfile_path.exists():
            print(f"Radfile source not found: {radfile_path}", flush=True)
            return

        radical_batches = []
        current_batch = []

        with open(radfile_path, 'rb') as f:
            radicals = ijson.kvitems(f, 'radicals')
            total_radicals = 0
            for radical_char, radical_data in radicals:
                current_batch.append((
                    radical_char,
                    radical_data.get('strokeCount', 0),
                    radical_data.get('code')
                ))
                total_radicals += 1
                
                if len(current_batch) >= BATCH_SIZE:
                    radical_batches.append(current_batch)
                    current_batch = []
            
            if current_batch:
                radical_batches.append(current_batch)
        
        print(f"Split {total_radicals} radicals into {len(radical_batches)} batches", flush=True)
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = [executor.submit(self.process_radical_batch_parallel, batch) for batch in radical_batches]
            
            completed = 0
            for future in as_completed(futures):
                try:
                    count = future.result()
                    completed += count
                    print(f"Radical Phase 1 Progress: {completed}/{total_radicals} radicals processed", flush=True)
                except Exception as e:
                    print(f"Radical batch processing error: {e}", flush=True)
        
        print("Radical Phase 1 complete.", flush=True)
        # === Phase 2: Process kradfile (link kanji to radicals) ===
        print("Radical Phase 2: Processing kradfile...", flush=True)
        kradfile_path = self.source_dir / "kradfile" / "source.json"
        if not kradfile_path.exists():
            print(f"Kradfile source not found: {kradfile_path}", flush=True)
            return
            
        krad_batches = []
        current_batch = []
        
        with open(kradfile_path, 'rb') as f:
            kanji_items = ijson.kvitems(f, 'kanji')
            total_krad = 0
            for kanji_char, components in kanji_items:
                current_batch.append((kanji_char, components))
                total_krad += 1
                
                if len(current_batch) >= BATCH_SIZE:
                    krad_batches.append(current_batch)
                    current_batch = []
            
            if current_batch:
                krad_batches.append(current_batch)
        
        print(f"Split {total_krad} krad entries into {len(krad_batches)} batches", flush=True)
        
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = [executor.submit(self.process_krad_batch_parallel, batch) for batch in krad_batches]
            
            completed = 0
            for future in as_completed(futures):
                try:
                    count = future.result()
                    completed += count
                    print(f"Radical Phase 2 Progress: {completed}/{total_krad} krad entries processed", flush=True)
                except Exception as e:
                    print(f"Krad batch processing error: {e}", flush=True)

        print("Radical processing complete.", flush=True)

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

    def process_proper_nouns_batch_parallel(self, name_batch_data):
        """Process a batch of vocabulary in parallel."""
        conn = self.get_db_connection()
        cursor = conn.cursor()

        local_pending_proper_nouns_relations = []

        try:
            processed = 0
            for name_data in name_batch_data:
                jmdict_id = name_data.get('id', '')
                if not jmdict_id:
                    continue
                
                # Insert proper noun
                cursor.execute("""
                    INSERT INTO jlpt.proper_noun DEFAULT VALUES
                    RETURNING id
                """)

                proper_noun_id = cursor.fetchone()[0]

                # Process proper noun forms, translations and relationships
                self._process_proper_noun_forms(cursor, proper_noun_id, name_data)
                self._process_proper_noun_translations(cursor, proper_noun_id, name_data, local_pending_proper_nouns_relations)
                self._process_proper_noun_kanji_relationships(cursor, proper_noun_id, name_data)

                processed += 1
                if processed % 100 == 0:
                    conn.commit()
                    sys.stdout.flush()
            conn.commit()
            return len(name_batch_data), local_pending_proper_nouns_relations

        except Exception as e:
            print(f"Error in proper noun batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()

    def process_proper_nouns_parallel(self):
        """Process proper noun data using parallel workers."""
        print(f"Processing proper nouns data with {NUM_WORKERS} workers...", flush=True)

        names_source_path = self.source_dir / "names" / "source.json"
        if not names_source_path.exists():
            print(f"Names source file not found: {names_source_path}")
            return

        # Read all proper nouns into batches
        names_batches = []
        current_batch = []

        with open(names_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for name_data in words:
                current_batch.append(name_data)
                if len(current_batch) >= BATCH_SIZE:
                    names_batches.append(current_batch)
                    current_batch = []
                    
            if current_batch:
                names_batches.append(current_batch)

        print(f"Split {sum(len(b) for b in names_batches)} proper nouns into {len(names_batches)} batches", flush=True)
        
        self.pending_proper_noun_relations.clear()

        # Process batches in parallel
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = []
            for batch in names_batches:
                future = executor.submit(self.process_proper_nouns_batch_parallel, batch)
                futures.append(future)

            completed = 0
            for future in as_completed(futures):
                try:
                    count, local_pending_proper_nouns_relations = future.result()
                    completed += count

                    if local_pending_proper_nouns_relations:
                        self.pending_proper_noun_relations.extend(local_pending_proper_nouns_relations)

                    print(f"Progress: {completed}/{sum(len(b) for b in names_batches)} proper nouns processed", flush=True)
                except Exception as e:
                    print(f"Batch processing error: {e}", flush=True)

        print(f"Proper nouns processing complete: {completed} total", flush=True)
        print(f"Collected {len(self.pending_proper_noun_relations)} pending proper noun relations", flush=True)

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

    def _process_proper_noun_translations(self, cursor, proper_noun_id, name_data, pending_relations_list):
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
            for related in trans.get('related', []):
                term = related.get('term')
                if term:
                    reading = related.get('reading')
                    sense_index = related.get('sense')  # Can be null
                    pending_relations_list.append((
                        translation_id, term, reading, sense_index
                    ))
            
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

    def process_vocabulary_examples_batch_parallel(self, example_batch_data):
        """Process a batch of vocabulary examples in parallel."""
        conn = self.get_db_connection()
        cursor = conn.cursor()

        try:
            processed = 0
            for word_data in example_batch_data:
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
                processed += 1
                if processed % 100 == 0:
                    conn.commit()
                    sys.stdout.flush()
            conn.commit()
            return len(example_batch_data)
        except Exception as e:
            print(f"Error in vocabulary example batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()


    def process_vocabulary_examples_parallel(self):
        """Process vocabulary examples data using parallel workers."""
        print(f"Processing vocabulary examples data with {NUM_WORKERS} workers...", flush=True)
        
        examples_source_path = self.source_dir / "vocabulary" / "vocabularyWithExamples" / "source.json"
        if not examples_source_path.exists():
            print(f"Vocabulary examples source file not found: {examples_source_path}")
            return
        
        examples_batches = []
        current_batch = []

        with open(examples_source_path, 'rb') as f:
            words = ijson.items(f, 'words.item')
            
            for word_data in words:
                current_batch.append(word_data)

                if len(current_batch) >= BATCH_SIZE:
                    examples_batches.append(current_batch)
                    current_batch = []
            if current_batch:
                examples_batches.append(current_batch)

        print(f"Split {sum(len(b) for b in examples_batches)} vocabulary examples into {len(examples_batches)} batches", flush=True)

        # Process batches in parallel
        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = []
            for batch in examples_batches:
                future = executor.submit(self.process_vocabulary_examples_batch_parallel, batch)
                futures.append(future)
            
            completed = 0
            for future in as_completed(futures):
                try:
                    count = future.result()
                    completed += count
                    print(f"Progress: {completed}/{sum(len(b) for b in examples_batches)} vocabulary example processed", flush=True)
                except Exception as e:
                    print(f"Batch processing error: {e}", flush=True)
        
        print(f"Vocabulary example processing complete: {completed} total", flush=True)

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

    def resolve_vocabulary_relations_batch(self, relation_batch):
        """Resolves a batch of pending vocabulary relationships."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        resolved_batch = []
        unresolved_count = 0
        
        try:
            for source_sense_id, target_term, target_reading, target_sense_index, relation_type in relation_batch:
                target_vocab_id = None
                target_sense_id = None
                
                if target_reading:
                    target_vocab_id = self.vocab_term_cache.get((target_term, target_reading))
                if not target_vocab_id:
                    target_vocab_id = self.vocab_term_cache.get((target_term, None))
                
                if target_vocab_id and target_sense_index is not None:
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
                
                resolved_batch.append((
                    source_sense_id, target_vocab_id, target_sense_id,
                    target_term, target_reading, relation_type
                ))
                
                if not target_vocab_id:
                    unresolved_count += 1
            
            if resolved_batch:
                cursor.executemany("""
                    INSERT INTO jlpt.vocabulary_sense_relation (
                        source_sense_id, target_vocab_id, target_sense_id, 
                        target_term, target_reading, relation_type
                    ) VALUES (%s, %s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, resolved_batch)
            
            conn.commit()
            return len(relation_batch), unresolved_count
        except Exception as e:
            print(f"Error in vocab relation batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()

    def resolve_vocabulary_relations_parallel(self):
        """Resolve pending vocabulary relationships."""
        total_relations = len(self.pending_vocab_relations)
        print(f"Resolving {total_relations} vocabulary relationships...", flush=True)        
        
        if total_relations == 0:
            return

        chunk_size = (total_relations + NUM_WORKERS - 1) // NUM_WORKERS
        relation_chunks = [
            self.pending_vocab_relations[i:i + chunk_size]
            for i in range(0, total_relations, chunk_size)
        ]

        print(f"Split relations into {len(relation_chunks)} chunks of ~{chunk_size}", flush=True)       
        
        total_processed = 0
        total_unresolved = 0

        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = [executor.submit(self.resolve_vocabulary_relations_batch, chunk) for chunk in relation_chunks]
            
            for future in as_completed(futures):
                try:
                    processed, unresolved = future.result()
                    total_processed += processed
                    total_unresolved += unresolved
                    print(f"Progress: {total_processed}/{total_relations} vocab relations resolved", flush=True)
                except Exception as e:
                    print(f"Vocab relation batch processing error: {e}", flush=True)
        
        print(f"Vocabulary relationships resolved: {total_processed - total_unresolved} resolved, {total_unresolved} unresolved", flush=True)
        self.pending_vocab_relations.clear()

    def resolve_proper_noun_relations_batch(self, relation_batch):
        """Resolves a batch of pending proper noun relationships."""
        conn = self.get_db_connection()
        cursor = conn.cursor()
        
        resolved_batch = []
        unresolved_count = 0
        
        try:
            for translation_id, related_term, related_reading, related_sense_index in relation_batch:
                reference_proper_noun_id = None
                reference_translation_id = None
                
                if related_reading:
                    reference_proper_noun_id = self.proper_noun_term_cache.get((related_term, related_reading))
                if not reference_proper_noun_id:
                    reference_proper_noun_id = self.proper_noun_term_cache.get((related_term, None))
                
                if reference_proper_noun_id and related_sense_index is not None:
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
                
                resolved_batch.append((
                    translation_id, related_term, related_reading,
                    reference_proper_noun_id, reference_translation_id
                ))
                
                if not reference_proper_noun_id:
                    unresolved_count += 1
            
            if resolved_batch:
                cursor.executemany("""
                    INSERT INTO jlpt.proper_noun_translation_related (
                        translation_id, related_term, related_reading,
                        reference_proper_noun_id, reference_proper_noun_translation_id
                    ) VALUES (%s, %s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """, resolved_batch)
            
            conn.commit()
            return len(relation_batch), unresolved_count

        except Exception as e:
            print(f"Error in proper noun relation batch: {e}", flush=True)
            conn.rollback()
            raise
        finally:
            cursor.close()
            conn.close()

    def resolve_proper_noun_relations_parallel(self):
        """Resolve pending proper noun relationships."""
        total_relations = len(self.pending_proper_noun_relations)
        print(f"Resolving {total_relations} proper noun relationships...", flush=True)
        
        if total_relations == 0:
            return

        chunk_size = (total_relations + NUM_WORKERS - 1) // NUM_WORKERS
        relation_chunks = [
            self.pending_proper_noun_relations[i:i + chunk_size]
            for i in range(0, total_relations, chunk_size)
        ]
        
        print(f"Split proper noun relations into {len(relation_chunks)} chunks of ~{chunk_size}", flush=True)

        total_processed = 0
        total_unresolved = 0

        with ThreadPoolExecutor(max_workers=NUM_WORKERS) as executor:
            futures = [executor.submit(self.resolve_proper_noun_relations_batch, chunk) for chunk in relation_chunks]
            
            for future in as_completed(futures):
                try:
                    processed, unresolved = future.result()
                    total_processed += processed
                    total_unresolved += unresolved
                    print(f"Progress: {total_processed}/{total_relations} proper noun relations resolved", flush=True)
                except Exception as e:
                    print(f"Proper noun relation batch processing error: {e}", flush=True)

        print(f"Proper noun relationships resolved: {total_processed - total_unresolved} resolved, {total_unresolved} unresolved", flush=True)
        self.pending_proper_noun_relations.clear()

    def process_all_data_parallel(self):
        """Process all data with parallelization where beneficial."""
        print("Starting parallel data processing...", flush=True)
        print(f"Using {NUM_WORKERS} worker threads", flush=True)
        print(f"Batch size: {BATCH_SIZE}", flush=True)
        
        if not self.wait_for_database():
            return False
        
        self.load_jlpt_mappings()
        self.pre_populate_tags()
        
        try:
            print("\n=== Step 1: Processing kanji data (PARALLEL) ===", flush=True)
            self.process_kanji_data_parallel()
            
            print("\n=== Step 2: Processing vocabulary data (PARALLEL) ===", flush=True)
            self.process_vocabulary_data_parallel()
            
            print("\n=== Step 3: Processing vocabulary examples (PARALLEL) ===", flush=True)
            self.process_vocabulary_examples_parallel()
            
            print("\n=== Step 4: Processing radical data ===", flush=True)
            self.process_radical_data_parallel()
            
            print("\n=== Step 5: Processing proper nouns (PARALLEL) ===", flush=True)
            self.process_proper_nouns_parallel()
            
            print("\n=== Step 6: Processing kanji-vocabulary relationships ===", flush=True)
            conn = self.get_db_connection()
            cursor = conn.cursor()
            self.process_kanji_vocabulary_relationships(conn, cursor)
            
            print("\n=== Step 7: Building term lookup caches ===", flush=True)
            self.build_vocabulary_term_cache(cursor)
            self.build_proper_noun_term_cache(cursor)
            
            # Close this connection, workers will use their own
            cursor.close()
            conn.close()

            print("\n=== Step 8: Resolving vocabulary relationships ===", flush=True)
            self.resolve_vocabulary_relations_parallel()
            
            print("\n=== Step 9: Resolving proper noun relationships ===", flush=True)
            self.resolve_proper_noun_relations_parallel()
            
            print("\n=== All data processing completed successfully! ===", flush=True)
                        
            # Print statistics
            conn = self.get_db_connection()
            cursor = conn.cursor()
            print("\n=== Database Statistics ===", flush=True)
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
            
            print(f"- Kanji entries: {kanji_count:,}", flush=True)
            print(f"- Vocabulary entries: {vocab_count:,}", flush=True)
            print(f"- Vocabulary examples: {examples_count:,}", flush=True)
            print(f"- Radicals: {radicals_count:,}", flush=True)
            print(f"- Proper nouns: {proper_nouns_count:,}", flush=True)
            print(f"- Kanji-vocabulary relationships: {relationships_count:,}", flush=True)
            print("=" * 40, flush=True)
            
            cursor.close()
            conn.close()
            
            return True
            
        except Exception as e:
            print(f"\nError during processing: {e}", flush=True)
            import traceback
            traceback.print_exc()
            return False

def main():
    """Main function."""
    print("=" * 60)
    print("JLPT Reference Database - Data Processor")
    print("=" * 60)
    
    processor = ParallelJLPTDataProcessor()
    
    if not processor.process_all_data_parallel():
        sys.exit(1)
    
    print("\nData processing completed successfully!")

if __name__ == "__main__":
    main()