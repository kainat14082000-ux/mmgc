#!/bin/bash
# Simple script to create database - used by init container
# This ensures database exists before SQLPad and webapp try to connect

set -e

MSSQL_HOST=${MSSQL_HOST:-mssql}
MSSQL_DATABASE=${MSSQL_DATABASE:-MMGC}

echo "Waiting for SQL Server to accept connections..."
until /opt/mssql-tools18/bin/sqlcmd -S "$MSSQL_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" -b > /dev/null 2>&1
do
    sleep 2
done

echo "Creating database '${MSSQL_DATABASE}' if it doesn't exist..."
/opt/mssql-tools18/bin/sqlcmd -S "$MSSQL_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '${MSSQL_DATABASE}') CREATE DATABASE [${MSSQL_DATABASE}]" -b

if [ $? -eq 0 ]; then
    echo "Database '${MSSQL_DATABASE}' is ready."
    exit 0
else
    echo "Failed to create database."
    exit 1
fi

