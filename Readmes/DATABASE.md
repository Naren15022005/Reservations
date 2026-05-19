# FODUN — Documentación de Base de Datos

**Proyecto:** Sistema de Reservas de Sedes Recreativas y Apartamentos  
**Motor:** Microsoft SQL Server 2019+  
**Base de datos:** `FODUN_Reservations`  
**ORM:** Entity Framework Core 8.0  

---

## Tabla de contenidos

1. [Cómo aplicar la base de datos](#1-cómo-aplicar-la-base-de-datos)
2. [Diagrama de relaciones](#2-diagrama-de-relaciones)
3. [Tablas — descripción completa](#3-tablas--descripción-completa)
4. [Índices y por qué existen](#4-índices-y-por-qué-existen)
5. [Relaciones y claves foráneas](#5-relaciones-y-claves-foráneas)
6. [Stored Procedures](#6-stored-procedures)
7. [Datos iniciales (Seed)](#7-datos-iniciales-seed)
8. [Migraciones con EF Core](#8-migraciones-con-ef-core)
9. [Arquitectura de persistencia](#9-arquitectura-de-persistencia)
10. [Decisiones de diseño](#10-decisiones-de-diseño)

---

## 1. Cómo aplicar la base de datos

Ejecutar los scripts en orden desde SQL Server Management Studio (SSMS) o desde `sqlcmd`:

```sql
-- 1. Crear la base de datos
scripts/sql/01_CreateDatabase.sql

-- 2. Crear las tablas e índices
scripts/sql/02_CreateSchema.sql

-- 3. Crear los Stored Procedures
scripts/sql/03_StoredProcedures.sql

-- 4. Insertar datos iniciales (sedes, alojamientos, tarifas)
scripts/sql/04_SeedData.sql
```

**Con sqlcmd desde terminal:**
```bash
sqlcmd -S . -U sa -P TuPassword123! -i scripts/sql/01_CreateDatabase.sql
sqlcmd -S . -U sa -P TuPassword123! -i scripts/sql/02_CreateSchema.sql
sqlcmd -S . -U sa -P TuPassword123! -i scripts/sql/03_StoredProcedures.sql
sqlcmd -S . -U sa -P TuPassword123! -i scripts/sql/04_SeedData.sql
```

**Con EF Core (alternativa):**
```bash
# Desde la carpeta raíz del proyecto
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj

dotnet ef database update \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj
```

> **Nota:** Los scripts SQL y las migraciones EF Core producen el mismo esquema. En producción se recomienda usar los scripts SQL directamente. Las migraciones son útiles para desarrollo y CI.

---

## 2. Diagrama de relaciones

```
┌─────────────┐       ┌────────────────────┐       ┌──────────────┐
│   Users     │       │   Accommodations   │       │    Seats     │
│─────────────│       │────────────────────│       │──────────────│
│ Id (PK)     │       │ Id (PK)            │1─────N│ Id (PK)      │
│ DocumentNum │       │ Code (UQ)          │       │ Accommodation│
│ FullName    │       │ Name               │       │ SeatNumber   │
│ Email (UQ)  │       │ Type               │       │ Type         │
│ PasswordHash│       │ City               │       │ Capacity     │
│ ...         │       │ MaxCapacity        │       │ Amenities    │
└──────┬──────┘       └────────────────────┘       └──────┬───────┘
       │                                                   │
       │1                                            ┌─────┴──────┐
       │                                             │            │
       N                                             1N           1N
┌──────┴──────┐                              ┌───────┴──┐  ┌─────┴──────┐
│ Reservations│                              │ Tariffs  │  │ Blackouts  │
│─────────────│                              │──────────│  │────────────│
│ Id (PK)     │                              │ Id (PK)  │  │ Id (PK)    │
│ UserId (FK) │                              │ SeatId   │  │ SeatId     │
│ CheckInDate │                              │ Season   │  │ StartDate  │
│ CheckOutDate│                              │ MinPers. │  │ EndDate    │
│ TotalPersons│                              │ MaxPers. │  │ Reason     │
│ Status      │                              │ Price    │  └────────────┘
│ TotalCost   │
└──────┬──────┘
       │1
       N
┌──────┴────────────┐       ┌──────────────┐
│  ReservationItems │       │  AuditLogs   │
│───────────────────│       │──────────────│
│ Id (PK)           │       │ Id (PK, IDENT│
│ ReservationId (FK)│       │ UserId       │
│ SeatId (FK)       │       │ Action       │
│ TariffId (FK,NULL)│       │ EntityType   │
│ PricePerNight     │       │ OldValues    │
│ Nights            │       │ NewValues    │
└───────────────────┘       └──────────────┘
```

---

## 3. Tablas — descripción completa

### Users — Usuarios del sistema

Almacena todos los usuarios registrados. Maneja autenticación, bloqueo de cuenta y recuperación de contraseña.

| Columna                  | Tipo             | Nulo | Descripción                                              |
|--------------------------|------------------|------|----------------------------------------------------------|
| `Id`                     | UNIQUEIDENTIFIER | NO   | Clave primaria. Se genera automáticamente con `NEWID()`. |
| `DocumentNumber`         | NVARCHAR(20)     | NO   | Número de documento (cédula). Único en la tabla.         |
| `FullName`               | NVARCHAR(255)    | NO   | Nombre completo del usuario.                             |
| `Email`                  | NVARCHAR(255)    | NO   | Correo electrónico. Único. Se guarda en minúsculas.      |
| `PhoneNumber`            | NVARCHAR(20)     | SÍ   | Teléfono de contacto, opcional.                          |
| `PasswordHash`           | NVARCHAR(MAX)    | NO   | Hash PBKDF2-SHA256 de la contraseña. Nunca texto plano.  |
| `PasswordSalt`           | NVARCHAR(MAX)    | SÍ   | Salt aleatorio de 32 bytes en Base64 para el hash.       |
| `IsEmailConfirmed`       | BIT              | NO   | `0` hasta que el usuario confirme su correo.             |
| `EmailConfirmationToken` | NVARCHAR(MAX)    | SÍ   | Token GUID para confirmar el email. Se borra al confirmar.|
| `PasswordResetToken`     | NVARCHAR(MAX)    | SÍ   | Token para resetear contraseña. Caduca en 30 minutos.    |
| `PasswordResetTokenExpiry` | DATETIME2      | SÍ   | Fecha de expiración del token de reset.                  |
| `LockoutEnabled`         | BIT              | NO   | `1` si la cuenta está actualmente bloqueada.             |
| `LockoutEndTime`         | DATETIME2        | SÍ   | Hasta cuándo está bloqueada (15 minutos tras 5 intentos).|
| `FailedLoginAttempts`    | INT              | NO   | Contador de intentos fallidos. Se resetea al loguearse.  |
| `IsActive`               | BIT              | NO   | Borrado lógico. `0` = usuario desactivado.               |
| `CreatedAt`              | DATETIME2        | NO   | Fecha de creación en UTC.                                |
| `UpdatedAt`              | DATETIME2        | NO   | Fecha de última modificación en UTC.                     |
| `CreatedBy`              | NVARCHAR(255)    | SÍ   | Usuario que creó el registro (auditoría).                |
| `UpdatedBy`              | NVARCHAR(255)    | SÍ   | Usuario que modificó el registro (auditoría).            |

**Restricciones:** `UQ_Users_Email`, `UQ_Users_DocumentNumber`

---

## 4. Índices y por qué existen

| Índice                            | Tabla             | Columnas                         | Por qué existe                                                                 |
|-----------------------------------|-------------------|----------------------------------|--------------------------------------------------------------------------------|
| `IX_Users_Email`                  | Users             | Email                            | El login busca por email. Sin índice sería un full scan.                       |
| `IX_Tariffs_SeatId_Season`        | Tariffs           | SeatId, Season                   | Las consultas de tarifas siempre filtran por alojamiento y temporada.          |
| `IX_Blackouts_SeatId_Dates`       | Blackouts         | SeatId, StartDate, EndDate       | La consulta de disponibilidad verifica bloqueos por alojamiento y rango de fechas. |
| `IX_ReservationItems_SeatId`      | ReservationItems  | SeatId (INCLUDE PricePerNight, Nights) | Consulta de disponibilidad: busca reservas activas por alojamiento. El INCLUDE evita lookup adicional. |
| `IX_Reservations_UserId_Status`   | Reservations      | UserId, Status                   | "Mis reservas" filtra por usuario. El status permite filtrar las activas sin leer toda la tabla. |

**Índices de restricción única (también aceleran búsquedas):**
- `UQ_Users_Email` — login y validación de duplicados.
- `UQ_Users_DocumentNumber` — validación de duplicados en registro.
- `UQ_Accommodations_Code` — búsqueda de sede por código corto.
- `UQ_Seats_AccommodationId_SeatNumber` — integridad: no puede repetirse el número dentro de una sede.

---

## 5. Relaciones y claves foráneas

```
Accommodations (1) ──────────── (N) Seats
Seats          (1) ──────────── (N) Tariffs        [CASCADE DELETE]
Seats          (1) ──────────── (N) Blackouts       [CASCADE DELETE]
Users          (1) ──────────── (N) Reservations    [CASCADE DELETE]
Reservations   (1) ──────────── (N) ReservationItems [CASCADE DELETE]
Seats          (1) ──────────── (N) ReservationItems [NO CASCADE — FK opcional]
Tariffs        (1) ──────────── (N) ReservationItems [FK nullable — tarifa puede haberse eliminado]
```

**Comportamiento de cascada:**
- `Accommodations → Seats`: Si se elimina una sede, se eliminan todos sus alojamientos.
- `Seats → Tariffs / Blackouts`: Si se elimina un alojamiento, se eliminan sus tarifas y bloqueos.
- `Users → Reservations → ReservationItems`: Si se elimina un usuario, se eliminan todas sus reservas y sus ítems.
- `Seats → ReservationItems`: **Sin cascada.** Un ítem de reserva no debe eliminarse si se desactiva un alojamiento; se desactiva el alojamiento con borrado lógico.
- `Tariffs → ReservationItems`: `TariffId` es nullable. Si una tarifa se elimina, el ítem de reserva conserva el precio histórico (`PricePerNight`) que estaba guardado.

---

## 6. Stored Procedures

Los 4 Stored Procedures están en `scripts/sql/03_StoredProcedures.sql`.

---

## 7. Datos iniciales (Seed)

El script `04_SeedData.sql` inserta los datos mínimos para que el sistema funcione.

### Sedes recreativas (6)

| Código | Nombre                 | Ciudad               | Capacidad |
|--------|------------------------|----------------------|-----------|
| VLL    | Sede Villeta           | Villeta              | 80        |
| FUS    | Sede Fusagasugá        | Fusagasugá           | 60        |
| CHN    | Sede Chinchiná         | Chinchiná            | 50        |
| PLM    | Sede Palmira           | Palmira              | 70        |
| SFA    | Sede Santa Fe Ant.     | Santa Fe de Antioquia| 45        |
| BOG    | Sede Bogotá            | Bogotá               | 100       |

### Apartamentos (2)

| Código  | Nombre                  | Ciudad      | Unidades | Cap. total |
|---------|-------------------------|-------------|----------|------------|
| APT-MED | Apartamento Medellín    | Medellín    | 1        | 6          |
| APT-STM | Apartamentos Santa Marta| Santa Marta | 3        | 18         |

---

## 8. Migraciones con EF Core

### Primera migración (cuando se conecte a SQL Server)

```bash
# Desde la raíz del proyecto
export PATH="$PATH:/home/alfon/.dotnet"

dotnet ef migrations add InitialCreate \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj \
  --output-dir Migrations

dotnet ef database update \
  --project src/Infrastructure/FODUN.Reservations.Infrastructure.csproj \
  --startup-project src/Api/FODUN.Reservations.Api.csproj
```

---

## 9. Arquitectura de persistencia

```
┌─────────────────────────────────────────────────────────┐
│                    API (Razor Pages)                    │
│  UserService / ReservationService                       │
└──────────────────────────┬──────────────────────────────┘
                           │ usa interfaces
┌──────────────────────────▼──────────────────────────────┐
│              Application Layer (contratos)              │
│  IUserRepository / IReservationRepository               │
│  IAccommodationRepository / IUnitOfWork                 │
└──────────────────────────┬──────────────────────────────┘
                           │ implementa
┌──────────────────────────▼──────────────────────────────┐
│             Infrastructure Layer (implementaciones)     │
│                                                         │
│  UserRepository          ─── ReservationsDbContext      │
│  AccommodationRepository ─┘  (EF Core 8)               │
│  ReservationRepository   ─┘                             │
│  AuditLogRepository      ─┘                             │
│  UnitOfWork              ─── IDbContextTransaction      │
└──────────────────────────┬──────────────────────────────┘
                           │ EF Core → SQL
┌──────────────────────────▼──────────────────────────────┐
│              SQL Server (FODUN_Reservations)             │
│  8 tablas | 5 índices | 4 Stored Procedures             │
└─────────────────────────────────────────────────────────┘
```

---

## 10. Decisiones de diseño

### ¿Por qué UNIQUEIDENTIFIER como PK y no INT IDENTITY?

Los GUIDs evitan predecir el ID siguiente (seguridad), facilitan la distribución eventual entre bases de datos, y permiten generar el ID en el cliente antes de hacer el INSERT (útil para eventos de dominio).

### ¿Por qué guardar enums como string y no como INT?

Un enum guardado como `1`, `2`, `3` en la base de datos es ilegible sin el código fuente. Si se hace un SELECT directo o un reporte, `'Confirmed'` es inmediatamente comprensible. Además, reordenar o eliminar valores del enum no rompe datos históricos.

### ¿Por qué AuditLogs usa BIGINT IDENTITY y no UNIQUEIDENTIFIER?

AuditLogs es append-only y de altísima frecuencia. El orden de inserción es importante para auditoría. Un BIGINT autoincremental garantiza el orden cronológico y es mucho más eficiente en disco que un GUID para tablas con millones de filas.

---

*Documento generado en: 2026-05-17*  
*Próxima actualización: cuando se agreguen migraciones EF Core o nuevas tablas.*
