# FODUN Reservations - Setup Instructions

## Prerequisites

1. **Docker Desktop** - https://www.docker.com/products/docker-desktop
2. **.NET 8 SDK** - https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. **SQL Server Management Studio (Optional)** - https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms
4. **User groups** - Add your user to docker group on Linux:
   ```bash
   sudo usermod -aG docker $USER
   newgrp docker
   ```

## Step 1: Start Docker and SQL Server

```bash
cd /path/to/FODUN.Reservations

# Start SQL Server container
docker-compose up -d

# Verify container is running
docker-compose ps

# Wait for healthcheck to pass (~40 seconds)
sleep 45

# Verify connection
docker exec -it fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -Q "SELECT 1 AS [Connection OK]"
```

## Step 2: Create Database Schema and Stored Procedures

```bash
# Run the complete database setup
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -i /scripts/00-create-database.sql

# Output should show: "Database schema and stored procedures created successfully!"
```

## Step 3: Insert Initial Data

```bash
# Insert seed data
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -i /scripts/01-insert-initial-data.sql

# Output should show: "Initial data inserted successfully!"
```

## Step 4: Verify Database

```bash
# Check tables exist
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -d FODUN_Reservations \
  -Q "SELECT COUNT(*) AS [Accommodations] FROM Accommodations"

# Check stored procedures exist
docker exec -i fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P FodunDev@2024 \
  -d FODUN_Reservations \
  -Q "SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'"
```

## Step 5: Build and Run .NET API

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build FODUN.Reservations.sln

# Run API
dotnet run --project src/Api/FODUN.Reservations.Api.csproj

# API should start on https://localhost:7000 or http://localhost:5000
```

## Connection Strings

**Development (Docker):**
```
Server=localhost,1433;Database=FODUN_Reservations;User Id=sa;Password=FodunDev@2024;Encrypt=false;TrustServerCertificate=true;
```

**Production:** (Requires environment configuration)
```
Server={YOUR_SERVER};Database=FODUN_Reservations;User Id={YOUR_USER};Password={YOUR_PASSWORD};Encrypt=true;
```

## Troubleshooting

### Docker Permission Denied
```bash
sudo usermod -aG docker $USER
newgrp docker
docker-compose up -d
```

### Port 1433 Already in Use
```bash
# Check what's using the port
lsof -i :1433

# Or modify docker-compose.yml to use different port:
# ports:
#   - "1434:1433"
```

### Connection Timeout
```bash
# Wait longer for SQL Server to start
sleep 60

# Check logs
docker-compose logs sqlserver
```

### Database Already Exists
```bash
# Drop and recreate
docker-compose down -v  # WARNING: Deletes data
docker-compose up -d
sleep 45
# Then run scripts again
```

## Scripts Location

- Database Schema: `scripts/00-create-database.sql`
- Stored Procedures: `scripts/stored-procedures.sql` (included in schema)
- Initial Data: `scripts/01-insert-initial-data.sql`

## Project Structure

```
src/
├── Domain/              # Entities, Enums, ValueObjects
├── Application/         # DTOs, Services, Validators
├── Infrastructure/      # DbContext, Persistence, Repositories
└── Api/                 # Controllers, Program.cs, appsettings

tests/
├── Unit/               # Unit tests
└── Integration/        # Integration tests

scripts/
├── 00-create-database.sql       # DDL + Stored Procedures
└── 01-insert-initial-data.sql   # Seed data

docker-compose.yml              # SQL Server container config
.env                            # Environment variables
```

## Next Steps

1. ✅ Docker setup complete
2. ✅ Database schema created
3. ✅ Stored procedures installed
4. ✅ Initial data seeded
5. 🔄 Run .NET API: `dotnet run --project src/Api`
6. 📝 Implement API endpoints
7. ✅ Add authentication/authorization
8. 🧪 Run tests: `dotnet test`

## Documentation

- Architecture: See `Readmes/` folder
- Database Design: See `Readmes/database.md`
- Project Timeline: See `Readmes/flujo.md`

---

**Last Updated:** 2026-05-17  
**Version:** 1.0
