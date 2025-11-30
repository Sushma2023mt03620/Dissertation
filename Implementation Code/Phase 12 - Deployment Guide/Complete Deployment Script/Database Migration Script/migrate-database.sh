#!/bin/bash

set -e
 
echo "Running database migrations..."

# Get SQL connection details
SQL_SERVER="sql-automotive-platform.database.windows.net"
SQL_DATABASE="sqldb-manufacturing"
SQL_USER="sqladmin"

echo "Enter SQL password:"
read -s SQL_PASSWORD

# Run EF Core migrations
dotnet ef database update \
    --project src/ProductionLineService \
    --connection "Server=$SQL_SERVER;Database=$SQL_DATABASE;User Id=$SQL_USER;Password=$SQL_PASSWORD;Encrypt=True;"

echo "Database migration completed!"
