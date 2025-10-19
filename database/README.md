# JLPT Reference Database

This directory contains the database setup and data processing scripts for the JLPT Reference application.

## Data Sources

The database is populated with data from multiple sources:

### 1. Kanji Data (kanjidic2)
- **Source**: `database/source/kanji/source.json`
- **Reference**: `database/source/kanji/reference.json`
- **Description**: Comprehensive kanji data including readings, meanings, stroke counts, JLPT levels, and more

### 2. Vocabulary Data (JMdict)
- **Source**: `database/source/vocabulary/source.json`
- **Reference**: `database/source/vocabulary/reference.json`
- **Description**: Multi-language vocabulary with detailed grammatical information

### 3. Vocabulary with Examples
- **Source**: `database/source/vocabulary/vocabularyWithExamples/source.json`
- **Description**: English vocabulary entries with example sentences from Tatoeba

### 4. Radical Data
- **Radfile**: `database/source/radfile/source.json`
- **Kradfile**: `database/source/kradfile/source.json`
- **Description**: Radical information and kanji decomposition data

### 5. Names Data
- **Source**: `database/source/names/source.json`
- **Description**: Japanese names data

## Database Schema

### Tables

1. **kanji** - Main kanji table with comprehensive information
2. **kanji_radicals** - Radical relationships for kanji
3. **kanji_decompositions** - Component decomposition for kanji
4. **vocabulary** - Vocabulary entries with multiple languages
5. **vocabulary_examples** - Example sentences for vocabulary
6. **radicals** - Radical information

### Key Features

- **JLPT Level Mapping**: Both old and new JLPT levels are supported
- **Cross-references**: Vocabulary entries are linked with examples
- **Multi-language Support**: Vocabulary supports multiple languages
- **Comprehensive Kanji Data**: Includes readings, meanings, stroke counts, and more

## Data Processing

### Comprehensive Processor

The `process_all_data.py` script processes all data sources:

```bash
# Run the comprehensive processor
python database/scripts/process_all_data.py
```

### Features

1. **JLPT Level Mapping**: Automatically maps JLPT levels from reference files
2. **Cross-reference Processing**: Links vocabulary with examples
3. **Data Validation**: Ensures data integrity during processing
4. **Batch Processing**: Efficiently processes large datasets

### Processing Steps

1. Load JLPT level mappings from reference files
2. Process kanji data from kanjidic2
3. Process vocabulary data from JMdict
4. Process vocabulary examples
5. Process radical and decomposition data
6. Update database with all processed data

## Usage

### Docker Compose

The comprehensive processor runs automatically when using Docker Compose:

```bash
# Start all services including data processing
docker-compose up
```

### Local Development

For local development, you can run the processor manually:

```bash
# Install dependencies
pip install -r database/scripts/requirements.txt

# Run the comprehensive processor
python database/scripts/run_local_processor.py
```

## Data Structure

### Kanji Data Structure

```json
{
  "literal": "一",
  "codepoints": [...],
  "radicals": [...],
  "misc": {
    "grade": 1,
    "strokeCounts": [1],
    "jlptLevel": 5
  },
  "readingMeaning": {
    "groups": [...],
    "nanori": [...]
  }
}
```

### Vocabulary Data Structure

```json
{
  "id": "1000000",
  "kanji": [...],
  "kana": [...],
  "sense": [
    {
      "partOfSpeech": [...],
      "gloss": [...],
      "examples": [...]
    }
  ]
}
```

## API Endpoints

### Kanji Endpoints

- `GET /api/kanji` - List kanji with filtering
- `GET /api/kanji/{id}` - Get specific kanji by ID
- `GET /api/kanji/character/{character}` - Get kanji by character

### Vocabulary Endpoints

- `GET /api/vocabulary` - List vocabulary with filtering
- `GET /api/vocabulary/{jmdictId}` - Get specific vocabulary entry
- `GET /api/vocabulary/jlpt/{level}` - Get vocabulary by JLPT level
- `GET /api/vocabulary/common` - Get common vocabulary

## Environment Variables

- `POSTGRES_HOST` - Database host (default: postgres)
- `POSTGRES_PORT` - Database port (default: 5432)
- `POSTGRES_DB` - Database name (default: jlptreference)
- `POSTGRES_USER` - Database user (default: jlptuser)
- `POSTGRES_PASSWORD` - Database password (default: jlptpassword)

## Troubleshooting

### Common Issues

1. **Database Connection**: Ensure PostgreSQL is running and accessible
2. **Data Files**: Verify all source JSON files are present
3. **Permissions**: Ensure proper file permissions for data access
4. **Memory**: Large datasets may require sufficient memory

### Logs

Check Docker logs for processing status:

```bash
# Check data processor logs
docker logs jlpt-reference-data-processor

# Check database logs
docker logs jlpt-reference-db
```