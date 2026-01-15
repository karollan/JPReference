#!/usr/bin/env python3
"""
Data Processor Runner for Docker Compose

This script runs the data processor that handles all data sources:
- Kanji from kanjidic2
- Vocabulary from JMdict
- Vocabulary with examples
- Radical and kradfile data
- JLPT level mapping
"""

import os
import sys
import time
import psycopg2
from pathlib import Path

# CRITICAL: Disable output buffering for real-time logs
sys.stdout.reconfigure(line_buffering=True)
sys.stderr.reconfigure(line_buffering=True)

# Add the scripts directory to the path
script_dir = Path(__file__).parent
sys.path.insert(0, str(script_dir))

# Import both processors
from process_data import JLPTDataProcessor

def clean_database():
    """Clean the database before processing."""
    print("Cleaning database...", flush=True)
    
    db_params = {
        'host': os.getenv('POSTGRES_HOST', 'localhost'),
        'port': os.getenv('POSTGRES_PORT', '5432'),
        'database': os.getenv('POSTGRES_DB', 'jlptreference'),
        'user': os.getenv('POSTGRES_USER', 'jlptuser'),
        'password': os.getenv('POSTGRES_PASSWORD', 'jlptpassword')
    }
    
    max_retries = 30
    retry_delay = 2
    
    conn = None
    for attempt in range(max_retries):
        try:
            conn = psycopg2.connect(**db_params)
            break
        except psycopg2.OperationalError as e:
            print(f"Attempt {attempt + 1}/{max_retries}: Database not ready yet - {e}", flush=True)
            time.sleep(retry_delay)
            
    if not conn:
        print("Failed to connect to database for cleaning", flush=True)
        return

    try:
        cursor = conn.cursor()
        
        # Check which tables actually exist before truncating
        # This handles cases where we are migrating or starting fresh and some tables might not exist
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'jlpt'
        """)
        existing_tables_result = cursor.fetchall()
        existing_table_names = {row[0] for row in existing_tables_result}
        
        # List of tables we want to truncate if they exist
        target_tables = [
            "kanji", 
            "vocabulary", 
            "proper_noun", 
            "radical", 
            "tag",
            "status"
        ]
        
        tables_to_truncate = []
        for table in target_tables:
            if table in existing_table_names:
                tables_to_truncate.append(f"jlpt.{table}")
        
        if tables_to_truncate:
            print(f"Truncating tables: {', '.join(tables_to_truncate)}", flush=True)
            cursor.execute(f"TRUNCATE TABLE {', '.join(tables_to_truncate)} RESTART IDENTITY CASCADE;")
        else:
            print("No tables found to truncate - proceeding with potentially empty database", flush=True)
        
        conn.commit()
        print("Database cleaned successfully.", flush=True)
        
    except Exception as e:
        print(f"Error cleaning database: {e}", flush=True)
        if conn:
            conn.rollback()
        # We don't exit here, we let the processor try and fail if it must, 
        # or maybe we should exit? duplicate keys will happen if we don't clean.
        # But failing to clean might be due to other issues. 
        # Let's verify: if truncate fails, duplicates are guaranteed.
        sys.exit(1)
    finally:
        if conn:
            conn.close()



def update_status():
    """Update the database status with the current timestamp."""
    print("Updating database status...", flush=True)
    
    db_params = {
        'host': os.getenv('POSTGRES_HOST', 'localhost'),
        'port': os.getenv('POSTGRES_PORT', '5432'),
        'database': os.getenv('POSTGRES_DB', 'jlptreference'),
        'user': os.getenv('POSTGRES_USER', 'jlptuser'),
        'password': os.getenv('POSTGRES_PASSWORD', 'jlptpassword')
    }
    
    conn = None
    try:
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()
        
        # Insert the status record (id=1 is enforced by check constraint)
        # using NOW() for the timestamp
        cursor.execute("INSERT INTO jlpt.status (id, last_update) VALUES (1, NOW()) ON CONFLICT (id) DO UPDATE SET last_update = NOW();")
        
        conn.commit()
        print("Database status updated successfully.", flush=True)
        return True
    except Exception as e:
        print(f"Error updating database status: {e}", flush=True)
        if conn:
            conn.rollback()
        return False
    finally:
        if conn:
            conn.close()

def main():
    """Main function."""
    print("=" * 60, flush=True)
    print("JLPT Reference Database - Data Processor", flush=True)
    print("=" * 60, flush=True)
    
    # Clean the database first
    clean_database()
    
    print("", flush=True)
    print("Processing data sources:", flush=True)
    print("- Kanji data from kanjidic2", flush=True)
    print("- Vocabulary data from JMdict", flush=True)
    print("- Vocabulary with examples", flush=True)
    print("- Radical and kradfile data", flush=True)
    print("- JLPT level mapping from reference files", flush=True)
    print("- Proper nouns from JMnedict", flush=True)
    print("", flush=True)
    
    # Try async processor first (most optimized)
    use_async = os.getenv('USE_ASYNC', '1') == '1'
    
    if use_async:
        print("Using ASYNC processor (optimized for memory and speed)", flush=True)
        print("", flush=True)
        
        try:
            import asyncio
            from process_data_async import AsyncJLPTDataProcessor
            
            async def run_async():
                processor = AsyncJLPTDataProcessor()
                return await processor.process_all()
            
            start_time = time.time()
            success = asyncio.run(run_async())
            elapsed = time.time() - start_time
            
            if success:
                print("", flush=True)
                print("=" * 60, flush=True)
                print(f"✅ Data processing completed successfully in {elapsed:.2f} seconds!", flush=True)
                print("=" * 60, flush=True)
                update_status()
                return 0
            else:
                print("❌ Data processing failed!", flush=True)
                return 1
                
        except ImportError as e:
            print(f"Async processor not available ({e}), falling back to parallel", flush=True)
            use_async = False
        except Exception as e:
            print(f"Async processor failed ({e}), falling back to parallel", flush=True)
            use_async = False
    
    # Fall back to parallel processor
    num_workers = int(os.getenv('NUM_WORKERS', '4'))
    use_parallel = num_workers > 1 and not use_async
    
    if use_parallel:
        print(f"Using PARALLEL processing with {num_workers} workers", flush=True)
        print("", flush=True)
        
        try:
            from process_data_parallel import ParallelJLPTDataProcessor
            processor = ParallelJLPTDataProcessor()
            
            start_time = time.time()
            success = processor.process_all_data_parallel()
            elapsed = time.time() - start_time
            
            if success:
                print("", flush=True)
                print("=" * 60, flush=True)
                print(f"✅ Data processing completed successfully in {elapsed:.2f} seconds!", flush=True)
                print("=" * 60, flush=True)
                update_status()
                return 0
            else:
                print("❌ Data processing failed!", flush=True)
                return 1
                
        except ImportError:
            print("Parallel processor not found, falling back to sequential", flush=True)
            use_parallel = False
    
    # Final fallback to sequential
    print("Using SEQUENTIAL processing", flush=True)
    print("Set USE_ASYNC=1 or NUM_WORKERS=4 for faster processing", flush=True)
    print("", flush=True)
    
    processor = JLPTDataProcessor()
    
    start_time = time.time()
    success = processor.process_all_data()
    elapsed = time.time() - start_time
    
    if success:
        print("", flush=True)
        print("=" * 60, flush=True)
        print(f"✅ Data processing completed successfully in {elapsed:.2f} seconds!", flush=True)
        print("=" * 60, flush=True)
        update_status()
        return 0
    else:
        print("❌ Data processing failed!", flush=True)
        return 1

if __name__ == "__main__":
    try:
        exit_code = main()
        sys.exit(exit_code)
    except KeyboardInterrupt:
        print("\nProcessing interrupted by user", flush=True)
        sys.exit(130)
    except Exception as e:
        print(f"\n❌ Fatal error: {e}", flush=True)
        import traceback
        traceback.print_exc()
        sys.exit(1)