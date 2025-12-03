#!/bin/bash
# Verification script to test Docker Compose setup

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}Docker Compose Setup Verification${NC}"
echo "===================================="
echo ""

# Check if Docker is running
echo -e "${BLUE}1. Checking Docker...${NC}"
if docker info > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Docker is running${NC}"
else
    echo -e "${RED}✗ Docker is not running${NC}"
    exit 1
fi

# Check if scripts exist and are executable
echo -e "${BLUE}2. Checking scripts...${NC}"
SCRIPTS=("healthcheck.sh" "create-db.sh" "migrate.sh")
for script in "${SCRIPTS[@]}"; do
    if [ -f "$script" ]; then
        if [ -x "$script" ]; then
            echo -e "${GREEN}✓ $script exists and is executable${NC}"
        else
            echo -e "${YELLOW}⚠ $script exists but is not executable (fixing...)${NC}"
            chmod +x "$script"
            echo -e "${GREEN}✓ $script is now executable${NC}"
        fi
    else
        echo -e "${RED}✗ $script NOT FOUND${NC}"
    fi
done

# Check if docker-compose.yml exists
echo -e "${BLUE}3. Checking docker-compose.yml...${NC}"
if [ -f "docker-compose.yml" ]; then
    echo -e "${GREEN}✓ docker-compose.yml exists${NC}"
    
    # Check if healthcheck script is referenced
    if grep -q "healthcheck.sh" docker-compose.yml; then
        echo -e "${GREEN}✓ healthcheck.sh is referenced in docker-compose.yml${NC}"
    else
        echo -e "${RED}✗ healthcheck.sh is NOT referenced in docker-compose.yml${NC}"
    fi
else
    echo -e "${RED}✗ docker-compose.yml NOT FOUND${NC}"
fi

# Check if .env file exists
echo -e "${BLUE}4. Checking environment file...${NC}"
if [ -f ".env" ]; then
    echo -e "${GREEN}✓ .env file exists${NC}"
    
    # Check for required variables
    if grep -q "MSSQL_SA_PASSWORD" .env; then
        echo -e "${GREEN}✓ MSSQL_SA_PASSWORD is set${NC}"
    else
        echo -e "${YELLOW}⚠ MSSQL_SA_PASSWORD not found in .env${NC}"
    fi
else
    echo -e "${YELLOW}⚠ .env file not found (will use defaults)${NC}"
fi

# Check if containers are running
echo -e "${BLUE}5. Checking containers...${NC}"
if docker ps --format '{{.Names}}' | grep -q "^mmgc-mssql$"; then
    echo -e "${GREEN}✓ mmgc-mssql container is running${NC}"
    
    # Check health status
    HEALTH=$(docker inspect --format='{{.State.Health.Status}}' mmgc-mssql 2>/dev/null || echo "unknown")
    if [ "$HEALTH" = "healthy" ]; then
        echo -e "${GREEN}✓ mmgc-mssql is healthy${NC}"
    elif [ "$HEALTH" = "starting" ]; then
        echo -e "${YELLOW}⚠ mmgc-mssql healthcheck is starting...${NC}"
    else
        echo -e "${YELLOW}⚠ mmgc-mssql health status: $HEALTH${NC}"
    fi
else
    echo -e "${YELLOW}⚠ mmgc-mssql container is not running${NC}"
fi

echo ""
echo -e "${GREEN}Verification complete!${NC}"
echo ""
echo -e "${BLUE}To start the setup:${NC}"
echo -e "  docker compose down -v"
echo -e "  docker compose up --build"
echo ""

