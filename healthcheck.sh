#!/bin/bash
# Health check script for MSSQL Server
# This script is used by Docker healthcheck to verify SQL Server is ready

# Exit immediately if a command exits with a non-zero status
set -e

# Use the correct sqlcmd path (mssql-tools18 for SQL Server 2022)
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

# Check if sqlcmd exists
if [ ! -f "$SQLCMD" ]; then
    echo "Error: sqlcmd not found at $SQLCMD" >&2
    exit 1
fi

# Run healthcheck query
# -S: Server (localhost,1433 - port is required for some configurations)
# -U: Username (sa)
# -P: Password (from environment variable)
# -C: Trust server certificate (required for containerized SQL Server)
# -Q: Query to execute
# -b: Exit with error code on failure
# -h -1: Remove headers
# -W: Remove trailing whitespace
# -o /dev/null: Suppress output (healthcheck only needs exit code)

$SQLCMD \
    -S localhost,1433 \
    -U sa \
    -P "$MSSQL_SA_PASSWORD" \
    -C \
    -Q "SELECT 1" \
    -b \
    -h -1 \
    -W \
    -o /dev/null

# Exit code 0 = healthy, non-zero = unhealthy
exit $?
