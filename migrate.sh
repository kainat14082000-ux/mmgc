#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Error handling - handle errors manually for better control
set -o pipefail  # Exit on pipe failure

echo -e "${GREEN}EF Core Migration Helper${NC}"
echo "================================"

# Load environment variables if .env exists
if [ -f .env ]; then
    set -a  # Automatically export all variables
    source .env
    set +a
fi

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Function to check if container is running and ready
check_container_ready() {
    local container_name=$1
    local max_attempts=${2:-30}
    local attempt=0
    
    # Check if container exists
    if ! docker ps -a --format '{{.Names}}' | grep -q "^${container_name}$"; then
        echo -e "${RED}Error: ${container_name} container does not exist.${NC}"
        return 1
    fi
    
    # Check if container is running
    if ! docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
        echo -e "${YELLOW}Warning: ${container_name} container is not running.${NC}"
        return 1
    fi
    
    # Wait for container to be fully ready (dotnet command available)
    echo -e "${BLUE}Waiting for ${container_name} to be ready...${NC}"
    while [ $attempt -lt $max_attempts ]; do
        if docker exec ${container_name} dotnet --version &> /dev/null 2>&1; then
            echo -e "${GREEN}${container_name} is ready!${NC}"
            return 0
        fi
        attempt=$((attempt + 1))
        if [ $((attempt % 5)) -eq 0 ]; then
            echo -e "${YELLOW}Waiting... (${attempt}/${max_attempts})${NC}"
        fi
        sleep 1
    done
    
    echo -e "${YELLOW}Warning: ${container_name} may not be fully ready, but proceeding...${NC}"
    return 0
}

# Check if webapp container exists and is ready
if ! check_container_ready "mmgc-webapp" 30; then
    echo -e "${YELLOW}Attempting to start containers...${NC}"
    DOCKER_COMPOSE_CMD="docker-compose"
    if ! command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker compose"
    fi
    
    # Try to start webapp container
    if ! $DOCKER_COMPOSE_CMD start webapp 2>/dev/null; then
        echo -e "${RED}Error: Could not start mmgc-webapp container.${NC}"
        echo -e "${YELLOW}Please run: docker compose up -d${NC}"
        exit 1
    fi
    
    # Wait again after starting
    if ! check_container_ready "mmgc-webapp" 60; then
        echo -e "${RED}Error: Container did not become ready after starting.${NC}"
        echo -e "${YELLOW}Check logs: docker logs mmgc-webapp${NC}"
        exit 1
    fi
fi

# Function to run docker-compose commands
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker compose"
fi

# Check if MSSQL is healthy (for migrations that need database)
if [ "$1" = "update" ] || [ "$1" = "add" ]; then
    echo -e "${BLUE}Checking MSSQL Server health...${NC}"
    if docker ps --format '{{.Names}}' | grep -q "^mmgc-mssql$"; then
        MSSQL_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' mmgc-mssql 2>/dev/null || echo "unknown")
        if [ "$MSSQL_HEALTH" != "healthy" ]; then
            echo -e "${YELLOW}Warning: MSSQL Server is not healthy (status: ${MSSQL_HEALTH})${NC}"
            echo -e "${YELLOW}Migrations may fail if database is not ready.${NC}"
            echo -e "${BLUE}Waiting 10 seconds for MSSQL to become ready...${NC}"
            sleep 10
        else
            echo -e "${GREEN}MSSQL Server is healthy${NC}"
        fi
    else
        echo -e "${YELLOW}Warning: MSSQL Server container not found${NC}"
    fi
fi

case "$1" in
    add)
        if [ -z "$2" ]; then
            echo -e "${RED}Error: Migration name is required.${NC}"
            echo -e "Usage: ./migrate.sh add <MigrationName>"
            exit 1
        fi
        echo -e "${GREEN}Creating migration: $2${NC}"
        if docker exec mmgc-webapp dotnet ef migrations add "$2" --project MMGC.csproj; then
            echo -e "${GREEN}Migration '$2' created successfully!${NC}"
            echo -e "${YELLOW}Don't forget to apply it: ./migrate.sh update${NC}"
        else
            echo -e "${RED}Failed to create migration${NC}"
            echo -e "${YELLOW}Check the logs: docker logs mmgc-webapp${NC}"
            exit 1
        fi
        ;;
    update)
        echo -e "${GREEN}Applying migrations to database...${NC}"
        
        # Check if Migrations folder exists
        if ! docker exec mmgc-webapp test -d Migrations 2>/dev/null; then
            echo -e "${YELLOW}No migrations found. Creating initial migration...${NC}"
            if docker exec mmgc-webapp dotnet ef migrations add InitialCreate --project MMGC.csproj; then
                echo -e "${GREEN}Initial migration created${NC}"
            else
                echo -e "${RED}Failed to create initial migration${NC}"
                echo -e "${YELLOW}Check the logs: docker logs mmgc-webapp${NC}"
                exit 1
            fi
        fi
        
        # Apply migrations
        echo -e "${BLUE}Running: dotnet ef database update${NC}"
        if docker exec mmgc-webapp dotnet ef database update --project MMGC.csproj; then
            echo -e "${GREEN}Migrations applied successfully!${NC}"
        else
            echo -e "${RED}Failed to apply migrations${NC}"
            echo -e "${YELLOW}Check the logs: docker logs mmgc-webapp${NC}"
            echo -e "${YELLOW}Check MSSQL logs: docker logs mmgc-mssql${NC}"
            exit 1
        fi
        ;;
    remove)
        echo -e "${YELLOW}Removing the last migration...${NC}"
        if docker exec -it mmgc-webapp dotnet ef migrations remove --project MMGC.csproj; then
            echo -e "${GREEN}Migration removed successfully!${NC}"
        else
            echo -e "${RED}Failed to remove migration${NC}"
            echo -e "${YELLOW}Check the logs: docker logs mmgc-webapp${NC}"
            exit 1
        fi
        ;;
    list)
        echo -e "${GREEN}Listing migrations...${NC}"
        if docker exec mmgc-webapp dotnet ef migrations list --project MMGC.csproj; then
            echo -e "${GREEN}Migration list retrieved${NC}"
        else
            echo -e "${YELLOW}No migrations found or error occurred${NC}"
            echo -e "${YELLOW}Check the logs: docker logs mmgc-webapp${NC}"
            exit 1
        fi
        ;;
    *)
        echo -e "${YELLOW}Usage:${NC}"
        echo -e "  ./migrate.sh add <MigrationName>    - Create a new migration"
        echo -e "  ./migrate.sh update                 - Apply migrations to database"
        echo -e "  ./migrate.sh remove                 - Remove the last migration"
        echo -e "  ./migrate.sh list                   - List all migrations"
        exit 1
        ;;
esac
