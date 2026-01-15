#!/usr/bin/env python3
"""Async Memory-Optimized Data Processor for JP Reference Database"""

import os
import sys
import asyncio
import tempfile
from pathlib import Path
from typing import Any, AsyncIterator, Dict, List, Optional, Tuple
from collections import defaultdict

import ijson
import asyncpg

try:
    import orjson
    def json_dumps(obj): return orjson.dumps(obj).decode()
    def json_loads(data): return orjson.loads(data)
except ImportError:
    import json
    def json_dumps(obj): return json.dumps(obj)
    def json_loads(data): return json.loads(data)

from cache_manager import DiskBackedCache
from spillable_list import SpillableList

# Configuration
BATCH_SIZE = int(os.getenv('BATCH_SIZE', '2000'))
MAX_CONCURRENT = int(os.getenv('MAX_CONCURRENT', '8'))
CACHE_DIR = os.getenv('CACHE_DIR', tempfile.gettempdir())

# Language code normalization
LANGUAGE_MAP = {
    'en': 'eng', 'de': 'ger', 'ru': 'rus', 'hu': 'hun',
    'nl': 'dut', 'es': 'spa', 'fr': 'fre', 'sv': 'swe',
    'sl': 'slv', 'pt': 'por', 'it': 'ita', 'ja': 'jpn'
}


def safe_print(text: str) -> None:
    """Safely print text that may contain Unicode characters."""
    try:
        print(text, flush=True)
    except UnicodeEncodeError:
        print(str(text).encode('ascii', 'replace').decode('ascii'), flush=True)


