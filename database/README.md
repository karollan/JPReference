# JLPT Reference Database

This folder contains all the necessary files to run a PostgreSQL database for the JLPT Reference application using Docker.

## Quick Start

1. **Copy the environment file:**
   ```bash
   cp env.example .env
   ```

2. **Start the database:**
   ```bash
   docker-compose up -d
   ```

3. **Access the database:**
   - **PostgreSQL:** `localhost:5432`
   - **PgAdmin:** `http://localhost:8080`

## Services

### PostgreSQL Database
- **Container:** `jlpt-reference-db`
- **Port:** 5432 (configurable via `POSTGRES_PORT`)
- **Database:** `jlptreference`
- **Username:** `jlptuser`
- **Password:** `jlptpassword`

### PgAdmin (Database Management)
- **Container:** `jlpt-reference-pgadmin`
- **Port:** 8080 (configurable via `PGADMIN_PORT`)
- **Email:** `admin@jlptreference.com`
- **Password:** `admin123`

## Database Schema

The database includes the following table:

- **kanji** - Kanji characters with readings, meanings, JLPT levels, and metadata from kanji-jouyou.json
  - Fields: character, meanings, readings_on, readings_kun, stroke_count, grade, frequency, jlpt_old, jlpt_new
  - Automatically populated with data from kanji-jouyou.json (WaniKani fields removed)

## Configuration

Edit the `.env` file to customize:

```bash
# Database settings
POSTGRES_DB=jlptreference
POSTGRES_USER=jlptuser
POSTGRES_PASSWORD=jlptpassword
POSTGRES_PORT=5432

# PgAdmin settings
PGADMIN_EMAIL=admin@jlptreference.com
PGADMIN_PASSWORD=admin123
PGADMIN_PORT=8080
```

## Connection String for .NET

Use this connection string in your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=jlptreference;Username=jlptuser;Password=jlptpassword"
  }
}
```

## Commands

### Start services
```bash
docker-compose up -d
```

### Stop services
```bash
docker-compose down
```

### View logs
```bash
docker-compose logs -f postgres
```

### Reset database (removes all data)
```bash
docker-compose down -v
docker-compose up -d
```

### Access PostgreSQL directly
```bash
docker exec -it jlpt-reference-db psql -U jlptuser -d jlptreference
```

### Backup database
```bash
docker exec jlpt-reference-db pg_dump -U jlptuser jlptreference > backup.sql
```

### Restore database
```bash
docker exec -i jlpt-reference-db psql -U jlptuser -d jlptreference < backup.sql
```

## Sample Data

The database is automatically populated with kanji data from the `kanji-jouyou.json` file. The Python processor:
- Removes WaniKani-specific fields (starting with `wk_`)
- Inserts all kanji data into the database
- Handles conflicts with ON CONFLICT DO UPDATE

You can run the processor locally for development:
```bash
cd database/scripts
python run_local_processor.py
```

## Troubleshooting

### Port conflicts
If ports 5432 or 8080 are already in use, modify the `.env` file:
```bash
POSTGRES_PORT=5433
PGADMIN_PORT=8081
```

### Permission issues
On Linux/macOS, you might need to fix permissions:
```bash
sudo chown -R 999:999 ./postgres_data
```

### Container won't start
Check logs for errors:
```bash
docker-compose logs postgres
```

## Health Checks

The PostgreSQL service includes health checks. You can verify the database is ready by checking:
```bash
docker-compose ps
```

The health status should show "healthy" for the postgres service.
