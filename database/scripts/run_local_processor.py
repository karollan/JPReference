#!/usr/bin/env python3
"""
Local Kanji Processor - Run this script locally to process kanji data
without Docker. Useful for development and testing.
"""

import sys
from pathlib import Path

# Add the scripts directory to Python path
script_dir = Path(__file__).parent
sys.path.insert(0, str(script_dir))

from process_kanji import main as process_main

if __name__ == "__main__":
    print("Running kanji processor locally...")
    process_main()
