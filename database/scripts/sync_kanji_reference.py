#!/usr/bin/env python3
"""
Kanji Reference Data Sync Script

Downloads the kanji.json reference file from davidluzgouveia/kanji-data repository.
This provides additional metadata for kanji (frequency, JLPT level, etc.).
"""

import os
import sys
import json
import urllib.request
from pathlib import Path

# Configuration
REPO_OWNER = "davidluzgouveia"
REPO_NAME = "kanji-data"
BRANCH = "master"
FILE_PATH = "kanji.json"

GITHUB_API_URL = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/commits/{BRANCH}"
GITHUB_RAW_URL = f"https://raw.githubusercontent.com/{REPO_OWNER}/{REPO_NAME}/{BRANCH}/{FILE_PATH}"

# Paths - configurable via environment variables for Docker
BASE_DIR = Path(__file__).parent.parent.parent  # c:\Projects\JLPTReference
SOURCE_DIR = Path(os.getenv("JMDICT_SOURCE_DIR", BASE_DIR / "database" / "source"))
STATE_FILE = Path(os.getenv("KANJI_REF_STATE_FILE", BASE_DIR / "database" / "scripts" / ".kanji_ref_commit"))
TARGET_FILE = SOURCE_DIR / "kanji" / "reference.json"


def get_latest_commit_sha():
    """Fetches the latest commit SHA from GitHub API."""
    print(f"Checking latest commit on {REPO_OWNER}/{REPO_NAME} ({BRANCH})...")
    try:
        req = urllib.request.Request(
            GITHUB_API_URL, 
            headers={"User-Agent": "JLPTReference-Sync-Script"}
        )
        with urllib.request.urlopen(req) as response:
            data = json.loads(response.read().decode())
            return data["sha"]
    except Exception as e:
        print(f"Error fetching latest commit: {e}")
        return None


def get_local_commit_sha():
    """Reads the local commit SHA if it exists."""
    if STATE_FILE.exists():
        return STATE_FILE.read_text().strip()
    return None


def download_kanji_json():
    """Downloads the kanji.json file from GitHub."""
    print(f"Downloading {FILE_PATH}...")
    try:
        req = urllib.request.Request(
            GITHUB_RAW_URL,
            headers={"User-Agent": "JLPTReference-Sync-Script"}
        )
        with urllib.request.urlopen(req) as response:
            data = response.read()
        
        # Ensure target directory exists
        TARGET_FILE.parent.mkdir(parents=True, exist_ok=True)
        
        # Write the file
        TARGET_FILE.write_bytes(data)
        print(f"Downloaded {len(data):,} bytes to {TARGET_FILE}")
        return True
    except Exception as e:
        print(f"Error downloading file: {e}")
        return False


def save_commit_sha(sha):
    """Save the commit SHA to state file."""
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(sha)


def main():
    print("=" * 60)
    print("Kanji Reference Data Sync Script")
    print("=" * 60)
    
    latest_sha = get_latest_commit_sha()
    if not latest_sha:
        print("Could not retrieve latest commit info.")
        # If we have the file already, continue
        if TARGET_FILE.exists():
            print("Existing file found locally, proceeding with what we have.")
            return 0
        else:
            print("No local file found and remote check failed. Critical error.")
            return 1
    
    local_sha = get_local_commit_sha()
    
    if local_sha == latest_sha and TARGET_FILE.exists():
        print(f"Kanji reference data is up to date (SHA: {local_sha[:7]}).")
        return 0
    
    if local_sha != latest_sha:
        print(f"Update available: {local_sha[:7] if local_sha else 'None'} -> {latest_sha[:7]}")
    else:
        print("File missing. Redownloading...")
    
    if download_kanji_json():
        save_commit_sha(latest_sha)
        print("Synchronization complete!")
        return 0
    else:
        return 1


if __name__ == "__main__":
    sys.exit(main())
