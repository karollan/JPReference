#!/usr/bin/env python3
"""
Comprehensive Data Processor Runner for Docker Compose

This script runs the comprehensive data processor that handles all data sources:
- Kanji from kanjidic2
- Vocabulary from JMdict
- Vocabulary with examples
- Radical and kradfile data
- JLPT level mapping
"""

import os
import sys
import time
from pathlib import Path

# Add the scripts directory to the path
script_dir = Path(__file__).parent
sys.path.insert(0, str(script_dir))

from process_all_data import JLPTDataProcessor

def main():
    """Main function."""
    print("Starting comprehensive JLPT data processor...")
    print("This will process all data sources including:")
    print("- Kanji data from kanjidic2")
    print("- Vocabulary data from JMdict")
    print("- Vocabulary with examples")
    print("- Radical and kradfile data")
    print("- JLPT level mapping from reference files")
    print()
    
    # Create processor instance
    processor = JLPTDataProcessor()
    
    # Process all data
    if not processor.process_all_data():
        print("Data processing failed!")
        sys.exit(1)
    
    print("Comprehensive data processing completed successfully!")

if __name__ == "__main__":
    main()