class AsyncJLPTDataProcessor:
    """Async data processor with memory and speed optimizations."""
    
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.source_dir = self.project_root / "source"
        
        # Database pool
        self.pool: Optional[asyncpg.Pool] = None
        
        # JLPT mappings (kept in memory - small enough)
        self.kanji_jlpt_mapping: Dict[str, dict] = {}
        self.vocabulary_jlpt_mapping: Dict[Tuple[str, str], int] = {}
        
        # ID caches (kept in memory for fast lookup)
        self.kanji_cache: Dict[str, int] = {}
        self.radical_cache: Dict[str, int] = {}
        self.vocabulary_cache: Dict[str, int] = {}
        self.tag_cache: set = set()
        
        # Disk-backed caches for large data
        self.vocab_furigana_cache: Optional[DiskBackedCache] = None
        self.proper_noun_furigana_cache: Optional[DiskBackedCache] = None
        
        # Spillable lists for pending relations
        self.pending_vocab_relations = SpillableList(threshold=50000)
        self.pending_proper_noun_relations = SpillableList(threshold=50000)
        
        # Tag descriptions
        self._tag_descriptions: Dict[str, str] = {}
        
        # Semaphore for concurrent batch processing
        self.semaphore = asyncio.Semaphore(MAX_CONCURRENT)

    async def init_pool(self) -> None:
        """Initialize the asyncpg connection pool."""
        self.pool = await asyncpg.create_pool(
            host=os.getenv('POSTGRES_HOST', 'localhost'),
            port=int(os.getenv('POSTGRES_PORT', '5432')),
            database=os.getenv('POSTGRES_DB', 'jlptreference'),
            user=os.getenv('POSTGRES_USER', 'jlptuser'),
            password=os.getenv('POSTGRES_PASSWORD', 'jlptpassword'),
            min_size=2,
            max_size=MAX_CONCURRENT + 2,
            command_timeout=300
        )
        safe_print(f"Database pool initialized (size: 2-{MAX_CONCURRENT + 2})")

    async def close_pool(self) -> None:
        """Close the connection pool."""
        if self.pool:
            await self.pool.close()
    
    def _normalize_radical_char(self, char: str) -> str:
        """Normalize look-alike radical characters."""
        norm_map = {
            '｜': '丨', '|': '丨', 'ノ': '丿', 'ハ': '八',
            'ト': '卜', 'ヨ': '彐', 'ユ': '彐', 'マ': '龴', 'ム': '厶'
        }
        return norm_map.get(char, char)

    # ========== Streaming Batch Generator ==========
    
    def stream_batches(self, file_path: Path, json_path: str) -> AsyncIterator[List[dict]]:
        """
        Generator that streams batches from JSON file without buffering all.
        
        This is the key memory optimization - we never hold more than
        BATCH_SIZE items in memory at once.
        """
        async def _generator():
            with open(file_path, 'rb') as f:
                items = ijson.items(f, json_path)
                batch = []
                for item in items:
                    batch.append(item)
                    if len(batch) >= BATCH_SIZE:
                        yield batch
                        batch = []
                if batch:
                    yield batch
        return _generator()

    # ========== JLPT Mappings (small, kept in memory) ==========
    
    def load_jlpt_mappings(self) -> None:
        """Load JLPT level mappings from reference files."""
        safe_print("Loading JLPT level mappings...")
        
        # Kanji JLPT mapping
        kanji_ref_path = self.source_dir / "kanji" / "reference.json"
        if kanji_ref_path.exists():
            with open(kanji_ref_path, 'rb') as f:
                for character, data in ijson.kvitems(f, ''):
                    self.kanji_jlpt_mapping[character] = {
                        'jlpt_old': data.get('jlpt_old'),
                        'jlpt_new': data.get('jlpt_new')
                    }
            safe_print(f"Loaded JLPT mapping for {len(self.kanji_jlpt_mapping)} kanji")
        
        # Vocabulary JLPT mapping
        vocab_ref_path = self.source_dir / "vocabulary" / "reference.json"
        if vocab_ref_path.exists():
            with open(vocab_ref_path, 'rb') as f:
                for entry in ijson.items(f, 'item'):
                    original = entry.get('Original', '')
                    furigana = entry.get('Furigana', '')
                    jlpt_level = entry.get('JLPT Level', '')
                    
                    jlpt_numeric = None
                    if isinstance(jlpt_level, str) and jlpt_level.startswith('N'):
                        jlpt_numeric = int(jlpt_level[1:])
                    
                    if original and furigana:
                        self.vocabulary_jlpt_mapping[(original, furigana)] = jlpt_numeric
                    elif furigana:
                        self.vocabulary_jlpt_mapping[(furigana, furigana)] = jlpt_numeric
            safe_print(f"Loaded JLPT mapping for {len(self.vocabulary_jlpt_mapping)} vocabulary")

    # ========== Furigana (disk-backed cache) ==========
    
    def load_furigana_data(self) -> None:
        """Load furigana data into disk-backed caches."""
        safe_print("Loading furigana data into disk-backed caches...")
        
        # Vocabulary furigana
        vocab_furigana_path = self.source_dir / "vocabulary" / "furigana.json"
        if vocab_furigana_path.exists():
            self.vocab_furigana_cache = DiskBackedCache(
                os.path.join(CACHE_DIR, 'vocab_furigana.db'), 'furigana'
            )
            count = 0
            with open(vocab_furigana_path, 'r', encoding='utf-8-sig') as f:
                for entry in ijson.items(f, 'item'):
                    text = entry.get('text')
                    reading = entry.get('reading')
                    furigana = entry.get('furigana')
                    if text and reading and furigana:
                        self.vocab_furigana_cache.set((text, reading), furigana)
                        count += 1
            safe_print(f"Loaded {count} vocabulary furigana entries to disk cache")
        
        # Proper noun furigana
        names_furigana_path = self.source_dir / "names" / "furigana.json"
        if names_furigana_path.exists():
            self.proper_noun_furigana_cache = DiskBackedCache(
                os.path.join(CACHE_DIR, 'pn_furigana.db'), 'furigana'
            )
            count = 0
            with open(names_furigana_path, 'r', encoding='utf-8-sig') as f:
                for entry in ijson.items(f, 'item'):
                    text = entry.get('text')
                    reading = entry.get('reading')
                    furigana = entry.get('furigana')
                    if text and reading and furigana:
                        self.proper_noun_furigana_cache.set((text, reading), furigana)
                        count += 1
            safe_print(f"Loaded {count} proper noun furigana entries to disk cache")

    # ========== Tag Pre-population ==========
    
    def _load_tag_descriptions(self) -> None:
        """Load tag descriptions from JSON source files."""
        json_files = [
            self.source_dir / 'vocabulary' / 'source.json',
            self.source_dir / 'vocabulary' / 'vocabularyWithExamples' / 'source.json',
            self.source_dir / 'names' / 'source.json'
        ]
        
        for json_file in json_files:
            if json_file.exists():
                try:
                    with open(json_file, 'rb') as f:
                        for tag_code, description in ijson.kvitems(f, 'tags'):
                            self._tag_descriptions[tag_code] = description
                except Exception as e:
                    safe_print(f"Error loading tags from {json_file}: {e}")
        
        safe_print(f"Loaded {len(self._tag_descriptions)} tag descriptions")

    async def pre_populate_tags(self) -> None:
        """Pre-populate all tags to avoid race conditions."""
        safe_print("Pre-populating tag table...")
        
        self._load_tag_descriptions()
        # Use dict to track categories and sources per tag
        # {tag_code: {'categories': set(), 'sources': set()}}
        all_tags: Dict[str, Dict[str, set]] = {}
        
        def add_tag(tag: str, category: str, source: str) -> None:
            if tag not in all_tags:
                all_tags[tag] = {'categories': set(), 'sources': set()}
            all_tags[tag]['categories'].add(category)
            all_tags[tag]['sources'].add(source)
        
        # Scan vocabulary file
        vocab_path = self.source_dir / "vocabulary" / "source.json"
        if vocab_path.exists():
            with open(vocab_path, 'rb') as f:
                for word in ijson.items(f, 'words.item'):
                    for kanji in word.get('kanji', []):
                        for tag in kanji.get('tags', []):
                            add_tag(tag, 'kanji', 'vocabulary')
                    for kana in word.get('kana', []):
                        for tag in kana.get('tags', []):
                            add_tag(tag, 'kana', 'vocabulary')
                    for sense in word.get('sense', []):
                        for tag in sense.get('partOfSpeech', []):
                            add_tag(tag, 'part_of_speech', 'vocabulary')
                        for tag in sense.get('field', []):
                            add_tag(tag, 'field', 'vocabulary')
                        for tag in sense.get('dialect', []):
                            add_tag(tag, 'dialect', 'vocabulary')
                        for tag in sense.get('misc', []):
                            add_tag(tag, 'misc', 'vocabulary')
        
        # Scan names file
        names_path = self.source_dir / "names" / "source.json"
        if names_path.exists():
            with open(names_path, 'rb') as f:
                for name in ijson.items(f, 'words.item'):
                    for kanji in name.get('kanji', []):
                        for tag in kanji.get('tags', []):
                            add_tag(tag, 'proper_noun', 'proper-noun')
                    for kana in name.get('kana', []):
                        for tag in kana.get('tags', []):
                            add_tag(tag, 'proper_noun', 'proper-noun')
                    for trans in name.get('translation', []):
                        for tag in trans.get('type', []):
                            add_tag(tag, 'translation_type', 'proper-noun')
        
        safe_print(f"Found {len(all_tags)} unique tags")
        
        # Insert all tags with source array
        async with self.pool.acquire() as conn:
            records = [
                (code, self._tag_descriptions.get(code, f'{list(data["categories"])[0]} tag'), 
                 list(data['categories'])[0], list(data['sources']))
                for code, data in all_tags.items()
            ]
            await conn.executemany('''
                INSERT INTO jlpt.tag (code, description, category, source)
                VALUES ($1, $2, $3, $4)
                ON CONFLICT (code) DO UPDATE SET source = EXCLUDED.source
            ''', records)
            
            self.tag_cache = set(all_tags.keys())
        
        safe_print("Tag pre-population complete")

    # ========== Kanji Processing ==========
    
    async def process_kanji_batch(self, batch: List[dict]) -> int:
        """Process a batch of kanji with optimized inserts."""
        async with self.semaphore:
            async with self.pool.acquire() as conn:
                async with conn.transaction():
                    for char_data in batch:
                        character = char_data.get('literal', '')
                        if not character:
                            continue
                        
                        misc = char_data.get('misc', {})
                        jlpt_old = None
                        jlpt_new = None
                        if character in self.kanji_jlpt_mapping:
                            jlpt_old = self.kanji_jlpt_mapping[character]['jlpt_old']
                            jlpt_new = self.kanji_jlpt_mapping[character]['jlpt_new']
                        
                        stroke_count = misc.get('strokeCounts', [0])[0] if misc.get('strokeCounts') else 0
                        
                        # Insert kanji
                        kanji_id = await conn.fetchval('''
                            INSERT INTO jlpt.kanji (
                                literal, grade, stroke_count, frequency, jlpt_level_old, jlpt_level_new
                            ) VALUES ($1, $2, $3, $4, $5, $6)
                            ON CONFLICT (literal) DO UPDATE SET
                                grade = EXCLUDED.grade, stroke_count = EXCLUDED.stroke_count,
                                frequency = EXCLUDED.frequency, jlpt_level_old = EXCLUDED.jlpt_level_old,
                                jlpt_level_new = EXCLUDED.jlpt_level_new, updated_at = CURRENT_TIMESTAMP
                            RETURNING id
                        ''', character, misc.get('grade'), stroke_count, 
                            misc.get('frequency'), jlpt_old, jlpt_new)
                        
                        self.kanji_cache[character] = kanji_id
                        
                        # Process sub-items
                        await self._process_kanji_subitems(conn, char_data, kanji_id)
                
                return len(batch)
    
    async def _process_kanji_subitems(self, conn, char_data: dict, kanji_id: int) -> None:
        """Process kanji sub-items (codepoints, readings, meanings, etc.)."""
        # Codepoints
        codepoints = [(kanji_id, cp.get('type', ''), cp.get('value', ''))
                      for cp in char_data.get('codepoints', [])]
        if codepoints:
            await conn.executemany('''
                INSERT INTO jlpt.kanji_codepoint (kanji_id, type, value)
                VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
            ''', codepoints)
        
        # Dictionary references
        dict_refs = []
        for ref in char_data.get('dictionaryReferences', []):
            morohashi = ref.get('morohashi') or {}
            dict_refs.append((
                kanji_id, ref.get('type', ''), ref.get('value', ''),
                morohashi.get('volume'), morohashi.get('page')
            ))
        if dict_refs:
            await conn.executemany('''
                INSERT INTO jlpt.kanji_dictionary_reference (
                    kanji_id, type, value, morohashi_volume, morohashi_page
                ) VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
            ''', dict_refs)
        
        # Query codes
        query_codes = [(kanji_id, qc.get('type', ''), qc.get('value', ''), 
                        qc.get('skipMisclassification'))
                       for qc in char_data.get('queryCodes', [])]
        if query_codes:
            await conn.executemany('''
                INSERT INTO jlpt.kanji_query_code (kanji_id, type, value, skip_missclassification)
                VALUES ($1, $2, $3, $4) ON CONFLICT DO NOTHING
            ''', query_codes)
        
        # Readings and meanings
        reading_meaning = char_data.get('readingMeaning', {})
        if reading_meaning and 'groups' in reading_meaning:
            readings = []
            meanings = []
            for group in reading_meaning['groups']:
                for r in group.get('readings', []):
                    readings.append((kanji_id, r.get('type', ''), r.get('value', ''),
                                    r.get('status'), r.get('onType')))
                for m in group.get('meanings', []):
                    meanings.append((kanji_id, LANGUAGE_MAP.get(m.get('lang', '')), m.get('value', '')))
            
            if readings:
                await conn.executemany('''
                    INSERT INTO jlpt.kanji_reading (kanji_id, type, value, status, on_type)
                    VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
                ''', readings)
            if meanings:
                await conn.executemany('''
                    INSERT INTO jlpt.kanji_meaning (kanji_id, lang, value)
                    VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                ''', meanings)
        
        # Nanori
        if reading_meaning and 'nanori' in reading_meaning:
            nanoris = []
            for nanori in reading_meaning['nanori']:
                nanoris.append((kanji_id, nanori))
            if nanoris:
                await conn.executemany('''
                    INSERT INTO jlpt.kanji_nanori (kanji_id, value)
                    VALUES ($1, $2) ON CONFLICT DO NOTHING
                ''', nanoris)

    async def process_kanji_data(self) -> None:
        """Process all kanji data with streaming and bounded concurrency."""
        safe_print(f"Processing kanji data (batch={BATCH_SIZE}, concurrent={MAX_CONCURRENT})...")
        
        kanji_path = self.source_dir / "kanji" / "source.json"
        if not kanji_path.exists():
            safe_print(f"Kanji source not found: {kanji_path}")
            return
        
        tasks = []
        total = 0
        completed = 0
        
        # Stream batches without buffering all
        with open(kanji_path, 'rb') as f:
            batch = []
            for char_data in ijson.items(f, 'characters.item'):
                batch.append(char_data)
                total += 1
                if len(batch) >= BATCH_SIZE:
                    tasks.append(asyncio.create_task(self.process_kanji_batch(batch)))
                    batch = []
            if batch:
                tasks.append(asyncio.create_task(self.process_kanji_batch(batch)))
        
        # Process with progress reporting
        for coro in asyncio.as_completed(tasks):
            count = await coro
            completed += count
            if completed % 1000 == 0:
                safe_print(f"Kanji progress: {completed}/{total}")
        
        safe_print(f"Kanji processing complete: {len(self.kanji_cache)} entries")

    # ========== Vocabulary Processing ==========
    
    def _get_vocab_jlpt_level(self, word_data: dict) -> Optional[int]:
        """Get JLPT level for vocabulary from mapping."""
        kanji_list = word_data.get('kanji', [])
        primary_kanji = kanji_list[0].get('text', '') if kanji_list else ''
        kana_list = word_data.get('kana', [])
        primary_kana = kana_list[0].get('text', '') if kana_list else ''
        
        if primary_kanji and primary_kana:
            jlpt = self.vocabulary_jlpt_mapping.get((primary_kanji, primary_kana))
            if jlpt:
                return jlpt
        if primary_kana and not primary_kanji:
            return self.vocabulary_jlpt_mapping.get((primary_kana, primary_kana))
        return None

    async def process_vocabulary_batch(self, batch: List[dict]) -> Tuple[int, List]:
        """Process a batch of vocabulary."""
        local_relations = []
        
        async with self.semaphore:
            async with self.pool.acquire() as conn:
                async with conn.transaction():
                    for word_data in batch:
                        jmdict_id = word_data.get('id', '')
                        if not jmdict_id:
                            continue
                        
                        jlpt_new = self._get_vocab_jlpt_level(word_data)
                        
                        vocab_id = await conn.fetchval('''
                            INSERT INTO jlpt.vocabulary (jmdict_id, jlpt_level_new)
                            VALUES ($1, $2)
                            ON CONFLICT (jmdict_id) DO UPDATE SET
                                jlpt_level_new = EXCLUDED.jlpt_level_new,
                                updated_at = CURRENT_TIMESTAMP
                            RETURNING id
                        ''', jmdict_id, jlpt_new)
                        
                        self.vocabulary_cache[jmdict_id] = vocab_id
                        
                        await self._process_vocab_forms(conn, vocab_id, word_data)
                        await self._process_vocab_senses(conn, vocab_id, word_data, local_relations)
                        await self._process_furigana(conn, vocab_id, 'vocabulary', word_data, 
                                                      self.vocab_furigana_cache)
                
                return len(batch), local_relations
    
    async def _process_vocab_forms(self, conn, vocab_id: int, word_data: dict) -> None:
        """Process kanji and kana forms for vocabulary."""
        # Kanji forms
        kanji_batch = [(vocab_id, k.get('text', ''), k.get('common', False), idx == 0)
                       for idx, k in enumerate(word_data.get('kanji', []))]
        if kanji_batch:
            await conn.executemany('''
                INSERT INTO jlpt.vocabulary_kanji (vocabulary_id, text, is_common, is_primary)
                VALUES ($1, $2, $3, $4) ON CONFLICT DO NOTHING
            ''', kanji_batch)
        
        # Kana forms
        kana_batch = [(vocab_id, k.get('text', ''), k.get('appliesToKanji', []),
                       k.get('common', False), idx == 0)
                      for idx, k in enumerate(word_data.get('kana', []))]
        if kana_batch:
            await conn.executemany('''
                INSERT INTO jlpt.vocabulary_kana (vocabulary_id, text, applies_to_kanji, is_common, is_primary)
                VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
            ''', kana_batch)

    async def _process_vocab_senses(self, conn, vocab_id: int, word_data: dict, 
                                     local_relations: List) -> None:
        """Process senses for vocabulary."""
        for sense in word_data.get('sense', []):
            sense_id = await conn.fetchval('''
                INSERT INTO jlpt.vocabulary_sense (vocabulary_id, applies_to_kanji, applies_to_kana, info)
                VALUES ($1, $2, $3, $4) RETURNING id
            ''', vocab_id, sense.get('appliesToKanji', []), 
                sense.get('appliesToKana', []), sense.get('info', []))
            
            # Tags
            tag_batch = []
            for pos in sense.get('partOfSpeech', []):
                tag_batch.append((sense_id, pos, 'pos'))
            for field in sense.get('field', []):
                tag_batch.append((sense_id, field, 'field'))
            for dialect in sense.get('dialect', []):
                tag_batch.append((sense_id, dialect, 'dialect'))
            for misc in sense.get('misc', []):
                tag_batch.append((sense_id, misc, 'misc'))
            
            if tag_batch:
                await conn.executemany('''
                    INSERT INTO jlpt.vocabulary_sense_tag (sense_id, tag_code, tag_type)
                    VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                ''', tag_batch)
            
            # Glosses
            glosses = [(sense_id, g.get('lang'), g.get('text'), g.get('gender'), g.get('type'))
                       for g in sense.get('gloss', [])]
            if glosses:
                await conn.executemany('''
                    INSERT INTO jlpt.vocabulary_sense_gloss (sense_id, lang, text, gender, type)
                    VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
                ''', glosses)
            
            # Language sources
            lang_sources = [(sense_id, ls.get('lang'), ls.get('text'), ls.get('full'), ls.get('wasei'))
                           for ls in sense.get('languageSource', [])]
            if lang_sources:
                await conn.executemany('''
                    INSERT INTO jlpt.vocabulary_sense_language_source (sense_id, lang, text, "full", wasei)
                    VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
                ''', lang_sources)
            
            # Related/Antonym - collect for later resolution
            for rel in sense.get('related', []):
                if isinstance(rel, list) and len(rel) > 0:
                    local_relations.append((sense_id, rel[0], rel[1] if len(rel) > 1 else None,
                                           rel[2] if len(rel) > 2 else None, 'related'))
            for ant in sense.get('antonym', []):
                if isinstance(ant, list) and len(ant) > 0:
                    local_relations.append((sense_id, ant[0], ant[1] if len(ant) > 1 else None,
                                           ant[2] if len(ant) > 2 else None, 'antonym'))

    async def _process_furigana(self, conn, entity_id: int, entity_type: str, 
                                 word_data: dict, furigana_cache) -> None:
        """Process furigana data for an entity."""
        if not furigana_cache:
            return
        
        furigana_batch = []
        processed = set()
        
        for kanji in word_data.get('kanji', []):
            text = kanji.get('text')
            if not text:
                continue
            for kana in word_data.get('kana', []):
                reading = kana.get('reading', kana.get('text'))
                if not reading:
                    continue
                applies_to = kana.get('appliesToKanji', [])
                if applies_to and applies_to != ['*'] and text not in applies_to:
                    continue
                if (text, reading) in processed:
                    continue
                
                furi_data = furigana_cache.get((text, reading))
                if furi_data:
                    furigana_batch.append((entity_id, text, reading, json_dumps(furi_data)))
                    processed.add((text, reading))
        
        if furigana_batch:
            table = f"jlpt.{entity_type}_furigana"
            id_col = f"{entity_type}_id"
            await conn.executemany(f'''
                INSERT INTO {table} ({id_col}, text, reading, furigana)
                VALUES ($1, $2, $3, $4) ON CONFLICT ({id_col}, text, reading) DO NOTHING
            ''', furigana_batch)

    async def process_vocabulary_data(self) -> None:
        """Process all vocabulary data with streaming."""
        safe_print(f"Processing vocabulary data (batch={BATCH_SIZE})...")
        
        vocab_path = self.source_dir / "vocabulary" / "source.json"
        if not vocab_path.exists():
            safe_print(f"Vocabulary source not found: {vocab_path}")
            return
        
        tasks = []
        total = 0
        
        with open(vocab_path, 'rb') as f:
            batch = []
            for word_data in ijson.items(f, 'words.item'):
                batch.append(word_data)
                total += 1
                if len(batch) >= BATCH_SIZE:
                    tasks.append(asyncio.create_task(self.process_vocabulary_batch(batch)))
                    batch = []
            if batch:
                tasks.append(asyncio.create_task(self.process_vocabulary_batch(batch)))
        
        completed = 0
        for coro in asyncio.as_completed(tasks):
            count, local_rels = await coro
            completed += count
            for rel in local_rels:
                self.pending_vocab_relations.append(rel)
            if completed % 10000 == 0:
                safe_print(f"Vocabulary progress: {completed}/{total}")
        
        safe_print(f"Vocabulary complete: {len(self.vocabulary_cache)} entries, "
                   f"{len(self.pending_vocab_relations)} pending relations")

    # ========== Vocabulary Examples Processing ==========
    
    async def process_vocabulary_examples_batch(self, batch: List[dict]) -> int:
        """Process a batch of vocabulary examples."""
        async with self.semaphore:
            async with self.pool.acquire() as conn:
                async with conn.transaction():
                    for word_data in batch:
                        jmdict_id = word_data.get('id', '')
                        if not jmdict_id or jmdict_id not in self.vocabulary_cache:
                            continue
                        
                        vocab_id = self.vocabulary_cache[jmdict_id]
                        
                        # Get first sense ID
                        sense_id = await conn.fetchval('''
                            SELECT id FROM jlpt.vocabulary_sense 
                            WHERE vocabulary_id = $1 LIMIT 1
                        ''', vocab_id)
                        
                        if not sense_id:
                            continue
                        
                        for sense in word_data.get('sense', []):
                            for example in sense.get('examples', []):
                                source_type = example.get('source', {}).get('type')
                                source_value = example.get('source', {}).get('value')
                                text = example.get('text', '')
                                
                                example_id = await conn.fetchval('''
                                    INSERT INTO jlpt.vocabulary_sense_example 
                                        (sense_id, source_type, source_value, text)
                                    VALUES ($1, $2, $3, $4) RETURNING id
                                ''', sense_id, source_type, source_value, text)
                                
                                if example_id:
                                    sentences = [(example_id, s.get('lang'), s.get('text'))
                                                for s in example.get('sentences', [])]
                                    if sentences:
                                        await conn.executemany('''
                                            INSERT INTO jlpt.vocabulary_sense_example_sentence
                                                (example_id, lang, text)
                                            VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                                        ''', sentences)
                
                return len(batch)

    async def process_vocabulary_examples(self) -> None:
        """Process vocabulary examples."""
        safe_print("Processing vocabulary examples...")
        
        examples_path = self.source_dir / "vocabulary" / "vocabularyWithExamples" / "source.json"
        if not examples_path.exists():
            safe_print(f"Vocabulary examples not found: {examples_path}")
            return
        
        tasks = []
        total = 0
        
        with open(examples_path, 'rb') as f:
            batch = []
            for word_data in ijson.items(f, 'words.item'):
                batch.append(word_data)
                total += 1
                if len(batch) >= BATCH_SIZE:
                    tasks.append(asyncio.create_task(self.process_vocabulary_examples_batch(batch)))
                    batch = []
            if batch:
                tasks.append(asyncio.create_task(self.process_vocabulary_examples_batch(batch)))
        
        completed = 0
        for coro in asyncio.as_completed(tasks):
            count = await coro
            completed += count
            if completed % 10000 == 0:
                safe_print(f"Examples progress: {completed}/{total}")
        
        safe_print(f"Vocabulary examples complete: {completed} processed")

    # ========== Radical Processing ==========
    
    async def process_radical_data(self) -> None:
        """Process radical data from radfile and kradfile."""
        safe_print("Processing radical data...")
        
        ref_path = self.source_dir / "radfile" / "reference.txt"
        radfile_path = self.source_dir / "radfile" / "source.json"
        kradfile_path = self.source_dir / "kradfile" / "source.json"
        
        if not ref_path.exists():
            safe_print(f"Radical reference not found: {ref_path}")
            return
        
        async with self.pool.acquire() as conn:
            # Phase 1: Populate radical_group from reference.txt
            radical_group_cache = {}
            
            with open(ref_path, 'r', encoding='utf-8') as f:
                for line in f:
                    line = line.strip()
                    if not line:
                        continue
                    
                    parts = line.split('\t')
                    parts += [''] * (7 - len(parts))
                    
                    canonical = parts[0]
                    norm_canonical = self._normalize_radical_char(canonical)
                    
                    try:
                        kxn = int(parts[2]) if parts[2] else None
                    except ValueError:
                        kxn = None
                    
                    readings = [r.strip() for r in parts[4].split('・') if r.strip()]
                    meanings = [m.strip() for m in parts[5].split(',') if m.strip()]
                    notes = [parts[6]] if parts[6] else []
                    
                    if norm_canonical not in radical_group_cache:
                        group_id = await conn.fetchval('''
                            INSERT INTO jlpt.radical_group (canonical_literal, kang_xi_number, meanings, readings, notes)
                            VALUES ($1, $2, $3, $4, $5)
                            ON CONFLICT (canonical_literal) DO UPDATE SET
                                kang_xi_number = COALESCE(EXCLUDED.kang_xi_number, jlpt.radical_group.kang_xi_number)
                            RETURNING id
                        ''', norm_canonical, kxn, meanings, readings, notes)
                        radical_group_cache[norm_canonical] = group_id
                        radical_group_cache[canonical] = group_id
            
            safe_print(f"Created {len(set(radical_group_cache.values()))} radical groups")
            
            # Phase 2: Populate radical_group_member
            with open(ref_path, 'r', encoding='utf-8') as f:
                for line in f:
                    line = line.strip()
                    if not line:
                        continue
                    
                    parts = line.split('\t')
                    parts += [''] * (7 - len(parts))
                    
                    canonical = parts[0]
                    variant = parts[1] if parts[1] else None
                    norm_canonical = self._normalize_radical_char(canonical)
                    group_id = radical_group_cache.get(norm_canonical)
                    if not group_id:
                        continue
                    
                    for lit, is_canon in [(canonical, True), (variant, False)]:
                        if not lit:
                            continue
                        await conn.execute('''
                            INSERT INTO jlpt.radical_group_member (group_id, literal, is_canonical)
                            VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                        ''', group_id, lit, is_canon)
            
            # Build member -> group lookup
            rows = await conn.fetch("SELECT group_id, literal FROM jlpt.radical_group_member")
            member_to_group = {row['literal']: row['group_id'] for row in rows}
        
        # Phase 3: Populate radical from source.json
        if radfile_path.exists():
            async with self.pool.acquire() as conn:
                with open(radfile_path, 'rb') as f:
                    for char, data in ijson.kvitems(f, 'radicals'):
                        norm_char = self._normalize_radical_char(char)
                        group_id = member_to_group.get(norm_char) or member_to_group.get(char)
                        
                        radical_id = await conn.fetchval('''
                            INSERT INTO jlpt.radical (literal, stroke_count, code, group_id)
                            VALUES ($1, $2, $3, $4)
                            ON CONFLICT (literal) DO UPDATE SET
                                stroke_count = EXCLUDED.stroke_count, code = EXCLUDED.code,
                                group_id = EXCLUDED.group_id
                            RETURNING id
                        ''', norm_char, data.get('strokeCount', 0), data.get('code'), group_id)
                        
                        self.radical_cache[norm_char] = radical_id
            
            safe_print(f"Processed {len(self.radical_cache)} radicals")
        
        # Phase 4: Link kanji to radicals
        if kradfile_path.exists():
            async with self.pool.acquire() as conn:
                batch = []
                with open(kradfile_path, 'rb') as f:
                    for kanji_char, components in ijson.kvitems(f, 'kanji'):
                        kanji_id = self.kanji_cache.get(kanji_char)
                        if not kanji_id:
                            continue
                        for comp in components:
                            comp = self._normalize_radical_char(comp)
                            radical_id = self.radical_cache.get(comp)
                            if radical_id:
                                batch.append((kanji_id, radical_id))
                
                if batch:
                    await conn.executemany('''
                        INSERT INTO jlpt.kanji_radical (kanji_id, radical_id)
                        VALUES ($1, $2) ON CONFLICT DO NOTHING
                    ''', batch)
                    safe_print(f"Linked {len(batch)} kanji-radical relationships")

    # ========== Proper Nouns Processing ==========
    
    async def process_proper_noun_batch(self, batch: List[dict]) -> Tuple[int, List]:
        """Process a batch of proper nouns."""
        local_relations = []
        
        async with self.semaphore:
            async with self.pool.acquire() as conn:
                async with conn.transaction():
                    for name_data in batch:
                        jmnedict_id = name_data.get('id', '')
                        if not jmnedict_id:
                            continue
                        
                        pn_id = await conn.fetchval('''
                            INSERT INTO jlpt.proper_noun (jmnedict_id)
                            VALUES ($1) ON CONFLICT (jmnedict_id) DO NOTHING RETURNING id
                        ''', jmnedict_id)
                        
                        if not pn_id:
                            pn_id = await conn.fetchval(
                                "SELECT id FROM jlpt.proper_noun WHERE jmnedict_id = $1", jmnedict_id)
                        
                        # Forms
                        kanji_batch = [(pn_id, k.get('text', ''), idx == 0)
                                       for idx, k in enumerate(name_data.get('kanji', []))]
                        if kanji_batch:
                            await conn.executemany('''
                                INSERT INTO jlpt.proper_noun_kanji (proper_noun_id, text, is_primary)
                                VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                            ''', kanji_batch)
                        
                        kana_batch = [(pn_id, k.get('text', ''), k.get('appliesToKanji', []), idx == 0)
                                      for idx, k in enumerate(name_data.get('kana', []))]
                        if kana_batch:
                            await conn.executemany('''
                                INSERT INTO jlpt.proper_noun_kana (proper_noun_id, text, applies_to_kanji, is_primary)
                                VALUES ($1, $2, $3, $4) ON CONFLICT DO NOTHING
                            ''', kana_batch)
                        
                        # Translations
                        for trans in name_data.get('translation', []):
                            trans_id = await conn.fetchval('''
                                INSERT INTO jlpt.proper_noun_translation (proper_noun_id)
                                VALUES ($1) RETURNING id
                            ''', pn_id)
                            
                            # Types
                            type_batch = [(trans_id, t) for t in trans.get('type', [])]
                            if type_batch:
                                await conn.executemany('''
                                    INSERT INTO jlpt.proper_noun_translation_type (translation_id, tag_code)
                                    VALUES ($1, $2) ON CONFLICT DO NOTHING
                                ''', type_batch)
                            
                            # Text
                            text_batch = [(trans_id, t.get('lang'), t.get('text'))
                                         for t in trans.get('translation', [])]
                            if text_batch:
                                await conn.executemany('''
                                    INSERT INTO jlpt.proper_noun_translation_text (translation_id, lang, text)
                                    VALUES ($1, $2, $3) ON CONFLICT DO NOTHING
                                ''', text_batch)
                            
                            # Related
                            for rel in trans.get('related', []):
                                if isinstance(rel, list) and len(rel) > 0:
                                    local_relations.append((trans_id, rel[0], 
                                        rel[1] if len(rel) > 1 else None,
                                        rel[2] if len(rel) > 2 else None))
                        
                        # Kanji relationships
                        for kanji in name_data.get('kanji', []):
                            for char in kanji.get('text', ''):
                                if char in self.kanji_cache:
                                    await conn.execute('''
                                        INSERT INTO jlpt.proper_noun_uses_kanji (proper_noun_id, kanji_id)
                                        VALUES ($1, $2) ON CONFLICT DO NOTHING
                                    ''', pn_id, self.kanji_cache[char])
                        
                        # Furigana
                        await self._process_furigana(conn, pn_id, 'proper_noun', name_data,
                                                      self.proper_noun_furigana_cache)
                
                return len(batch), local_relations

    async def process_proper_nouns(self) -> None:
        """Process all proper nouns."""
        safe_print("Processing proper nouns...")
        
        names_path = self.source_dir / "names" / "source.json"
        if not names_path.exists():
            safe_print(f"Names source not found: {names_path}")
            return
        
        tasks = []
        total = 0
        
        with open(names_path, 'rb') as f:
            batch = []
            for name_data in ijson.items(f, 'words.item'):
                batch.append(name_data)
                total += 1
                if len(batch) >= BATCH_SIZE:
                    tasks.append(asyncio.create_task(self.process_proper_noun_batch(batch)))
                    batch = []
            if batch:
                tasks.append(asyncio.create_task(self.process_proper_noun_batch(batch)))
        
        completed = 0
        for coro in asyncio.as_completed(tasks):
            count, local_rels = await coro
            completed += count
            for rel in local_rels:
                self.pending_proper_noun_relations.append(rel)
            if completed % 50000 == 0:
                safe_print(f"Proper nouns progress: {completed}/{total}")
        
        safe_print(f"Proper nouns complete: {completed} entries")

    # ========== Relation Resolution ==========
    
    async def build_term_caches(self) -> None:
        """Build term lookup caches for relation resolution."""
        safe_print("Building term lookup caches...")
        
        self.vocab_term_cache = {}
        self.proper_noun_term_cache = {}
        
        async with self.pool.acquire() as conn:
            # Vocabulary terms
            rows = await conn.fetch('''
                SELECT v.id, vk.text as kanji, vka.text as kana
                FROM jlpt.vocabulary v
                LEFT JOIN jlpt.vocabulary_kanji vk ON vk.vocabulary_id = v.id
                LEFT JOIN jlpt.vocabulary_kana vka ON vka.vocabulary_id = v.id
            ''')
            for row in rows:
                if row['kanji']:
                    self.vocab_term_cache[(row['kanji'], None)] = row['id']
                    if row['kana']:
                        self.vocab_term_cache[(row['kanji'], row['kana'])] = row['id']
                if row['kana']:
                    self.vocab_term_cache[(row['kana'], None)] = row['id']
            
            safe_print(f"Vocabulary term cache: {len(self.vocab_term_cache)} entries")
            
            # Proper noun terms
            rows = await conn.fetch('''
                SELECT pn.id, pnk.text as kanji, pnka.text as kana
                FROM jlpt.proper_noun pn
                LEFT JOIN jlpt.proper_noun_kanji pnk ON pnk.proper_noun_id = pn.id
                LEFT JOIN jlpt.proper_noun_kana pnka ON pnka.proper_noun_id = pn.id
            ''')
            for row in rows:
                if row['kanji']:
                    self.proper_noun_term_cache[(row['kanji'], None)] = row['id']
                if row['kana']:
                    self.proper_noun_term_cache[(row['kana'], None)] = row['id']
            
            safe_print(f"Proper noun term cache: {len(self.proper_noun_term_cache)} entries")

    async def resolve_vocab_relations(self) -> None:
        """Resolve vocabulary relations."""
        total = len(self.pending_vocab_relations)
        safe_print(f"Resolving {total} vocabulary relations...")
        
        if total == 0:
            return
        
        # Prefetch sense IDs
        sense_cache = defaultdict(list)
        async with self.pool.acquire() as conn:
            rows = await conn.fetch(
                "SELECT vocabulary_id, id FROM jlpt.vocabulary_sense ORDER BY vocabulary_id, id")
            for row in rows:
                sense_cache[row['vocabulary_id']].append(row['id'])
        
        resolved = []
        for sense_id, term, reading, sense_idx, rel_type in self.pending_vocab_relations:
            target_vocab_id = None
            if reading:
                target_vocab_id = self.vocab_term_cache.get((term, reading))
            if not target_vocab_id:
                target_vocab_id = self.vocab_term_cache.get((term, None))
            
            target_sense_id = None
            if target_vocab_id and sense_idx and sense_idx > 0:
                senses = sense_cache.get(target_vocab_id, [])
                if sense_idx <= len(senses):
                    target_sense_id = senses[sense_idx - 1]
            
            # Ensure all values are proper types for asyncpg
            resolved.append((
                sense_id, 
                target_vocab_id, 
                target_sense_id, 
                str(term) if term else None, 
                str(reading) if reading else None, 
                str(rel_type) if rel_type else None
            ))
        
        # Batch insert
        async with self.pool.acquire() as conn:
            await conn.executemany('''
                INSERT INTO jlpt.vocabulary_sense_relation 
                    (source_sense_id, target_vocab_id, target_sense_id, target_term, target_reading, relation_type)
                VALUES ($1, $2, $3, $4, $5, $6) ON CONFLICT DO NOTHING
            ''', resolved)
        
        resolved_count = sum(1 for r in resolved if r[1] is not None)
        safe_print(f"Vocabulary relations: {resolved_count} resolved, {total - resolved_count} unresolved")
        self.pending_vocab_relations.clear()

    async def resolve_proper_noun_relations(self) -> None:
        """Resolve proper noun relations."""
        total = len(self.pending_proper_noun_relations)
        safe_print(f"Resolving {total} proper noun relations...")
        
        if total == 0:
            return
        
        resolved = []
        for trans_id, term, reading, sense_idx in self.pending_proper_noun_relations:
            ref_pn_id = None
            if reading:
                ref_pn_id = self.proper_noun_term_cache.get((term, reading))
            if not ref_pn_id:
                ref_pn_id = self.proper_noun_term_cache.get((term, None))
            
            resolved.append((trans_id, term, reading, ref_pn_id, None))
        
        async with self.pool.acquire() as conn:
            await conn.executemany('''
                INSERT INTO jlpt.proper_noun_translation_related 
                    (translation_id, related_term, related_reading, reference_proper_noun_id, reference_proper_noun_translation_id)
                VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING
            ''', resolved)
        
        resolved_count = sum(1 for r in resolved if r[3] is not None)
        safe_print(f"Proper noun relations: {resolved_count} resolved")
        self.pending_proper_noun_relations.clear()

    # ========== Kanji-Vocabulary Relationships ==========
    
    async def process_kanji_vocabulary_relationships(self) -> None:
        """Link vocabulary to kanji characters."""
        safe_print("Processing kanji-vocabulary relationships...")
        
        async with self.pool.acquire() as conn:
            rows = await conn.fetch('''
                SELECT v.id, vk.text FROM jlpt.vocabulary v
                JOIN jlpt.vocabulary_kanji vk ON vk.vocabulary_id = v.id
            ''')
            
            batch = []
            for row in rows:
                for char in row['text']:
                    if char in self.kanji_cache:
                        batch.append((row['id'], self.kanji_cache[char]))
            
            if batch:
                await conn.executemany('''
                    INSERT INTO jlpt.vocabulary_uses_kanji (vocabulary_id, kanji_id)
                    VALUES ($1, $2) ON CONFLICT DO NOTHING
                ''', batch)
            
            safe_print(f"Linked {len(batch)} vocabulary-kanji relationships")

    # ========== Main Processing ==========
    
    async def process_all(self) -> bool:
        """Orchestrate all data processing."""
        safe_print("=" * 60)
        safe_print("JP Reference Database - Async Data Processor")
        safe_print(f"Batch size: {BATCH_SIZE}, Max concurrent: {MAX_CONCURRENT}")
        safe_print("=" * 60)
        
        try:
            await self.init_pool()
            
            # Load reference data
            self.load_jlpt_mappings()
            self.load_furigana_data()
            await self.pre_populate_tags()
            
            # Process data
            safe_print("\n=== Step 1: Processing kanji ===")
            await self.process_kanji_data()
            
            safe_print("\n=== Step 2: Processing vocabulary ===")
            await self.process_vocabulary_data()
            
            safe_print("\n=== Step 3: Processing vocabulary examples ===")
            await self.process_vocabulary_examples()
            
            safe_print("\n=== Step 4: Processing radicals ===")
            await self.process_radical_data()
            
            safe_print("\n=== Step 5: Processing proper nouns ===")
            await self.process_proper_nouns()
            
            safe_print("\n=== Step 6: Processing kanji-vocabulary relationships ===")
            await self.process_kanji_vocabulary_relationships()
            
            safe_print("\n=== Step 7: Building term caches ===")
            await self.build_term_caches()
            
            safe_print("\n=== Step 8: Resolving relations ===")
            await self.resolve_vocab_relations()
            await self.resolve_proper_noun_relations()
            
            # Print statistics
            async with self.pool.acquire() as conn:
                stats = await conn.fetch('''
                    SELECT 'kanji' as t, COUNT(*) as c FROM jlpt.kanji
                    UNION ALL SELECT 'vocabulary', COUNT(*) FROM jlpt.vocabulary
                    UNION ALL SELECT 'radicals', COUNT(*) FROM jlpt.radical
                    UNION ALL SELECT 'proper_nouns', COUNT(*) FROM jlpt.proper_noun
                ''')
                
                safe_print("\n=== Database Statistics ===")
                for row in stats:
                    safe_print(f"- {row['t']}: {row['c']:,}")
            
            safe_print("\n=== Processing completed successfully! ===")
            return True
            
        except Exception as e:
            safe_print(f"\nError during processing: {e}")
            import traceback
            traceback.print_exc()
            return False
        
        finally:
            # Cleanup
            if self.vocab_furigana_cache:
                self.vocab_furigana_cache.close()
            if self.proper_noun_furigana_cache:
                self.proper_noun_furigana_cache.close()
            self.pending_vocab_relations.cleanup()
            self.pending_proper_noun_relations.cleanup()
            await self.close_pool()


async def main():
    """Main entry point."""
    processor = AsyncJLPTDataProcessor()
    success = await processor.process_all()
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    asyncio.run(main())
