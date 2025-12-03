#!/bin/bash
# Health check script for MSSQL Server
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -b -o /dev/null

