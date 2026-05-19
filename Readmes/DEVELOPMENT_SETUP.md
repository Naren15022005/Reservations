# Configuración de Desarrollo - FODUN Reservations

## Requisitos previos

1. **.NET 8 SDK** instalado
2. **Docker** corriendo SQL Server
3. **Git** para control de versiones

---

## 1. Configuración de Secretos Locales

### En Windows:

```bash
# Inicializar User Secrets para el proyecto API
cd src/Api
dotnet user-secrets init

# Configurar secretos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FODUN_Reservations_Dev;User Id=sa;Password=FodunDev@2024;Encrypt=false;TrustServerCertificate=true;"

dotnet user-secrets set "Authentication:Google:ClientId" "tu_google_client_id.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "tu_google_client_secret"

dotnet user-secrets set "Smtp:Username" "tu_email@gmail.com"
dotnet user-secrets set "Smtp:Password" "tu_gmail_app_password"

dotnet user-secrets set "AppSettings:JwtSecret" "tu_secret_key_de_32_caracteres_minimo"
```

### En Linux/Mac:

```bash
# Los User Secrets se almacenan en: ~/.microsoft/usersecrets/{ProjectGuid}/secrets.json
cd src/Api
dotnet user-secrets init

# Luego los mismos comandos que Windows
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
```

### Verificar secretos configurados:

```bash
dotnet user-secrets list
```

---

## 2. Obtener Credenciales

### Google OAuth

1. Ir a: https://console.cloud.google.com/
2. Crear nuevo proyecto: "FODUN Reservations"
3. Habilitar OAuth 2.0:
   - Productos > APIs y servicios > OAuth consent screen
   - Scopes: `email`, `profile`
4. Crear credenciales OAuth 2.0:
   - Tipo: Aplicación web
   - URIs autorizados: `http://localhost:5000/auth/google-callback`, `https://tu-dominio/auth/google-callback`
   - Copiar: `Client ID` y `Client Secret`

### Gmail App Password

1. Habilitar autenticación de dos factores en tu cuenta de Gmail
2. Ir a: https://myaccount.google.com/apppasswords
3. Seleccionar: Mail + Windows Computer (o tu SO)
4. Copiar la contraseña generada de 16 caracteres

### JWT Secret

Generar una clave segura de 32+ caracteres:

```bash
# En PowerShell:
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))

# En Bash/Linux:
openssl rand -base64 32
```

---

## 3. Estructura de appsettings

### Archivos presentes:

- `appsettings.json` - Configuración base (NO incluir secretos, solo placeholders)
- `appsettings.Development.json` - Configuración específica de desarrollo (puede tener secretos locales)
- `appsettings.example.json` - Template de referencia (NO incluir secretos)
- Secretos en User Secrets Store - NO commitear a Git

### Jerarquía de carga:

```
appsettings.json
    ↓ (override)
appsettings.{ASPNETCORE_ENVIRONMENT}.json
    ↓ (override)
User Secrets (solo en Development)
    ↓ (override)
Variables de entorno
```

---

## 4. Ejecutar en desarrollo

```bash
# Asegurarse de estar en la carpeta raíz del proyecto
cd /ruta/a/FODUN.Reservations

# Build
dotnet build

# Run
dotnet run --project src/Api/FODUN.Reservations.Api.csproj

# El API estará disponible en:
# HTTP:  http://localhost:5000
# HTTPS: https://localhost:7000
```

---

## 5. Verificar configuración

### Acceder a la aplicación:

1. Abrir navegador: `http://localhost:5000`
2. Navegar a `/Auth/Login`
3. Probar Google OAuth (si está configurado)
4. Verificar que se carga sin errores de secretos

### Revisar logs:

```bash
# Los logs aparecen en la consola de .NET y en: logs/fodun-{fecha}.log
tail -f logs/fodun-*.log
```

### Conectar a la BD:

```bash
# Desde SSMS o sqlcmd:
sqlcmd -S localhost,1433 -U sa -P FodunDev@2024 -d FODUN_Reservations -Q "SELECT COUNT(*) FROM Users"
```

---

## 6. Debugging

### Debug en Visual Studio:

1. Abrir solución: `FODUN.Reservations.sln`
2. Click derecho en proyecto `Api` → `Set as Startup Project`
3. F5 para debug (con breakpoints)
4. Logs en Output window

### Debug en VS Code:

1. Install C# extension
2. `.vscode/launch.json` ya configurado
3. F5 para iniciar debugger

### Debug en CLI:

```bash
# Con Serilog verbose
LOGGING__LOGLEVEL__DEFAULT=Debug dotnet run --project src/Api
```

---

## 7. Base de Datos en Desarrollo

### Conectar Docker SQL Server:

```bash
# Verificar que está corriendo
docker-compose ps

# Si no está corriendo:
docker-compose up -d

# Ejecutar comandos en el contenedor:
docker exec -it fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P FodunDev@2024 \
  -Q "SELECT @@VERSION"
```

### Migraciones (si es necesario):

```bash
# Crear migración
dotnet ef migrations add MigracionName \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj

# Aplicar migraciones
dotnet ef database update \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj
```

---

## 8. Tests en Desarrollo

```bash
# Ejecutar todos los tests
dotnet test

# Tests específicos de Unit
dotnet test tests/Unit/FODUN.Reservations.Tests.Unit.csproj

# Tests de integración
dotnet test tests/Integration/FODUN.Reservations.Tests.Integration.csproj

# Con cobertura (requiere ReportGenerator):
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

---

## 9. Problemas comunes

### "Secretos no encontrados"

```
Error: The entity type 'User' could not be mapped because there are no public setters for any of these properties...
```

**Solución:** Verificar que User Secrets están inicializados:

```bash
cd src/Api
dotnet user-secrets list
```

### "Connection refused" a BD

```
Error: Cannot connect to SQL Server at localhost:1433
```

**Solución:**

```bash
# Reiniciar Docker
docker-compose down
docker-compose up -d
sleep 45

# Verificar conexión
docker exec -it fodun_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P FodunDev@2024 -Q "SELECT 1"
```

### Conflicto de puertos

```
Error: Port 5000 already in use
```

**Solución:**

```bash
# Cambiar puerto en launchSettings.json:
"applicationUrl": "http://localhost:5001;https://localhost:7001"

# O matar el proceso:
lsof -i :5000
kill -9 <PID>
```

---

## 10. Próximos pasos

- [ ] Ejecutar setup completo (Docker + BD + API)
- [ ] Configurar Google OAuth
- [ ] Configurar Gmail App Password
- [ ] Ejecutar tests
- [ ] Comenzar desarrollo de endpoints

---

**Última actualización:** 2026-05-19  
**Versión:** 1.0
