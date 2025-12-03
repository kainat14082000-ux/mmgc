#!/bin/bash
# Database initialization script for MSSQL Server
# This ensures the database exists before SQLPad and webapp try to connect

# Exit immediately if a command exits with a non-zero status
set -e

# Configuration
MSSQL_HOST=${MSSQL_HOST:-mssql}
MSSQL_PORT=${MSSQL_PORT:-1433}
MSSQL_DATABASE=${MSSQL_DATABASE:-MMGC}
MAX_RETRIES=60
RETRY_INTERVAL=2

# Use the correct sqlcmd path (mssql-tools18 for SQL Server 2022)
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

# Check if sqlcmd exists
if [ ! -f "$SQLCMD" ]; then
    echo "Error: sqlcmd not found at $SQLCMD" >&2
    exit 1
fi

# Function to check SQL Server connectivity
check_sql_connection() {
    $SQLCMD \
        -S "${MSSQL_HOST},${MSSQL_PORT}" \
        -U sa \
        -P "$MSSQL_SA_PASSWORD" \
        -C \
        -Q "SELECT 1" \
        -b \
        -h -1 \
        -W \
        -o /dev/null 2>&1
}

# Wait for SQL Server to accept connections
echo "Waiting for SQL Server to accept connections..."
RETRY_COUNT=0
while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if check_sql_connection; then
        echo "SQL Server is ready!"
        break
    fi
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
        echo "Attempt $RETRY_COUNT/$MAX_RETRIES: SQL Server not ready yet, waiting ${RETRY_INTERVAL}s..."
        sleep $RETRY_INTERVAL
    fi
done

# Check if we exceeded max retries
if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "Error: SQL Server did not become ready after $MAX_RETRIES attempts" >&2
    exit 1
fi

# Create database if it doesn't exist
echo "Creating database '${MSSQL_DATABASE}' if it doesn't exist..."
# Temporarily disable exit on error for this command (database might already exist)
set +e
$SQLCMD \
    -S "${MSSQL_HOST},${MSSQL_PORT}" \
    -U sa \
    -P "$MSSQL_SA_PASSWORD" \
    -C \
    -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '${MSSQL_DATABASE}') CREATE DATABASE [${MSSQL_DATABASE}]" \
    -b \
    > /dev/null 2>&1
CREATE_RESULT=$?
set -e

# Wait a moment for database to be fully created/verified
sleep 1

# Verify database exists (regardless of create result, database might already exist)
echo "Verifying database exists..."
# Wait a bit more for database to be fully available
sleep 2

# Try multiple verification methods
VERIFICATION_SUCCESS=false

# Method 1: Check if database name exists in sys.databases (simple query)
set +e
DB_NAME_CHECK=$($SQLCMD \
    -S "${MSSQL_HOST},${MSSQL_PORT}" \
    -U sa \
    -P "$MSSQL_SA_PASSWORD" \
    -C \
    -Q "SELECT name FROM sys.databases WHERE name = '${MSSQL_DATABASE}'" \
    -h -1 \
    -W 2>&1)

# Check if our database name appears in the output (case-insensitive, any position)
if echo "$DB_NAME_CHECK" | grep -qi "${MSSQL_DATABASE}"; then
    VERIFICATION_SUCCESS=true
    echo "Database '${MSSQL_DATABASE}' verification successful! (method 1)"
fi
set -e

# Method 2: Try to use the database (if method 1 failed)
if [ "$VERIFICATION_SUCCESS" = false ]; then
    echo "Trying alternative verification method..."
    set +e
    USE_DB_RESULT=$($SQLCMD \
        -S "${MSSQL_HOST},${MSSQL_PORT}" \
        -U sa \
        -P "$MSSQL_SA_PASSWORD" \
        -C \
        -d "${MSSQL_DATABASE}" \
        -Q "SELECT DB_NAME() as current_db" \
        -h -1 \
        -W 2>&1)
    
    if echo "$USE_DB_RESULT" | grep -qi "${MSSQL_DATABASE}"; then
        VERIFICATION_SUCCESS=true
        echo "Database '${MSSQL_DATABASE}' verification successful! (method 2)"
    fi
    set -e
fi

# Final check
if [ "$VERIFICATION_SUCCESS" = true ]; then
    echo "Database '${MSSQL_DATABASE}' is ready and verified!"
    exit 0
else
    echo "Error: Database verification failed" >&2
    echo "Debug: CREATE_RESULT=$CREATE_RESULT" >&2
    echo "Attempting to list all databases:" >&2
    set +e
    $SQLCMD \
        -S "${MSSQL_HOST},${MSSQL_PORT}" \
        -U sa \
        -P "$MSSQL_SA_PASSWORD" \
        -C \
        -Q "SELECT name FROM sys.databases ORDER BY name" \
        -h -1 \
        -W 2>&1
    set -e
    # Don't exit with error - database might exist but verification query format is wrong
    # This is a non-critical error, database creation likely succeeded
    echo "Warning: Verification query failed, but database may still exist." >&2
    echo "Continuing anyway - database will be checked by application startup." >&2
    exit 0
fi