#!/usr/bin/env python3
"""
JMDict-Simplified Sync Script

Clones/updates the jmdict-simplified repository, builds the JSON files,
and copies them to the appropriate source directories for database initialization.
"""

import os
import sys
import json
import subprocess
import shutil
import glob
from pathlib import Path

# Configuration
REPO_URL = "https://github.com/scriptin/jmdict-simplified.git"
REPO_BRANCH = "master"

# Paths - configurable via environment variables for Docker
BASE_DIR = Path(__file__).parent.parent.parent  # c:\Projects\JLPTReference
CACHE_DIR = Path(os.getenv("JMDICT_CACHE_DIR", BASE_DIR / "database" / "scripts" / ".jmdict-cache"))
SOURCE_DIR = Path(os.getenv("JMDICT_SOURCE_DIR", BASE_DIR / "database" / "source"))
STATE_FILE = Path(os.getenv("JMDICT_STATE_FILE", BASE_DIR / "database" / "scripts" / ".jmdict_state"))
DATA_SEEDED_FLAG = Path(os.getenv("DATA_SEEDED_FLAG", BASE_DIR / "database" / "scripts" / ".data_seeded"))

# File mappings: source pattern -> target path (relative to SOURCE_DIR)
FILE_MAPPINGS = {
    "jmdict-all-*.json": "vocabulary/source.json",
    "jmdict-examples-eng-*.json": "vocabulary/vocabularyWithExamples/source.json",
    "jmnedict-all-*.json": "names/source.json",
    "kanjidic2-all-*.json": "kanji/source.json",
    "kradfile-*.json": "kradfile/source.json",
    "radkfile-*.json": "radfile/source.json",
}


def run_command(cmd, cwd=None, capture_output=False):
    """Run a shell command and return success status.
    
    When capture_output=False, streams output in real-time for Docker logs.
    """
    cmd_str = ' '.join(cmd) if isinstance(cmd, list) else cmd
    print(f"  Running: {cmd_str}", flush=True)
    
    try:
        if capture_output:
            result = subprocess.run(
                cmd, cwd=cwd, shell=isinstance(cmd, str),
                capture_output=True, text=True
            )
            return result.returncode == 0, result.stdout.strip()
        else:
            # Stream output in real-time for Docker visibility
            process = subprocess.Popen(
                cmd, cwd=cwd, shell=isinstance(cmd, str),
                stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                text=True, bufsize=1
            )
            # Read and print output line by line
            for line in process.stdout:
                print(f"    {line.rstrip()}", flush=True)
            process.wait()
            if process.returncode != 0:
                print(f"  Command failed with exit code {process.returncode}", flush=True)
                return False, None
            return True, None
    except subprocess.CalledProcessError as e:
        print(f"  Command failed with exit code {e.returncode}", flush=True)
        return False, None
    except Exception as e:
        print(f"  Command error: {e}", flush=True)
        return False, None


def clone_or_update_repo():
    """Clone the repository if it doesn't exist, or pull latest changes."""
    repo_dir = CACHE_DIR / "jmdict-simplified"
    
    if repo_dir.exists() and (repo_dir / ".git").exists():
        print("Updating existing jmdict-simplified repository...", flush=True)
        success, _ = run_command(["git", "fetch", "origin"], cwd=repo_dir)
        if success:
            success, _ = run_command(["git", "reset", "--hard", f"origin/{REPO_BRANCH}"], cwd=repo_dir)
        return success, repo_dir
    else:
        print("Cloning jmdict-simplified repository...", flush=True)
        CACHE_DIR.mkdir(parents=True, exist_ok=True)
        # Add --progress flag for better visibility in Docker logs
        success, _ = run_command(
            ["git", "clone", "--depth", "1", "--progress", "--branch", REPO_BRANCH, REPO_URL],
            cwd=CACHE_DIR
        )
        return success, repo_dir


def get_repo_commit(repo_dir):
    """Get the current commit SHA of the repository."""
    success, sha = run_command(
        ["git", "rev-parse", "HEAD"],
        cwd=repo_dir, capture_output=True
    )
    return sha if success else None


def load_state():
    """Load the previous state from state file."""
    if STATE_FILE.exists():
        try:
            return json.loads(STATE_FILE.read_text())
        except (json.JSONDecodeError, IOError):
            return {}
    return {}


def save_state(state):
    """Save state to state file."""
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(state, indent=2))


def get_gradle_command(repo_dir):
    """
    Get the appropriate Gradle command.
    Uses system 'gradle' if available (Docker image), otherwise falls back to gradlew.
    """
    # Check if we're in Docker with pre-installed Gradle
    if shutil.which("gradle"):
        return ["gradle"]
    
    # Fall back to gradlew wrapper for local development
    gradlew = repo_dir / ("gradlew.bat" if os.name == 'nt' else "gradlew")
    if os.name != 'nt':
        run_command(["chmod", "+x", str(gradlew)], cwd=repo_dir)
    return [str(gradlew)]


