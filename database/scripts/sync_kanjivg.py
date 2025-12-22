#!/usr/bin/env python3
import os
import sys
import json
import urllib.request
import tarfile
import io
import shutil
from pathlib import Path

# Configuration
REPO_OWNER = "karollan"
REPO_NAME = "kanjivg-storage"
BRANCH = "master"
GITHUB_API_URL = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/commits/{BRANCH}"
GITHUB_TAR_URL = f"https://github.com/{REPO_OWNER}/{REPO_NAME}/archive/refs/heads/{BRANCH}.tar.gz"

# Local paths
# Inside docker, we'll set these via ENV in docker-compose
# Defaults are set for running directly on the host from the scripts directory
BASE_DIR = Path(__file__).parent.parent.parent # c:\Projects\JLPTReference
TARGET_DIR = Path(os.getenv("KANJIVG_TARGET_DIR", BASE_DIR / "frontend" / "public" / "kanjivg"))
STATE_FILE = Path(os.getenv("KANJIVG_STATE_FILE", BASE_DIR / "database" / "scripts" / ".kanjivg_commit"))

def get_latest_commit_sha():
    """Fetches the latest commit SHA from GitHub API."""
    print(f"Checking latest commit on {REPO_OWNER}/{REPO_NAME} ({BRANCH})...")
    try:
        # Use a User-Agent to avoid 403 from GitHub API
        req = urllib.request.Request(GITHUB_API_URL, headers={"User-Agent": "JLPTReference-Sync-Script"})
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

def download_and_extract(sha):
    """Downloads the repository tarball and extracts the kanji directory."""
    print(f"Downloading KanjiVG data (commit {sha[:7]})...")
    try:
        req = urllib.request.Request(GITHUB_TAR_URL, headers={"User-Agent": "JLPTReference-Sync-Script"})
        with urllib.request.urlopen(req) as response:
            compressed_data = response.read()
            
        print("Extracting files...")
        with tarfile.open(fileobj=io.BytesIO(compressed_data), mode="r:gz") as tar:
            # The top-level directory in the tarball is user-repo-sha
            # We want repo-branch/kanji/
            root_in_tar = f"{REPO_NAME}-{BRANCH}"
            
            # Create target directory if it doesn't exist
            TARGET_DIR.mkdir(parents=True, exist_ok=True)
            
            # Extract only files from the 'kanji' subdirectory
            count = 0
            for member in tar.getmembers():
                if member.name.startswith(f"{root_in_tar}/kanji/") and member.isfile():
                    # Strip the repository prefix and 'kanji/' prefix for final destination
                    member.name = os.path.basename(member.name)
                    tar.extract(member, path=TARGET_DIR)
                    count += 1
            
            print(f"Successfully extracted {count} SVG files to {TARGET_DIR}")
            
            # Save the new commit SHA
            STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
            STATE_FILE.write_text(sha)
            return True
    except Exception as e:
        print(f"Error during download/extraction: {e}")
        return False

def main():
    print("=" * 60)
    print("KanjiVG Synchronization Script")
    print("=" * 60)
    
    latest_sha = get_latest_commit_sha()
    if not latest_sha:
        print("Could not retrieve latest commit info. Skipping sync.")
        # If we have files already, we might want to continue anyway
        if TARGET_DIR.exists() and any(TARGET_DIR.iterdir()):
            print("Existing files found locally, proceeding with what we have.")
            return 0
        else:
            print("No local files found and remote check failed. Critical error.")
            return 1

    local_sha = get_local_commit_sha()
    
    if local_sha == latest_sha and TARGET_DIR.exists() and any(TARGET_DIR.iterdir()):
        print(f"KanjiVG is up to date (SHA: {local_sha[:7]}).")
        return 0
    else:
        if local_sha != latest_sha:
            print(f"Update available: {local_sha[:7] if local_sha else 'None'} -> {latest_sha[:7]}")
        else:
            print("Consistency check: Files missing. Redownloading...")
            
        if download_and_extract(latest_sha):
            print("Synchronization complete!")
            return 0
        else:
            return 1

if __name__ == "__main__":
    sys.exit(main())
