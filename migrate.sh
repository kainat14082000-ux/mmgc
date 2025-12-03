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

# Check if webapp container exists
if ! docker ps -a --format '{{.Names}}' | grep -q "^mmgc-webapp$"; then
    echo -e "${RED}Error: mmgc-webapp container does not exist.${NC}"
    echo -e "${YELLOW}Please run ./setup.sh first to create the containers.${NC}"
    exit 1
fi

# Check if webapp container is running
if ! docker ps --format '{{.Names}}' | grep -q "^mmgc-webapp$"; then
    echo -e "${YELLOW}Warning: mmgc-webapp container is not running. Attempting to start...${NC}"
    DOCKER_COMPOSE_CMD="docker-compose"
    if ! command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker compose"
    fi
    if ! $DOCKER_COMPOSE_CMD start webapp 2>/dev/null; then
        echo -e "${RED}Error: Could not start mmgc-webapp container.${NC}"
        echo -e "${YELLOW}Please run ./setup.sh first to start the containers.${NC}"
        exit 1
    fi
    echo -e "${YELLOW}Waiting for container to be ready...${NC}"
    sleep 5
fi

# Wait for container to be fully ready
MAX_ATTEMPTS=20
ATTEMPT=0
while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker exec mmgc-webapp dotnet --version &> /dev/null 2>&1; then
        break
    fi
    ATTEMPT=$((ATTEMPT + 1))
    sleep 1
done

if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
    echo -e "${YELLOW}Warning: Container may not be fully ready, but proceeding...${NC}"
fi

# Function to run docker-compose commands
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker compose"
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
                exit 1
            fi
        fi
        
        if docker exec mmgc-webapp dotnet ef database update --project MMGC.csproj; then
            echo -e "${GREEN}Migrations applied successfully!${NC}"
        else
            echo -e "${RED}Failed to apply migrations${NC}"
            echo -e "${YELLOW}Check the logs: $DOCKER_COMPOSE_CMD logs webapp${NC}"
            exit 1
        fi
        ;;
    remove)
        echo -e "${YELLOW}Removing the last migration...${NC}"
        if docker exec -it mmgc-webapp dotnet ef migrations remove --project MMGC.csproj; then
            echo -e "${GREEN}Migration removed successfully!${NC}"
        else
            echo -e "${RED}Failed to remove migration${NC}"
            exit 1
        fi
        ;;
    list)
        echo -e "${GREEN}Listing migrations...${NC}"
        if docker exec -it mmgc-webapp dotnet ef migrations list --project MMGC.csproj; then
            echo -e "${GREEN}Migration list retrieved${NC}"
        else
            echo -e "${YELLOW}No migrations found or error occurred${NC}"
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