def check_dictionaries_changed(repo_dir):
    """
    Check if any dictionaries have changed using Gradle's checksum tasks.
    Returns dict of {dict_name: has_changed}
    """
    gradle_cmd = get_gradle_command(repo_dir)
    
    changes = {}
    for dict_name in ["jmdict", "jmnedict", "kanjidic"]:
        task = f"{dict_name}HasChanged"
        success, output = run_command(
            gradle_cmd + ["--quiet", task],
            cwd=repo_dir, capture_output=True
        )
        # Output is "YES" or "NO"
        changes[dict_name] = (output == "YES") if success else True  # Assume changed if check fails
        print(f"  {dict_name}: {'CHANGED' if changes[dict_name] else 'unchanged'}", flush=True)
    
    return changes


def run_gradle_download(repo_dir):
    """Run gradle download task to fetch dictionary XML files."""
    print("Downloading dictionary XML files...", flush=True)
    gradle_cmd = get_gradle_command(repo_dir)
    success, _ = run_command(gradle_cmd + ["download"], cwd=repo_dir)
    return success


def run_gradle_convert(repo_dir):
    """Run gradle convert task to generate JSON files."""
    print("Converting dictionaries to JSON (this may take a few minutes)...", flush=True)
    gradle_cmd = get_gradle_command(repo_dir)
    success, _ = run_command(gradle_cmd + ["convert"], cwd=repo_dir)
    return success


def copy_json_files(repo_dir):
    """Copy generated JSON files to source directories."""
    print("Copying JSON files to source directories...")
    json_dir = repo_dir / "build" / "dict-json"
    
    if not json_dir.exists():
        print(f"  ERROR: JSON output directory not found: {json_dir}")
        return False
    
    copied_count = 0
    for pattern, target_rel in FILE_MAPPINGS.items():
        matches = list(json_dir.glob(pattern))
        if not matches:
            print(f"  WARNING: No files matching '{pattern}' found")
            continue
        
        # Use the first match (should only be one per pattern)
        source_file = matches[0]
        target_file = SOURCE_DIR / target_rel
        
        # Ensure target directory exists
        target_file.parent.mkdir(parents=True, exist_ok=True)
        
        print(f"  Copying {source_file.name} -> {target_rel}")
        shutil.copy2(source_file, target_file)
        copied_count += 1
    
    print(f"  Copied {copied_count}/{len(FILE_MAPPINGS)} files")
    return copied_count > 0


def signal_database_rebuild():
    """Remove the data_seeded flag to trigger database rebuild."""
    if DATA_SEEDED_FLAG.exists():
        print("Signaling database rebuild by removing .data_seeded flag...")
        DATA_SEEDED_FLAG.unlink()
        return True
    return False


def main():
    print("=" * 60)
    print("JMDict-Simplified Sync Script")
    print("=" * 60)
    
    # Load previous state
    state = load_state()
    
    # Clone or update repository
    success, repo_dir = clone_or_update_repo()
    if not success:
        print("ERROR: Failed to clone/update repository")
        return 1
    
    # Get current commit
    current_commit = get_repo_commit(repo_dir)
    if not current_commit:
        print("ERROR: Could not determine repository commit")
        return 1
    
    print(f"Repository at commit: {current_commit[:7]}")
    
    # Check if we need to rebuild
    if state.get("commit") == current_commit and state.get("completed"):
        # Repo hasn't changed and we completed successfully before
        # But we should still check if dictionary files have changed (upstream releases)
        print("Repository unchanged. Checking for dictionary updates...")
        
        # Download to check for updates
        if not run_gradle_download(repo_dir):
            print("ERROR: Failed to download dictionary files")
            return 1
        
        changes = check_dictionaries_changed(repo_dir)
        
        if not any(changes.values()):
            print("All dictionaries are up to date. Nothing to do.")
            return 0
        else:
            print("Dictionary updates detected. Will rebuild.")
    else:
        print("First run or repository updated. Will build all dictionaries.")
        
        # Download dictionary XMLs
        if not run_gradle_download(repo_dir):
            print("ERROR: Failed to download dictionary files")
            return 1
    
    # Convert to JSON
    if not run_gradle_convert(repo_dir):
        print("ERROR: Failed to convert dictionaries to JSON")
        return 1
    
    # Copy files to source directories
    if not copy_json_files(repo_dir):
        print("ERROR: Failed to copy JSON files")
        return 1
    
    # Signal database rebuild if data was updated
    signal_database_rebuild()
    
    # Save state
    save_state({
        "commit": current_commit,
        "completed": True,
    })
    
    print("=" * 60)
    print("JMDict sync completed successfully!")
    print("=" * 60)
    return 0


if __name__ == "__main__":
    sys.exit(main())
