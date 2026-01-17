"""
Spillable list implementation for memory-efficient handling of large collections.
Spills to disk when memory threshold is exceeded.
"""

import os
import pickle
import tempfile
from typing import Any, Iterator, List


class SpillableList:
    """
    List that automatically spills to disk when threshold is exceeded.
    Prevents unbounded memory growth for large pending relations.
    """
    
    def __init__(self, threshold: int = 100000, spill_dir: str = None):
        """
        Initialize spillable list.
        
        Args:
            threshold: Number of items before spilling to disk
            spill_dir: Directory for spill files (defaults to temp dir)
        """
        self.threshold = threshold
        self.spill_dir = spill_dir or tempfile.gettempdir()
        self._memory: List[Any] = []
        self._spill_files: List[str] = []
        self._total_count = 0
        self._prefix = f"spillable_{id(self)}_"
    
    def append(self, item: Any) -> None:
        """Append an item, spilling to disk if threshold exceeded."""
        self._memory.append(item)
        self._total_count += 1
        
        if len(self._memory) >= self.threshold:
            self._spill()
    
    def extend(self, items: List[Any]) -> None:
        """Extend with multiple items."""
        for item in items:
            self.append(item)
    
    def _spill(self) -> None:
        """Spill current memory buffer to disk."""
        if not self._memory:
            return
        
        os.makedirs(self.spill_dir, exist_ok=True)
        fname = os.path.join(self.spill_dir, f"{self._prefix}{len(self._spill_files)}.pkl")
        
        with open(fname, 'wb') as f:
            pickle.dump(self._memory, f, protocol=pickle.HIGHEST_PROTOCOL)
        
        self._spill_files.append(fname)
        self._memory.clear()
    
    def __iter__(self) -> Iterator[Any]:
        """Iterate over all items (spilled + in-memory)."""
        # First yield from spill files
        for fname in self._spill_files:
            with open(fname, 'rb') as f:
                items = pickle.load(f)
                yield from items
        
        # Then yield from memory
        yield from self._memory
    
    def __len__(self) -> int:
        """Return total count of items."""
        return self._total_count
    
    def clear(self) -> None:
        """Clear all data and remove spill files."""
        self._memory.clear()
        self._total_count = 0
        
        for fname in self._spill_files:
            try:
                os.remove(fname)
            except OSError:
                pass
        self._spill_files.clear()
    
    def cleanup(self) -> None:
        """Alias for clear() - removes all spill files."""
        self.clear()
    
    def __del__(self):
        """Cleanup spill files on garbage collection."""
        try:
            self.cleanup()
        except Exception:
            pass
