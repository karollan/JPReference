#!/usr/bin/env python3
"""
Comprehensive Data Processor for JLPT Reference Database

This script processes all data sources:
1. Kanji data from kanjidic2
2. Vocabulary data from JMdict
3. Vocabulary with examples
4. Radical and kradfile data
5. JLPT level mapping from reference files
"""

import json
import os
import sys
import time
import psycopg2
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple

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

    def load_jlpt_mappings(self):
        """Load JLPT level mappings from reference files."""
        print("Loading JLPT level mappings...")
        
        # Load kanji JLPT mapping
        kanji_ref_path = self.source_dir / "kanji" / "reference.json"
        if kanji_ref_path.exists():
            with open(kanji_ref_path, 'r', encoding='utf-8') as f:
                kanji_ref = json.load(f)
                for character, data in kanji_ref.items():
                    self.kanji_jlpt_mapping[character] = {
                        'jlpt_old': data.get('jlpt_old'),
                        'jlpt_new': data.get('jlpt_new')
                    }
            print(f"Loaded JLPT mapping for {len(self.kanji_jlpt_mapping)} kanji")
        
        # Load vocabulary JLPT mapping
        vocab_ref_path = self.source_dir / "vocabulary" / "reference.json"
        if vocab_ref_path.exists():
            with open(vocab_ref_path, 'r', encoding='utf-8') as f:
                vocab_ref = json.load(f)
                for word, jlpt_level in vocab_ref.items():
                    # Convert N1, N2, etc. to numeric values
                    jlpt_numeric = None
                    if jlpt_level.startswith('N'):
                        jlpt_numeric = int(jlpt_level[1:])
                    self.vocabulary_jlpt_mapping[word] = jlpt_numeric
            print(f"Loaded JLPT mapping for {len(self.vocabulary_jlpt_mapping)} vocabulary entries")

    def process_kanji_data(self, cursor):
        """Process kanji data from kanjidic2 source."""
        print("Processing kanji data...")
        
        kanji_source_path = self.source_dir / "kanji" / "source.json"
        if not kanji_source_path.exists():
            print(f"Kanji source file not found: {kanji_source_path}")
            return
        
        with open(kanji_source_path, 'r', encoding='utf-8') as f:
            kanji_data = json.load(f)
        
        # Process each kanji character
        for character_data in kanji_data.get('characters', []):
            character = character_data.get('literal', '')
            if not character:
                continue
            
            # Extract basic information
            misc = character_data.get('misc', {})
            reading_meaning = character_data.get('readingMeaning', {})
            
            # Extract meanings
            meanings = []
            if reading_meaning and 'groups' in reading_meaning:
                for group in reading_meaning['groups']:
                    for meaning in group.get('meanings', []):
                        if meaning.get('lang') == 'en':
                            meanings.append(meaning.get('value', ''))
            
            # Extract readings
            readings_on = []
            readings_kun = []
            if reading_meaning and 'groups' in reading_meaning:
                for group in reading_meaning['groups']:
                    for reading in group.get('readings', []):
                        if reading.get('type') == 'ja_on':
                            readings_on.append(reading.get('value', ''))
                        elif reading.get('type') == 'ja_kun':
                            readings_kun.append(reading.get('value', ''))
            
            # Extract other data
            codepoints = [cp.get('value', '') for cp in character_data.get('codepoints', [])]
            radicals = [str(r.get('value', '')) for r in character_data.get('radicals', [])]
            variants = [v.get('value', '') for v in misc.get('variants', [])]
            radical_names = misc.get('radicalNames', [])
            nanori = character_data.get('nanori', [])
            
            # Dictionary references
            dict_refs = []
            for ref in character_data.get('dictionaryReferences', []):
                dict_refs.append(f"{ref.get('type', '')}:{ref.get('value', '')}")
            
            # Query codes
            query_codes = []
            for qc in character_data.get('queryCodes', []):
                query_codes.append(f"{qc.get('type', '')}:{qc.get('value', '')}")
            
            # Get JLPT levels from mapping
            jlpt_old = None
            jlpt_new = None
            if character in self.kanji_jlpt_mapping:
                jlpt_old = self.kanji_jlpt_mapping[character]['jlpt_old']
                jlpt_new = self.kanji_jlpt_mapping[character]['jlpt_new']
            
            # Insert kanji
            insert_kanji_sql = """
                INSERT INTO jlpt.kanji (
                    character, meanings, readings_on, readings_kun, stroke_count, 
                    grade, frequency, jlpt_old, jlpt_new, codepoints, radicals, 
                    variants, radical_names, dictionary_references, query_codes, nanori
                ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                ON CONFLICT (character) DO UPDATE SET
                    meanings = EXCLUDED.meanings,
                    readings_on = EXCLUDED.readings_on,
                    readings_kun = EXCLUDED.readings_kun,
                    stroke_count = EXCLUDED.stroke_count,
                    grade = EXCLUDED.grade,
                    frequency = EXCLUDED.frequency,
                    jlpt_old = EXCLUDED.jlpt_old,
                    jlpt_new = EXCLUDED.jlpt_new,
                    codepoints = EXCLUDED.codepoints,
                    radicals = EXCLUDED.radicals,
                    variants = EXCLUDED.variants,
                    radical_names = EXCLUDED.radical_names,
                    dictionary_references = EXCLUDED.dictionary_references,
                    query_codes = EXCLUDED.query_codes,
                    nanori = EXCLUDED.nanori,
                    updated_at = CURRENT_TIMESTAMP
                RETURNING id
            """
            
            cursor.execute(insert_kanji_sql, (
                character, meanings, readings_on, readings_kun,
                misc.get('strokeCounts', [0])[0] if misc.get('strokeCounts') else 0,
                misc.get('grade'), misc.get('frequency'), jlpt_old, jlpt_new,
                codepoints, radicals, variants, radical_names, dict_refs, query_codes, nanori
            ))
            
            kanji_id = cursor.fetchone()[0]
            
            # Insert radicals
            for radical in character_data.get('radicals', []):
                insert_radical_sql = """
                    INSERT INTO jlpt.kanji_radicals (kanji_id, radical, stroke_count, code)
                    VALUES (%s, %s, %s, %s)
                    ON CONFLICT DO NOTHING
                """
                cursor.execute(insert_radical_sql, (
                    kanji_id, str(radical.get('value', '')), 
                    radical.get('value', 0), None
                ))

    def process_vocabulary_data(self, cursor):
        """Process vocabulary data from JMdict source."""
        print("Processing vocabulary data...")
        
        vocab_source_path = self.source_dir / "vocabulary" / "source.json"
        if not vocab_source_path.exists():
            print(f"Vocabulary source file not found: {vocab_source_path}")
            return
        
        with open(vocab_source_path, 'r', encoding='utf-8') as f:
            vocab_data = json.load(f)
        
        # Process each vocabulary entry
        for word_data in vocab_data.get('words', []):
            jmdict_id = word_data.get('id', '')
            if not jmdict_id:
                continue
            
            # Extract kanji and kana
            kanji_list = [k.get('text', '') for k in word_data.get('kanji', [])]
            kana_list = [k.get('text', '') for k in word_data.get('kana', [])]
            
            # Extract sense information
            all_part_of_speech = []
            all_field = []
            all_dialect = []
            all_misc = []
            all_info = []
            all_language_source = []
            all_gloss = []
            all_gloss_languages = []
            all_related = []
            all_antonym = []
            
            for sense in word_data.get('sense', []):
                all_part_of_speech.extend(sense.get('partOfSpeech', []))
                all_field.extend(sense.get('field', []))
                all_dialect.extend(sense.get('dialect', []))
                all_misc.extend(sense.get('misc', []))
                all_info.extend(sense.get('info', []))
                all_language_source.extend([ls.get('text', '') for ls in sense.get('languageSource', [])])
                
                for gloss in sense.get('gloss', []):
                    all_gloss.append(gloss.get('text', ''))
                    all_gloss_languages.append(gloss.get('lang', ''))
                
                all_related.extend([str(rel) for rel in sense.get('related', [])])
                all_antonym.extend([str(ant) for ant in sense.get('antonym', [])])
            
            # Check if common
            is_common = any(k.get('common', False) for k in word_data.get('kanji', [])) or \
                      any(k.get('common', False) for k in word_data.get('kana', []))
            
            # Get JLPT level from mapping
            jlpt_old = None
            jlpt_new = None
            # Try to match with kanji or kana
            for kanji in kanji_list:
                if kanji in self.vocabulary_jlpt_mapping:
                    jlpt_new = self.vocabulary_jlpt_mapping[kanji]
                    break
            for kana in kana_list:
                if kana in self.vocabulary_jlpt_mapping:
                    jlpt_new = self.vocabulary_jlpt_mapping[kana]
                    break
            
            # Insert vocabulary
            insert_vocab_sql = """
                INSERT INTO jlpt.vocabulary (
                    jmdict_id, kanji, kana, part_of_speech, field, dialect, misc, 
                    info, language_source, gloss, gloss_languages, related, antonym, 
                    is_common, jlpt_old, jlpt_new
                ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                ON CONFLICT (jmdict_id) DO UPDATE SET
                    kanji = EXCLUDED.kanji,
                    kana = EXCLUDED.kana,
                    part_of_speech = EXCLUDED.part_of_speech,
                    field = EXCLUDED.field,
                    dialect = EXCLUDED.dialect,
                    misc = EXCLUDED.misc,
                    info = EXCLUDED.info,
                    language_source = EXCLUDED.language_source,
                    gloss = EXCLUDED.gloss,
                    gloss_languages = EXCLUDED.gloss_languages,
                    related = EXCLUDED.related,
                    antonym = EXCLUDED.antonym,
                    is_common = EXCLUDED.is_common,
                    jlpt_old = EXCLUDED.jlpt_old,
                    jlpt_new = EXCLUDED.jlpt_new,
                    updated_at = CURRENT_TIMESTAMP
                RETURNING id
            """
            
            cursor.execute(insert_vocab_sql, (
                jmdict_id, kanji_list, kana_list, all_part_of_speech, all_field,
                all_dialect, all_misc, all_info, all_language_source, all_gloss,
                all_gloss_languages, all_related, all_antonym, is_common, jlpt_old, jlpt_new
            ))

    def process_vocabulary_examples(self, cursor):
        """Process vocabulary examples data."""
        print("Processing vocabulary examples...")
        
        examples_source_path = self.source_dir / "vocabulary" / "vocabularyWithExamples" / "source.json"
        if not examples_source_path.exists():
            print(f"Vocabulary examples source file not found: {examples_source_path}")
            return
        
        with open(examples_source_path, 'r', encoding='utf-8') as f:
            examples_data = json.load(f)
        
        # Process each vocabulary entry with examples
        for word_data in examples_data.get('words', []):
            jmdict_id = word_data.get('id', '')
            if not jmdict_id:
                continue
            
            # Get vocabulary ID
            cursor.execute("SELECT id FROM jlpt.vocabulary WHERE jmdict_id = %s", (jmdict_id,))
            vocab_result = cursor.fetchone()
            if not vocab_result:
                continue
            
            vocab_id = vocab_result[0]
            
            # Process examples for each sense
            for sense in word_data.get('sense', []):
                for example in sense.get('examples', []):
                    source = example.get('source', {}).get('value', '')
                    text = example.get('text', '')
                    
                    japanese_sentences = []
                    english_sentences = []
                    
                    for sentence in example.get('sentences', []):
                        if sentence.get('land') == 'jpn':
                            japanese_sentences.append(sentence.get('text', ''))
                        elif sentence.get('land') == 'eng':
                            english_sentences.append(sentence.get('text', ''))
                    
                    # Insert example
                    insert_example_sql = """
                        INSERT INTO jlpt.vocabulary_examples (
                            vocabulary_id, source, text, japanese_sentences, english_sentences
                        ) VALUES (%s, %s, %s, %s, %s)
                        ON CONFLICT DO NOTHING
                    """
                    
                    cursor.execute(insert_example_sql, (
                        vocab_id, source, text, japanese_sentences, english_sentences
                    ))

    def process_radical_data(self, cursor):
        """Process radical data from radfile and kradfile."""
        print("Processing radical data...")
        
        # Process radfile
        radfile_path = self.source_dir / "radfile" / "source.json"
        if radfile_path.exists():
            with open(radfile_path, 'r', encoding='utf-8') as f:
                radfile_data = json.load(f)
            
            for radical_char, radical_data in radfile_data.get('radicals', {}).items():
                insert_radical_sql = """
                    INSERT INTO jlpt.radicals (character, stroke_count, code, kanji)
                    VALUES (%s, %s, %s, %s)
                    ON CONFLICT (character) DO UPDATE SET
                        stroke_count = EXCLUDED.stroke_count,
                        code = EXCLUDED.code,
                        kanji = EXCLUDED.kanji,
                        updated_at = CURRENT_TIMESTAMP
                """
                
                cursor.execute(insert_radical_sql, (
                    radical_char,
                    radical_data.get('strokeCount', 0),
                    radical_data.get('code'),
                    radical_data.get('kanji', [])
                ))
        
        # Process kradfile for kanji decompositions
        kradfile_path = self.source_dir / "kradfile" / "source.json"
        if kradfile_path.exists():
            with open(kradfile_path, 'r', encoding='utf-8') as f:
                kradfile_data = json.load(f)
            
            for kanji_char, components in kradfile_data.get('kanji', {}).items():
                # Get kanji ID
                cursor.execute("SELECT id FROM jlpt.kanji WHERE character = %s", (kanji_char,))
                kanji_result = cursor.fetchone()
                if not kanji_result:
                    continue
                
                kanji_id = kanji_result[0]
                
                # Insert decompositions
                for component in components:
                    insert_decomp_sql = """
                        INSERT INTO jlpt.kanji_decompositions (kanji_id, component)
                        VALUES (%s, %s)
                        ON CONFLICT DO NOTHING
                    """
                    
                    cursor.execute(insert_decomp_sql, (kanji_id, component))

    def process_all_data(self):
        """Process all data sources."""
        print("Starting comprehensive data processing...")
        
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
            # Process all data sources
            self.process_kanji_data(cursor)
            self.process_vocabulary_data(cursor)
            self.process_vocabulary_examples(cursor)
            self.process_radical_data(cursor)
            
            # Commit all changes
            conn.commit()
            print("All data processing completed successfully!")
            
            # Print statistics
            cursor.execute("SELECT COUNT(*) FROM jlpt.kanji")
            kanji_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.vocabulary")
            vocab_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.vocabulary_examples")
            examples_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM jlpt.radicals")
            radicals_count = cursor.fetchone()[0]
            
            print(f"Database statistics:")
            print(f"- Kanji entries: {kanji_count}")
            print(f"- Vocabulary entries: {vocab_count}")
            print(f"- Vocabulary examples: {examples_count}")
            print(f"- Radicals: {radicals_count}")
            
            return True
            
        except Exception as e:
            print(f"Error during processing: {e}")
            conn.rollback()
            return False
        finally:
            cursor.close()
            conn.close()

def main():
    """Main function."""
    processor = JLPTDataProcessor()
    
    if not processor.process_all_data():
        sys.exit(1)
    
    print("Comprehensive data processing completed successfully!")

if __name__ == "__main__":
    main()
