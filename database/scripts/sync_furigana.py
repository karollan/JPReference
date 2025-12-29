import os
import requests
import json
import logging
from pathlib import Path

# Configuration
FURIGANA_REPO_API = "https://api.github.com/repos/Doublevil/JmdictFurigana/releases/latest"
SOURCE_DIR = Path(__file__).parent.parent / "source"
VOCAB_DIR = SOURCE_DIR / "vocabulary"
NAMES_DIR = SOURCE_DIR / "names"

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def download_file(url, target_path):
    logger.info(f"Downloading {url} to {target_path}...")
    response = requests.get(url, stream=True)
    response.raise_for_status()
    with open(target_path, 'wb') as f:
        for chunk in response.iter_content(chunk_size=8192):
            f.write(chunk)
    logger.info(f"Downloaded {target_path}")

def sync_furigana():
    # Ensure directories exist
    VOCAB_DIR.mkdir(parents=True, exist_ok=True)
    NAMES_DIR.mkdir(parents=True, exist_ok=True)

    try:
        logger.info("Fetching latest release info from GitHub...")
        response = requests.get(FURIGANA_REPO_API)
        response.raise_for_status()
        release_data = response.json()
        
        assets = release_data.get("assets", [])
        jmdict_url = next((a["browser_download_url"] for a in assets if a["name"] == "JmdictFurigana.json"), None)
        jmnedict_url = next((a["browser_download_url"] for a in assets if a["name"] == "JmnedictFurigana.json"), None)

        if jmdict_url:
            download_file(jmdict_url, VOCAB_DIR / "furigana.json")
        else:
            logger.error("JmdictFurigana.json not found in release assets.")

        if jmnedict_url:
            download_file(jmnedict_url, NAMES_DIR / "furigana.json")
        else:
            logger.error("JmnedictFurigana.json not found in release assets.")

    except Exception as e:
        logger.error(f"Failed to sync furigana data: {e}")
        raise

if __name__ == "__main__":
    sync_furigana()
