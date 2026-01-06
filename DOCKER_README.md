# JP Reference - Docker Setup

This guide will help you run the entire JP Reference application stack using Docker Compose, including the frontend, backend, and database.

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- Git (to clone the repository)

### Quick Start

1. **Copy environment configuration:**
   ```bash
   cp example.env .env
   ```

2. **Start all services:**
   ```bash
   docker-compose up --build -d
   ```

3. **Check service status:**
   ```bash
   docker-compose ps
   ```

## Service URLs

Once all services are running, you can access:

- **Frontend (Vue.js)**: http://localhost:3000
- **Backend API (.NET)**: http://localhost:5000
- **Swagger API Documentation**: http://localhost:5000/swagger
- **PgAdmin (Database Management)**: http://localhost:8080

### Default Credentials

**PgAdmin:**
- Email: `admin@jlptreference.com`
- Password: `admin123`

**Database:**
- Host: `localhost` (or `postgres` from within Docker)
- Port: `5432`
- Database: `jlptreference`
- Username: `jlptuser`
- Password: `jlptpassword`

## Environment Configuration

The application uses environment variables defined in the `.env` file:

```env
# Database Configuration
POSTGRES_DB=jlptreference
POSTGRES_USER=jlptuser
POSTGRES_PASSWORD=jlptpassword
POSTGRES_PORT=5432

# Service URLs
API_URL=http://localhost:5000/api
FRONTEND_URL=http://localhost:3000
BACKEND_PORT=5000
FRONTEND_PORT=3000

# PgAdmin
PGADMIN_EMAIL=admin@jlptreference.com
PGADMIN_PASSWORD=admin123
PGADMIN_PORT=8080
```

## Development Mode Features

When using Docker Compose for development, you get:

### **Frontend Hot-Reload**
- **Vue.js hot-reload**: Changes to `.vue`, `.ts`, `.js` files automatically reload in browser
- **Vite fast refresh**: Instant updates without losing component state
- **File watching**: Polling enabled for reliable file change detection in Docker

### **Backend Hot-Reload**
- **dotnet watch**: Automatic restart when C# files change
- **API endpoint updates**: Changes to controllers/services restart the API
- **Configuration changes**: Updates to `appsettings.json` trigger restart

### **Standard Docker Commands**

You can manage your services using standard `docker-compose` commands:

```bash
# View live logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f frontend
docker-compose logs -f backend

# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart frontend
```

## Available Commands

### Basic Operations

```bash
# Start all services
docker-compose up -d

# Start with rebuild
docker-compose up --build -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f frontend
docker-compose logs -f backend
docker-compose logs -f postgres
```

### Database Operations

```bash
# Run database migrations (if needed)
docker-compose exec backend dotnet ef database update

# Access database shell
docker-compose exec postgres psql -U jlptuser -d jlptreference

# Populate database with kanji data (optional)
docker-compose --profile tools up kanji-processor
```

### Development Operations

```bash
# Rebuild specific service
docker-compose build frontend
docker-compose build backend

# Execute commands in running containers
docker-compose exec frontend yarn install
docker-compose exec backend dotnet restore

# Access container shell
docker-compose exec frontend sh
docker-compose exec backend bash
```

## Architecture

The application consists of the following services:

### Frontend (Vue.js + Vuetify)
- **Port**: 3000
- **Technology**: Vue 3, TypeScript, Vuetify
- **Environment**: Uses `VITE_API_URL` to connect to backend

### Backend (.NET 8 Web API)
- **Port**: 5000
- **Technology**: ASP.NET Core, Entity Framework Core, PostgreSQL
- **Features**: CORS enabled for frontend, Swagger documentation

### Database (PostgreSQL 18)
- **Port**: 5432
- **Features**: Persistent data storage, health checks
- **Management**: PgAdmin interface available

### PgAdmin
- **Port**: 8080
- **Purpose**: Database administration interface

### Python data processors (Optional)
- **Purpose**: Fetch data and populate database with it
- **Profile**: `tools` (runs only when explicitly requested)

## Troubleshooting

### Common Issues

1. **Port conflicts**: If ports 3000, 5000, 5432, or 8080 are already in use:
   - Modify the port numbers in `.env` file
   - Update the docker-compose.yml accordingly

2. **Database connection issues**:
   - Ensure PostgreSQL service is healthy: `docker-compose ps`
   - Check database logs: `docker-compose logs postgres`

3. **Frontend not connecting to backend**:
   - Verify `API_URL` in `.env` file
   - Check backend logs: `docker-compose logs backend`
   - Ensure CORS is properly configured

4. **Services not starting**:
   - Check Docker Desktop is running
   - Verify all required ports are available
   - Check logs: `docker-compose logs`

### Reset Everything

```bash
# Stop and remove all containers, networks, and volumes
docker-compose down -v

# Remove all images (optional, will require rebuild)
docker-compose down --rmi all

# Start fresh
docker-compose up --build -d
```

## Project Structure

```
JLPTReference/
├── docker-compose.yml          # Main orchestration file
├── example.env                 # Environment template
├── .env                        # Your environment config (created from example.env)
├── frontend/
│   ├── Dockerfile              # Frontend container definition
│   ├── .dockerignore           # Frontend ignore patterns
│   └── ...
├── backend/
│   └── JLPTReference.Api/
│       ├── Dockerfile          # Backend container definition
│       ├── .dockerignore       # Backend ignore patterns
│       └── ...
└── database/
    ├── init                    # Has database initialization script
    └── ...
```

## Development Workflow

1. **Make changes** to your code
2. **Rebuild affected services**: `docker-compose build [service-name]`
3. **Restart services**: `docker-compose up -d [service-name]`
4. **View logs**: `docker-compose logs -f [service-name]`

## Notes

- The application uses Docker volumes for persistent data storage
- Database data persists between container restarts
- Frontend and backend have hot-reload enabled for development
- All services are connected via a custom Docker network (`jlpt-network`)

## Contributing

When making changes:
1. Update environment variables in `example.env` if needed and rename it to `.env`
2. Test the Docker setup locally
3. Update this README if you add new services or change configurations

---

**Happy learning Japanese! 🎌**
