#!/bin/bash
# Database Seeder Script
# Creates default roles and admin user by triggering the webapp's built-in seeding

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}MMGC Database Seeder${NC}"
echo "======================"
echo ""

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

# Check Docker Compose command
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker compose"
fi

# Check if MSSQL container is running
if ! docker ps --format '{{.Names}}' | grep -q "^mmgc-mssql$"; then
    echo -e "${RED}Error: mmgc-mssql container is not running.${NC}"
    echo -e "${YELLOW}Please start the containers first: $DOCKER_COMPOSE_CMD up -d${NC}"
    exit 1
fi

# Check if MSSQL is healthy
echo -e "${BLUE}Checking MSSQL Server health...${NC}"
MSSQL_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' mmgc-mssql 2>/dev/null || echo "unknown")
if [ "$MSSQL_HEALTH" != "healthy" ]; then
    echo -e "${YELLOW}Warning: MSSQL Server is not healthy (status: ${MSSQL_HEALTH})${NC}"
    echo -e "${YELLOW}Waiting for MSSQL to become ready...${NC}"
    MAX_ATTEMPTS=30
    ATTEMPT=0
    while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
        MSSQL_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' mmgc-mssql 2>/dev/null || echo "unknown")
        if [ "$MSSQL_HEALTH" = "healthy" ]; then
            echo -e "${GREEN}MSSQL Server is now healthy${NC}"
            break
        fi
        ATTEMPT=$((ATTEMPT + 1))
        sleep 2
    done
    if [ "$MSSQL_HEALTH" != "healthy" ]; then
        echo -e "${RED}Error: MSSQL Server did not become healthy. Cannot proceed with seeding.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}MSSQL Server is healthy${NC}"
fi

# Check if webapp container exists
if ! docker ps -a --format '{{.Names}}' | grep -q "^mmgc-webapp$"; then
    echo -e "${RED}Error: mmgc-webapp container does not exist.${NC}"
    echo -e "${YELLOW}Please start the containers first: $DOCKER_COMPOSE_CMD up -d${NC}"
    exit 1
fi

# Start webapp if not running
if ! docker ps --format '{{.Names}}' | grep -q "^mmgc-webapp$"; then
    echo -e "${YELLOW}Starting webapp container...${NC}"
    $DOCKER_COMPOSE_CMD start webapp || $DOCKER_COMPOSE_CMD up -d webapp
    echo -e "${YELLOW}Waiting for webapp to be ready...${NC}"
    sleep 5
fi

# Wait for webapp container to be ready
echo -e "${BLUE}Waiting for webapp container to be ready...${NC}"
MAX_ATTEMPTS=30
ATTEMPT=0
while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker exec mmgc-webapp dotnet --version &> /dev/null 2>&1; then
        echo -e "${GREEN}Webapp container is ready!${NC}"
        break
    fi
    ATTEMPT=$((ATTEMPT + 1))
    sleep 1
done

if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
    echo -e "${YELLOW}Warning: Container may not be fully ready, but proceeding...${NC}"
fi

# Trigger seeding by restarting webapp (seeding runs on startup in Program.cs)
echo -e "${BLUE}Triggering database seeding...${NC}"
echo -e "${YELLOW}Restarting webapp container (seeding runs automatically on startup)...${NC}"
$DOCKER_COMPOSE_CMD restart webapp

# Wait for seeding to complete
echo -e "${YELLOW}Waiting for seeding to complete (this may take 10-15 seconds)...${NC}"
sleep 15

# Check webapp logs for seeding confirmation
echo -e "${BLUE}Checking seeding status from webapp logs...${NC}"
SEEDING_SUCCESS=$(docker logs mmgc-webapp 2>&1 | grep -i "seeding completed successfully" | tail -1)
if [ -n "$SEEDING_SUCCESS" ]; then
    echo -e "${GREEN}✓ Seeding completed (confirmed from logs)${NC}"
else
    echo -e "${YELLOW}⚠ Could not confirm seeding from logs, verifying database...${NC}"
