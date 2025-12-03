# Docker Setup for MMGC

This project includes a complete Docker setup with MSSQL Server, SQLPad, and the ASP.NET Core web application with hot reload support.

## Prerequisites

- Docker and Docker Compose installed
- Bash shell (for setup scripts)

## Quick Start

1. **Copy the environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` file** with your desired configuration:
   - MSSQL Server password
   - Database name
   - Ports for all services
   - SQLPad admin credentials

3. **Run the setup script:**
   ```bash
   ./setup.sh
   ```

   This will:
   - Build Docker images
   - Start all containers (MSSQL, SQLPad, Web App)
   - Wait for MSSQL to be ready
   - Create the database
   - Run EF Core migrations

## Services

### MSSQL Server
- **Port:** Configurable via `MSSQL_PORT` (default: 1433)
- **Database:** Configurable via `MSSQL_DATABASE` (default: MMGC)
- **SA Password:** Configurable via `MSSQL_SA_PASSWORD`

### SQLPad (Database UI)
- **Port:** Configurable via `SQLPAD_PORT` (default: 3000)
- **URL:** http://localhost:3000 (or your configured port)
- **Admin Email:** Configurable via `SQLPAD_ADMIN`
- **Admin Password:** Configurable via `SQLPAD_ADMIN_PASSWORD`

### ASP.NET Core Web Application
- **Port:** Configurable via `WEBAPP_PORT` (default: 8080)
- **URL:** http://localhost:8080 (or your configured port)
- **Hot Reload:** Enabled - code changes are automatically detected and the app restarts

## Environment Variables

All configuration is done through the `.env` file. Key variables:

```bash
# MSSQL Server Configuration
MSSQL_SA_PASSWORD=YourStrong@Password123
MSSQL_DATABASE=MMGC
MSSQL_PORT=1433

# SQLPad Configuration
SQLPAD_PORT=3000
SQLPAD_ADMIN=admin@example.com
SQLPAD_ADMIN_PASSWORD=admin123

# Web Application Configuration
WEBAPP_PORT=8080

# ASP.NET Core Environment
ASPNETCORE_ENVIRONMENT=Development
```

## EF Core Migrations

### Using the migration helper script:

```bash
# Create a new migration
./migrate.sh add MigrationName

# Apply migrations to database
./migrate.sh update

# Remove the last migration
./migrate.sh remove

# List all migrations
./migrate.sh list
```

### Manual migration commands:

```bash
# Create migration
docker exec -it mmgc-webapp dotnet ef migrations add MigrationName --project MMGC.csproj

# Apply migrations
docker exec -it mmgc-webapp dotnet ef database update --project MMGC.csproj

# List migrations
docker exec -it mmgc-webapp dotnet ef migrations list --project MMGC.csproj
```

## Hot Reload

Hot reload is enabled by default. When you make changes to your code:
- The `dotnet watch` tool automatically detects changes
- The application restarts automatically
- No need to manually restart containers

**Note:** Some changes (like adding new dependencies) may require rebuilding the container:
```bash
docker-compose build webapp
docker-compose up -d webapp
```

## Common Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f webapp
docker-compose logs -f mssql
docker-compose logs -f sqlpad

# Restart a specific service
docker-compose restart webapp

# Rebuild and restart
docker-compose build webapp
docker-compose up -d webapp

# Access MSSQL Server directly
docker exec -it mmgc-mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword"
```

## Project Structure

- `Dockerfile` - ASP.NET Core application Docker image
- `docker-compose.yml` - Multi-container Docker setup
- `setup.sh` - Initial setup and migration script
- `migrate.sh` - EF Core migration helper script
- `.env.example` - Environment variables template
- `.dockerignore` - Files to exclude from Docker build

## Troubleshooting

### MSSQL Server not starting
- Check if port 1433 is already in use
- Verify password meets complexity requirements (at least 8 characters, uppercase, lowercase, numbers, special characters)
- Check logs: `docker-compose logs mssql`

### Migrations failing
- Ensure MSSQL Server is healthy: `docker-compose ps`
- Check connection string in `.env` file
- Verify database exists: `docker exec -it mmgc-mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword" -Q "SELECT name FROM sys.databases"`

### Hot reload not working
- Ensure volumes are mounted correctly
- Check `DOTNET_USE_POLLING_FILE_WATCHER` is set to `true`
- Verify file permissions on mounted volumes

### Port conflicts
- Change ports in `.env` file
- Restart containers: `docker-compose down && docker-compose up -d`

## Features

- ✅ MSSQL Server 2022
- ✅ SQLPad for database management UI
- ✅ ASP.NET Core 8.0 with hot reload
- ✅ Entity Framework Core 8.0
- ✅ ASP.NET Core Identity for authentication/authorization
- ✅ Configurable ports via environment variables
- ✅ Automatic database creation
- ✅ EF Core migration support
- ✅ Persistent data volumes

