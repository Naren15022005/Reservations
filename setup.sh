#!/bin/bash
# FODUN Setup Script - Execute this to complete the setup

set -e

echo "════════════════════════════════════════════"
echo "FODUN Reservations - Automated Setup"
echo "════════════════════════════════════════════"
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Step 1: Start Docker
echo -e "${YELLOW}[STEP 1]${NC} Starting Docker and SQL Server..."
docker-compose up -d

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker started${NC}"
else
    echo -e "${RED}✗ Docker failed to start${NC}"
    echo "  Run: sudo usermod -aG docker \$USER && newgrp docker"
    exit 1
fi

# Step 2: Wait for SQL Server
echo -e "${YELLOW}[STEP 2]${NC} Waiting for SQL Server to be ready (40 seconds)..."
sleep 40

# Step 3: Verify connection
echo -e "${YELLOW}[STEP 3]${NC} Verifying SQL Server connection..."
docker exec -it fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -Q "SELECT 'Connection OK' AS Status"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ SQL Server connection verified${NC}"
else
    echo -e "${RED}✗ SQL Server connection failed${NC}"
    exit 1
fi

# Step 4: Create database schema
echo -e "${YELLOW}[STEP 4]${NC} Creating database schema and stored procedures..."
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -i /scripts/00-create-database.sql

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Database schema created${NC}"
else
    echo -e "${RED}✗ Database schema creation failed${NC}"
    exit 1
fi

# Step 5: Insert initial data
echo -e "${YELLOW}[STEP 5]${NC} Inserting initial data..."
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -i /scripts/01-insert-initial-data.sql

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Initial data inserted${NC}"
else
    echo -e "${RED}✗ Initial data insertion failed${NC}"
    exit 1
fi

# Step 6: Verify database
echo -e "${YELLOW}[STEP 6]${NC} Verifying database setup..."

TABLES=$(docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -d FODUN_Reservations \
  -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'" \
  -h -1 | tr -d ' ')

echo "  Tables created: $TABLES"
[ "$TABLES" = "8" ] && echo -e "${GREEN}  ✓ Expected 8 tables${NC}" || echo -e "${RED}  ✗ Expected 8 tables, got $TABLES${NC}"

SPS=$(docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -d FODUN_Reservations \
  -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'" \
  -h -1 | tr -d ' ')

echo "  Stored Procedures: $SPS"
[ "$SPS" = "4" ] && echo -e "${GREEN}  ✓ Expected 4 SP${NC}" || echo -e "${RED}  ✗ Expected 4 SP, got $SPS${NC}"

ACCOMMODATIONS=$(docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -d FODUN_Reservations \
  -Q "SELECT COUNT(*) FROM Accommodations" \
  -h -1 | tr -d ' ')

echo "  Accommodations: $ACCOMMODATIONS"
[ "$ACCOMMODATIONS" = "8" ] && echo -e "${GREEN}  ✓ Expected 8 accommodations${NC}" || echo -e "${RED}  ✗ Expected 8, got $ACCOMMODATIONS${NC}"

# Step 7: Build .NET solution
echo -e "${YELLOW}[STEP 7]${NC} Building .NET solution..."
dotnet build FODUN.Reservations.sln -c Release -q

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Solution built successfully${NC}"
else
    echo -e "${RED}✗ Solution build failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}════════════════════════════════════════════${NC}"
echo -e "${GREEN}✓ SETUP COMPLETED SUCCESSFULLY!${NC}"
echo -e "${GREEN}════════════════════════════════════════════${NC}"
echo ""
echo "Next steps:"
echo "  1. Run API: dotnet run --project src/Api/FODUN.Reservations.Api.csproj"
echo "  2. API will be available at: http://localhost:5000"
echo "  3. SQL Server connection: Server=localhost,1433; User=sa; Password=FodunDev@2024"
echo ""
echo "Stop containers: docker-compose down"
echo "View logs: docker-compose logs -f sqlserver"
echo ""