fi

# Verify seeding by checking database
echo -e "${BLUE}Verifying seeding results in database...${NC}"
MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD:-YourStrong@Password123}
MSSQL_DATABASE=${MSSQL_DATABASE:-MMGC}

# Function to run sqlcmd safely (handles Windows/Git Bash path issues)
run_sqlcmd() {
    local query="$1"
    docker exec mmgc-mssql bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -P '$MSSQL_SA_PASSWORD' -C -d '$MSSQL_DATABASE' -Q \"$query\" -h -1 -W 2>/dev/null" || echo ""
}

ROLE_COUNT=$(run_sqlcmd "SELECT COUNT(*) FROM AspNetRoles" | tr -d '[:space:]' || echo "0")
USER_COUNT=$(run_sqlcmd "SELECT COUNT(*) FROM AspNetUsers" | tr -d '[:space:]' || echo "0")
ADMIN_EXISTS=$(run_sqlcmd "SELECT COUNT(*) FROM AspNetUsers WHERE Email = 'admin@mmgc.com'" | tr -d '[:space:]' || echo "0")

# Get role names
ROLES=$(run_sqlcmd "SELECT Name FROM AspNetRoles ORDER BY Name" | grep -v "^$" | grep -v "^Name$" | tr '\n' ', ' | sed 's/,$//' || echo "")

echo ""
# Ensure ROLE_COUNT, USER_COUNT, and ADMIN_EXISTS are numeric
ROLE_COUNT_NUM=$(echo "$ROLE_COUNT" | grep -E '^[0-9]+$' || echo "0")
USER_COUNT_NUM=$(echo "$USER_COUNT" | grep -E '^[0-9]+$' || echo "0")
ADMIN_EXISTS_NUM=$(echo "$ADMIN_EXISTS" | grep -E '^[0-9]+$' || echo "0")

if [ "$ROLE_COUNT_NUM" -ge "5" ] && [ "$USER_COUNT_NUM" -ge "1" ] && [ "$ADMIN_EXISTS_NUM" = "1" ]; then
    echo -e "${GREEN}✓ Seeding verified successfully!${NC}"
    echo -e "${GREEN}  - Roles created: $ROLE_COUNT_NUM${NC}"
    echo -e "${GREEN}  - Users created: $USER_COUNT_NUM${NC}"
    echo -e "${GREEN}  - Admin user exists: Yes${NC}"
    if [ -n "$ROLES" ]; then
        echo -e "${GREEN}  - Roles: $ROLES${NC}"
    fi
else
    echo -e "${YELLOW}⚠ Seeding may not have completed fully${NC}"
    echo -e "${YELLOW}  - Roles found: $ROLE_COUNT_NUM (expected: 5)${NC}"
    echo -e "${YELLOW}  - Users found: $USER_COUNT_NUM (expected: 1+)${NC}"
    echo -e "${YELLOW}  - Admin user exists: $([ "$ADMIN_EXISTS_NUM" = "1" ] && echo 'Yes' || echo 'No')${NC}"
    if [ -n "$ROLES" ]; then
        echo -e "${YELLOW}  - Existing roles: $ROLES${NC}"
    fi
    echo -e "${YELLOW}Check webapp logs: docker logs mmgc-webapp${NC}"
    echo -e "${YELLOW}You can also check logs for seeding messages:${NC}"
    echo -e "${YELLOW}  docker logs mmgc-webapp | grep -i seed${NC}"
fi

echo ""
echo -e "${BLUE}Default Admin Credentials:${NC}"
echo -e "  Email:    ${GREEN}admin@mmgc.com${NC}"
echo -e "  Password: ${GREEN}Admin@123${NC}"
echo ""
echo -e "${BLUE}Expected Roles:${NC}"
echo -e "  - Admin (Full system access)"
echo -e "  - Doctor (Doctor management)"
echo -e "  - Nurse (Nurse management)"
echo -e "  - LabStaff (Laboratory management)"
echo -e "  - Patient (Patient access)"
echo ""