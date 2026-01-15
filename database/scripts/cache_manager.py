"""
Disk-backed cache manager for memory-efficient large lookup tables.
Uses SQLite for persistence with batched writes for performance.
"""

import sqlite3
import os
from typing import Any, Optional, Tuple
from contextlib import contextmanager

try:
    import orjson
    def dumps(obj): return orjson.dumps(obj)
    def loads(data): return orjson.loads(data)
except ImportError:
    import json
    def dumps(obj): return json.dumps(obj).encode()
    def loads(data): return json.loads(data)


class DiskBackedCache:
    """
    SQLite-backed cache for large lookup tables.    
    """
    
    def __init__(self, db_path: str = ':memory:', table_name: str = 'cache', buffer_size: int = 10000):
        """
        Initialize disk-backed cache.
        
        Args:
            db_path: Path to SQLite database file. Use ':memory:' for in-memory (testing only).
            table_name: Name of the cache table.
            buffer_size: Number of items to buffer before flushing to disk.
        """
        self.db_path = db_path
        self.table = table_name
        self._buffer_size = buffer_size
        self._write_buffer: list = []
        self._size = 0
        
        # Ensure directory exists
        if db_path != ':memory:':
            os.makedirs(os.path.dirname(db_path) or '.', exist_ok=True)
        
        self.conn = sqlite3.connect(db_path, check_same_thread=False, isolation_level=None)
        self._setup_db()
    
    def _setup_db(self):
        """Configure SQLite for optimal performance."""
        self.conn.execute('PRAGMA journal_mode=WAL')
        self.conn.execute('PRAGMA synchronous=NORMAL')
        self.conn.execute('PRAGMA cache_size=-64000')  # 64MB cache
        self.conn.execute('PRAGMA temp_store=MEMORY')
        self.conn.execute(f'''
            CREATE TABLE IF NOT EXISTS {self.table} (
                key TEXT PRIMARY KEY,
                value BLOB
            ) WITHOUT ROWID
        ''')
    
    def set(self, key: Tuple, value: Any) -> None:
        """
        Set a cache entry. Buffers writes for performance.
        
        Args:
            key: Tuple key (will be JSON-serialized)
            value: Any JSON-serializable value
        """
        key_str = dumps(key).decode() if isinstance(dumps(key), bytes) else dumps(key)
        val_bytes = dumps(value)
        self._write_buffer.append((key_str, val_bytes))
        self._size += 1
        
        if len(self._write_buffer) >= self._buffer_size:
            self._flush()
    
    def get(self, key: Tuple, default: Any = None) -> Any:
        """
        Get a cache entry.
        
        Args:
            key: Tuple key to look up
            default: Default value if not found
            
        Returns:
            Cached value or default
        """
        # Ensure pending writes are visible
        if self._write_buffer:
            self._flush()
        
        key_str = dumps(key).decode() if isinstance(dumps(key), bytes) else dumps(key)
        cursor = self.conn.execute(
            f'SELECT value FROM {self.table} WHERE key = ?', (key_str,)
        )
        row = cursor.fetchone()
        return loads(row[0]) if row else default
    
    def __contains__(self, key: Tuple) -> bool:
        """Check if key exists in cache."""
        return self.get(key) is not None
    
    def __len__(self) -> int:
        """Return approximate size of cache."""
        return self._size
    
    def _flush(self) -> None:
        """Flush write buffer to disk."""
        if not self._write_buffer:
            return
        
        try:
            self.conn.execute('BEGIN')
            self.conn.executemany(
                f'INSERT OR REPLACE INTO {self.table} (key, value) VALUES (?, ?)',
                self._write_buffer
            )
            self.conn.execute('COMMIT')
        except Exception:
            self.conn.execute('ROLLBACK')
            raise
        finally:
            self._write_buffer.clear()
    
    def close(self) -> None:
        """Flush pending writes and close connection."""
        self._flush()
        self.conn.close()
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()


class InMemoryCache:
    """
    Simple in-memory cache with same interface as DiskBackedCache.
    Used when memory is not a constraint or for small datasets.
    """
    
    def __init__(self):
        self._data = {}
    
    def set(self, key: Tuple, value: Any) -> None:
        self._data[key] = value
    
    def get(self, key: Tuple, default: Any = None) -> Any:
        return self._data.get(key, default)
    
    def __contains__(self, key: Tuple) -> bool:
        return key in self._data
    
    def __len__(self) -> int:
        return len(self._data)
    
    def close(self) -> None:
        self._data.clear()
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()
