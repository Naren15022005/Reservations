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

### Accommodations — Sedes y Apartamentos

Representa tanto las sedes recreativas (6) como los apartamentos (2). El campo `Type` distingue entre ambos.

| Columna       | Tipo             | Nulo | Descripción                                               |
|---------------|------------------|------|-----------------------------------------------------------|
| `Id`          | UNIQUEIDENTIFIER | NO   | Clave primaria.                                           |
| `Code`        | NVARCHAR(50)     | NO   | Código corto único. Ej: `'VLL'`, `'APT-MED'`.             |
| `Name`        | NVARCHAR(255)    | NO   | Nombre completo de la sede o apartamento.                 |
| `Description` | NVARCHAR(MAX)    | SÍ   | Descripción libre para mostrar al usuario.                |
| `Type`        | NVARCHAR(50)     | NO   | `'RecreationalSite'` o `'Apartment'`.                     |
| `City`        | NVARCHAR(100)    | NO   | Ciudad donde está ubicada.                                |
| `Address`     | NVARCHAR(MAX)    | SÍ   | Dirección física completa.                                |
| `MaxCapacity` | INT              | NO   | Capacidad total de personas de toda la sede.              |
| `IsActive`    | BIT              | NO   | Borrado lógico.                                           |
| `CreatedAt`   | DATETIME2        | NO   | Fecha de creación en UTC.                                 |
| `UpdatedAt`   | DATETIME2        | NO   | Fecha de última modificación en UTC.                      |

**Datos cargados:** 6 sedes recreativas + 2 apartamentos (4 unidades totales en apartamentos).

---

### Seats — Alojamientos / Habitaciones

Cada fila es una unidad reservable (cabaña, habitación, apartamento). Una sede tiene N alojamientos.

| Columna          | Tipo             | Nulo | Descripción                                                   |
|------------------|------------------|------|---------------------------------------------------------------|
| `Id`             | UNIQUEIDENTIFIER | NO   | Clave primaria.                                               |
| `AccommodationId`| UNIQUEIDENTIFIER | NO   | FK hacia `Accommodations`.                                    |
| `SeatNumber`     | NVARCHAR(50)     | NO   | Identificador del alojamiento dentro de la sede. Ej: `'Alojamiento 1'`. |
| `Type`           | NVARCHAR(100)    | NO   | Tipo: `'Standard'`, `'Premium'`, `'Apartment'`, etc.          |
| `Capacity`       | INT              | NO   | Número máximo de personas que caben en este alojamiento.      |
| `Description`    | NVARCHAR(MAX)    | SÍ   | Descripción del alojamiento.                                  |
| `Amenities`      | NVARCHAR(MAX)    | SÍ   | JSON con lista de amenidades. Ej: `["baño","tv","nevera"]`.   |
| `IsActive`       | BIT              | NO   | Borrado lógico.                                               |
| `CreatedAt`      | DATETIME2        | NO   | Fecha de creación en UTC.                                     |
| `UpdatedAt`      | DATETIME2        | NO   | Fecha de última modificación en UTC.                          |

**Restricción única:** `UQ_Seats_AccommodationId_SeatNumber` — no puede repetirse el número de alojamiento dentro de la misma sede.  
**Cascade:** Si se elimina una `Accommodation`, se eliminan en cascada sus `Seats`.

---

### Tariffs — Tarifas

Define el precio por noche de un alojamiento. Cada alojamiento puede tener múltiples tarifas según temporada y número de personas.

| Columna                | Tipo             | Nulo | Descripción                                                    |
|------------------------|------------------|------|----------------------------------------------------------------|
| `Id`                   | UNIQUEIDENTIFIER | NO   | Clave primaria.                                                |
| `SeatId`               | UNIQUEIDENTIFIER | NO   | FK hacia `Seats`.                                              |
| `Season`               | NVARCHAR(50)     | NO   | Temporada: `'Low'` (baja), `'High'` (alta), `'Special'`.      |
| `MinPersons`           | INT              | NO   | Mínimo de personas para que aplique esta tarifa. Default: 1.  |
| `MaxPersons`           | INT              | NO   | Máximo de personas para que aplique esta tarifa.              |
| `PricePerNight`        | DECIMAL(18,2)    | NO   | Precio base por noche en COP.                                 |
| `AdditionalPersonPrice`| DECIMAL(18,2)    | SÍ   | Precio por persona adicional sobre la capacidad. Default: $16.000. |
| `DaysOfWeek`           | NVARCHAR(50)     | SÍ   | Días de semana donde aplica (para tarifas especiales).        |
| `IsExceptional`        | BIT              | NO   | `1` si es la tarifa especial de lunes a jueves.               |
| `ValidFrom`            | DATE             | NO   | Desde qué fecha aplica esta tarifa.                           |
| `ValidUntil`           | DATE             | SÍ   | Hasta qué fecha aplica. `NULL` = sin fecha de vencimiento.    |
| `CreatedAt`            | DATETIME2        | NO   | Fecha de creación en UTC.                                     |
| `UpdatedAt`            | DATETIME2        | NO   | Fecha de última modificación en UTC.                          |

**Lógica de prioridad de tarifas:**
1. Tarifa especial (`IsExceptional = 1`) solo si el check-in es lunes, martes, miércoles o jueves.
2. Temporada alta (`Season = 'High'`) — incremento del 77% aproximado respecto a temporada baja.
3. Temporada baja (`Season = 'Low'`) — tarifa base.

**Ejemplo de tarifas para un apartamento:**
| Temporada | Precio/noche | % vs Low |
|-----------|-------------|----------|
| Low       | $70.000     | —        |
| High      | $124.000    | +77%     |
| Special   | $27.000     | -61%     |

**Cascade:** Si se elimina un `Seat`, se eliminan en cascada sus `Tariffs`.

---

### Blackouts — Bloqueos de Fechas

Periodos en los que un alojamiento no está disponible (mantenimiento, eventos privados, etc.).

| Columna     | Tipo             | Nulo | Descripción                                        |
|-------------|------------------|------|----------------------------------------------------|
| `Id`        | UNIQUEIDENTIFIER | NO   | Clave primaria.                                    |
| `SeatId`    | UNIQUEIDENTIFIER | NO   | FK hacia `Seats`.                                  |
| `StartDate` | DATE             | NO   | Fecha de inicio del bloqueo (inclusive).           |
| `EndDate`   | DATE             | NO   | Fecha de fin del bloqueo (exclusive, como check-out). |
| `Reason`    | NVARCHAR(255)    | SÍ   | Motivo del bloqueo (mantenimiento, evento, etc.).  |
| `CreatedAt` | DATETIME2        | NO   | Fecha de creación en UTC.                          |
| `UpdatedAt` | DATETIME2        | NO   | Fecha de última modificación en UTC.               |

**Regla de solapamiento:** Un blackout bloquea el alojamiento si `StartDate < CheckOut AND EndDate > CheckIn`. Es la misma lógica que se usa para detectar conflictos entre reservas.

**Cascade:** Si se elimina un `Seat`, se eliminan en cascada sus `Blackouts`.

---

### Reservations — Reservas

Cabecera de cada reserva. Una reserva pertenece a un usuario y puede contener múltiples alojamientos (a través de `ReservationItems`).

| Columna               | Tipo             | Nulo | Descripción                                              |
|-----------------------|------------------|------|----------------------------------------------------------|
| `Id`                  | UNIQUEIDENTIFIER | NO   | Clave primaria.                                          |
| `UserId`              | UNIQUEIDENTIFIER | NO   | FK hacia `Users`.                                        |
| `CheckInDate`         | DATE             | NO   | Fecha de llegada.                                        |
| `CheckOutDate`        | DATE             | NO   | Fecha de salida.                                         |
| `TotalPersons`        | INT              | NO   | Número total de personas de la reserva.                  |
| `NumberOfRoomsNeeded` | INT              | NO   | Cantidad de alojamientos incluidos en la reserva.        |
| `Status`              | NVARCHAR(50)     | NO   | Estado actual (ver ciclo de vida abajo). Default: `'Pending'`. |
| `TotalCost`           | DECIMAL(18,2)    | NO   | Costo total ya calculado en COP (incluye adicionales y lavandería). |
| `LaundryService`      | BIT              | NO   | `1` si incluyó servicio de lavandería.                   |
| `LaundryServiceCost`  | DECIMAL(18,2)    | NO   | Costo fijo de lavandería: $18.000. `0` si no aplica.     |
| `Notes`               | NVARCHAR(MAX)    | SÍ   | Notas libres del usuario al hacer la reserva.            |
| `CreatedAt`           | DATETIME2        | NO   | Fecha de creación en UTC.                                |
| `UpdatedAt`           | DATETIME2        | NO   | Fecha de última modificación en UTC.                     |
| `CancelledAt`         | DATETIME2        | SÍ   | Fecha y hora de cancelación. `NULL` si no fue cancelada. |
| `CancelledReason`     | NVARCHAR(MAX)    | SÍ   | Motivo de cancelación ingresado por el usuario.          |

**Ciclo de vida del campo `Status`:**
```
Pending → Confirmed → CheckedIn → CheckedOut
    └──────────────────────────────→ Cancelled
```
- `Pending`: Reserva recién creada, pendiente de proceso.
- `Confirmed`: Reserva validada y confirmada (estado al crear desde la web).
- `CheckedIn`: El huésped ya llegó.
- `CheckedOut`: El huésped ya salió. Estado final positivo.
- `Cancelled`: Reserva cancelada por el usuario. Estado final negativo.

**Cascade:** Si se elimina un `User`, se eliminan en cascada sus `Reservations`.

---

### ReservationItems — Detalle de Reserva

Cada fila representa un alojamiento incluido dentro de una reserva. Si una reserva ocupa 2 habitaciones, tendrá 2 `ReservationItems`.

| Columna         | Tipo             | Nulo | Descripción                                                     |
|-----------------|------------------|------|-----------------------------------------------------------------|
| `Id`            | UNIQUEIDENTIFIER | NO   | Clave primaria.                                                 |
| `ReservationId` | UNIQUEIDENTIFIER | NO   | FK hacia `Reservations`.                                        |
| `SeatId`        | UNIQUEIDENTIFIER | NO   | FK hacia `Seats`. El alojamiento reservado.                     |
| `TariffId`      | UNIQUEIDENTIFIER | SÍ   | FK hacia `Tariffs`. La tarifa que se aplicó al momento de reservar. |
| `PricePerNight` | DECIMAL(18,2)    | NO   | Precio por noche tomado de la tarifa en el momento de la reserva. |
| `Nights`        | INT              | NO   | Número de noches (CheckOut - CheckIn).                          |

> **Nota:** El `Subtotal` (`PricePerNight × Nights`) es una propiedad calculada en el código C#, no se almacena en la BD para evitar inconsistencias.

**Cascade:** Si se elimina una `Reservation`, se eliminan en cascada sus `ReservationItems`.

---

### AuditLogs — Registro de Auditoría

Tabla de auditoría inmutable. Registra todas las acciones críticas del sistema (crear reserva, cancelar, etc.).

| Columna      | Tipo             | Nulo | Descripción                                                   |
|--------------|------------------|------|---------------------------------------------------------------|
| `Id`         | BIGINT           | NO   | Clave primaria con autoincremento (`IDENTITY(1,1)`).          |
| `UserId`     | UNIQUEIDENTIFIER | SÍ   | Usuario que realizó la acción. `NULL` para acciones del sistema. |
| `Action`     | NVARCHAR(255)    | NO   | Nombre de la acción. Ej: `'ReservationCreated'`, `'ReservationCancelled'`. |
| `EntityType` | NVARCHAR(100)    | SÍ   | Tipo de entidad afectada. Ej: `'Reservation'`, `'User'`.      |
| `EntityId`   | UNIQUEIDENTIFIER | SÍ   | ID de la entidad afectada.                                    |
| `OldValues`  | NVARCHAR(MAX)    | SÍ   | Estado anterior de la entidad en JSON.                        |
| `NewValues`  | NVARCHAR(MAX)    | SÍ   | Estado nuevo de la entidad en JSON.                           |
| `Timestamp`  | DATETIME2        | NO   | Momento exacto de la acción en UTC.                           |
| `IpAddress`  | NVARCHAR(45)     | SÍ   | IP del cliente (soporta IPv4 e IPv6).                         |

> Esta tabla **no tiene FK hacia Users** para que los logs persistan aunque se elimine el usuario.  
> Nunca se actualiza ni elimina un registro de auditoría — es append-only.

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

### SP1: `sp_GetAvailableRooms_ByDateRange`

**Propósito:** Devuelve todos los alojamientos disponibles de una sede para un rango de fechas y una capacidad mínima.

**Parámetros de entrada:**

| Parámetro        | Tipo             | Obligatorio | Descripción                                  |
|------------------|------------------|-------------|----------------------------------------------|
| `@AccommodationId` | UNIQUEIDENTIFIER | SÍ        | ID de la sede a consultar.                   |
| `@CheckInDate`   | DATE             | SÍ          | Fecha de llegada.                            |
| `@CheckOutDate`  | DATE             | SÍ          | Fecha de salida.                             |
| `@MinCapacity`   | INT              | NO          | Capacidad mínima requerida. Default: 1.      |

**Resultado:** Lista de alojamientos con columnas `SeatId`, `SeatNumber`, `Type`, `Capacity`, `Description`, `Amenities`, `Nights`.

**Lógica de disponibilidad:**
```sql
-- Un alojamiento está disponible si:
-- 1. Pertenece a la sede indicada y está activo
-- 2. Tiene capacidad >= MinCapacity
-- 3. NO tiene ningún Blackout que solape el rango
-- 4. NO tiene ninguna Reserva (Confirmed o CheckedIn) que solape el rango

-- Solapamiento: rango_existente.Inicio < CheckOut AND rango_existente.Fin > CheckIn
```

**Validaciones:** Rechaza con RAISERROR si CheckIn >= CheckOut o si alguna fecha es NULL.

---

### SP2: `sp_GetAvailableRooms_ByDateRangeAndPersons`

**Propósito:** Igual que SP1 pero orientado al número total de personas. Calcula cuántas habitaciones se necesitan y si una sola alcanza.

**Parámetros de entrada:**

| Parámetro        | Tipo             | Obligatorio | Descripción                                  |
|------------------|------------------|-------------|----------------------------------------------|
| `@AccommodationId` | UNIQUEIDENTIFIER | SÍ        | ID de la sede.                               |
| `@CheckInDate`   | DATE             | SÍ          | Fecha de llegada.                            |
| `@CheckOutDate`  | DATE             | SÍ          | Fecha de salida.                             |
| `@TotalPersons`  | INT              | SÍ          | Total de personas para la reserva.           |

**Columnas adicionales en el resultado:**
- `RoomsNeededForAllPersons`: `CEILING(TotalPersons / Capacity)` — cuántas habitaciones de este tipo hacen falta.
- `FitsInOneRoom`: `1` si la capacidad de una habitación es suficiente para todas las personas.

---

### SP3: `sp_GetApplicableTariffs`

**Propósito:** Devuelve las tarifas vigentes para un alojamiento, una fecha de check-in y un número de personas. Las ordena por prioridad.

**Parámetros de entrada:**

| Parámetro      | Tipo             | Obligatorio | Descripción                              |
|----------------|------------------|-------------|------------------------------------------|
| `@SeatId`      | UNIQUEIDENTIFIER | SÍ          | ID del alojamiento.                      |
| `@CheckInDate` | DATE             | SÍ          | Fecha de check-in (para calcular día de semana). |
| `@TotalPersons`| INT              | SÍ          | Número de personas (para filtrar por rango Min/Max). |

**Columna calculada `IsSpecialApplicableToday`:**
- Retorna `1` si la tarifa es especial (`IsExceptional = 1`) Y el check-in es lunes, martes, miércoles o jueves.
- SQL Server: `DATEPART(WEEKDAY, fecha)` con `SET DATEFIRST 7` retorna 2=lunes, 3=martes, 4=miércoles, 5=jueves.

**Orden de resultados:** Primero las tarifas especiales aplicables, luego por temporada descendente (High > Low).

---

### SP4: `sp_CalculateReservationCost`

**Propósito:** Calcula el costo total de una reserva para un alojamiento, fechas, personas y opciones de servicio.

**Parámetros de entrada:**

| Parámetro       | Tipo             | Obligatorio | Descripción                                    |
|-----------------|------------------|-------------|------------------------------------------------|
| `@SeatId`       | UNIQUEIDENTIFIER | SÍ          | ID del alojamiento.                            |
| `@CheckInDate`  | DATE             | SÍ          | Fecha de llegada.                              |
| `@CheckOutDate` | DATE             | SÍ          | Fecha de salida.                               |
| `@TotalPersons` | INT              | SÍ          | Total de personas.                             |
| `@IncludeLaundry` | BIT            | NO          | `1` para incluir lavandería ($18.000). Default: 0. |

**Parámetros OUTPUT:**

| Parámetro               | Tipo          | Descripción                              |
|-------------------------|---------------|------------------------------------------|
| `@OutTotalCost`         | DECIMAL(18,2) | Costo total calculado.                   |
| `@OutNights`            | INT           | Número de noches.                        |
| `@OutAdditionalPersons` | INT           | Personas adicionales sobre la capacidad. |

**Fórmula de cálculo:**
```
Noches = DATEDIFF(DAY, CheckIn, CheckOut)
PersonasAdicionales = MAX(0, TotalPersons - Capacity)

Costo = (PrecioBase × Noches)
      + (PrecioPersonaAdicional × PersonasAdicionales × Noches)
      + (Lavandería si aplica: $18.000)
```

**También retorna un SELECT** con el desglose completo para facilitar el consumo desde EF Core o la API.

**Ejemplo de uso desde T-SQL:**
```sql
DECLARE @Total DECIMAL(18,2), @Nights INT, @AddPersons INT;

EXEC [dbo].[sp_CalculateReservationCost]
    @SeatId          = '...',
    @CheckInDate     = '2026-07-10',
    @CheckOutDate    = '2026-07-13',
    @TotalPersons    = 5,
    @IncludeLaundry  = 1,
    @OutTotalCost    = @Total OUTPUT,
    @OutNights       = @Nights OUTPUT,
    @OutAdditionalPersons = @AddPersons OUTPUT;

SELECT @Total, @Nights, @AddPersons;
-- Resultado esperado (si tarifa base = $70.000, capacidad = 4):
-- Total = ($70.000 × 3) + ($16.000 × 1 × 3) + $18.000 = $276.000
-- Noches = 3, PersonasAdicionales = 1
```

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

### Alojamientos (Seats) cargados

- **Villeta:** 5 alojamientos (3 Standard cap.4, 2 Premium cap.6)
- **Fusagasugá:** 3 alojamientos (2 Standard cap.4, 1 Premium cap.6)
- **Medellín:** 1 apartamento cap.6
- **Santa Marta:** 3 apartamentos cap.6 c/u

### Tarifas cargadas (ejemplo — Villeta Alojamiento 1 y Medellín)

| Temporada | Precio/noche | IsExceptional | Aplica                        |
|-----------|-------------|---------------|-------------------------------|
| Low       | $70.000     | 0             | Cualquier día (baja temporada) |
| High      | $124.000    | 0             | Cualquier día (alta temporada) |
| Special   | $27.000     | 1             | Solo lunes a jueves            |

> Los scripts de las demás sedes se agregan al seed siguiendo el mismo patrón, o se cargan desde el panel de administración cuando esté disponible.

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

Esto genera los archivos en `src/Infrastructure/Migrations/` y aplica el esquema a la base de datos configurada en `appsettings.json`.

### Cómo se mapea cada tabla en EF Core

Cada tabla tiene un archivo de configuración en `src/Infrastructure/Persistence/Configurations/`:

| Archivo de configuración           | Tabla que configura   | Aspectos clave                                          |
|------------------------------------|-----------------------|---------------------------------------------------------|
| `UserConfiguration.cs`             | Users                 | Índices únicos, defaults, sin mapear campos de seguridad en logs |
| `AccommodationConfiguration.cs`    | Accommodations        | Enum `AccommodationType` → string, cascade delete       |
| `SeatConfiguration.cs`             | Seats                 | Índice compuesto (AccommodationId, SeatNumber), cascades|
| `TariffConfiguration.cs`           | Tariffs               | Enum `SeasonType` → string, índice SeatId+Season        |
| `BlackoutConfiguration.cs`         | Blackouts             | Índice compuesto (SeatId, StartDate, EndDate)            |
| `ReservationConfiguration.cs`      | Reservations          | Enum `ReservationStatus` → string, índice UserId+Status  |
| `ReservationItemConfiguration.cs`  | ReservationItems      | `Subtotal` ignorado (calculado), índice en SeatId       |
| `AuditLogConfiguration.cs`         | AuditLogs             | IDENTITY autoincrement en `Id` (BIGINT)                 |

### Convenciones usadas

- **Enums guardados como string:** `AccommodationType`, `SeasonType` y `ReservationStatus` se guardan como texto en la BD (`'RecreationalSite'`, `'Low'`, `'Confirmed'`) en vez de enteros. Facilita la lectura directa del SQL y evita confusión si se reordena el enum.
- **UNIQUEIDENTIFIER como PK:** Todos los IDs usan GUID para evitar colisiones en migraciones, para facilitar la distribución futura y por seguridad (no exponer IDs secuenciales).
- **DATETIME2 en vez de DATETIME:** Mayor precisión y rango. Siempre en UTC (`GETUTCDATE()`).
- **Borrado lógico:** Ninguna tabla usa DELETE real para entidades de negocio. Se usa `IsActive = 0`.
- **Propiedades computadas en C#:** `Subtotal` (en `ReservationItem`) se calcula en el dominio, no se persiste.

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

### Flujo de una operación de escritura (crear reserva)

```
1. ReservationService.CreateAsync(command)
2.   → AccommodationRepository.GetAvailableSeatsAsync(...)     [EF Core Query]
3.   → TariffService.CalculateCostAsync(...)                   [Lógica en C#]
4.   → UnitOfWork.BeginTransactionAsync()                      [BEGIN TRAN]
5.   → Reservation.Create(...) + reservation.Confirm()         [Dominio]
6.   → ReservationRepository.AddAsync(reservation)             [EF Core Add]
7.   → AuditLogRepository.AddAsync(log)                        [EF Core Add]
8.   → UnitOfWork.SaveChangesAsync()                           [INSERT x2]
9.   → UnitOfWork.CommitTransactionAsync()                     [COMMIT]
```

### Flujo de una operación de lectura (disponibilidad)

```
1. AvailabilityService.GetAvailableRoomsAsync(accommodationId, checkIn, checkOut, persons)
2.   → AccommodationRepository.GetAvailableSeatsAsync(...)
3.      → Consulta EF Core que excluye:
3a.        Seats con Blackouts que solapen las fechas
3b.        Seats con Reservas (Confirmed/CheckedIn) que solapen las fechas
4.   → Para cada Seat disponible: seleccionar tarifa aplicable
5.   → Retornar List<AvailableRoomDto>
```

---

## 10. Decisiones de diseño

### ¿Por qué UNIQUEIDENTIFIER como PK y no INT IDENTITY?

Los GUIDs evitan predecir el ID siguiente (seguridad), facilitan la distribución eventual entre bases de datos, y permiten generar el ID en el cliente antes de hacer el INSERT (útil para eventos de dominio).

El costo de rendimiento en índices clustered es aceptable para el volumen esperado. Si se necesita optimizar, se puede usar `NEWSEQUENTIALID()` en SQL Server para GUIDs secuenciales.

### ¿Por qué guardar enums como string y no como INT?

Un enum guardado como `1`, `2`, `3` en la base de datos es ilegible sin el código fuente. Si se hace un SELECT directo o un reporte, `'Confirmed'` es inmediatamente comprensible. Además, reordenar o eliminar valores del enum no rompe datos históricos.

### ¿Por qué Subtotal no se persiste?

`Subtotal = PricePerNight × Nights` es información derivada. Guardarlo implicaría mantener la consistencia manualmente (si cambia Nights, hay que actualizar Subtotal). En su lugar, se calcula en el dominio C# y EF Core lo ignora. La fórmula es simple y el costo es nulo.

### ¿Por qué los tokens de seguridad no tienen FK hacia Users?

Los tokens de reset de contraseña y confirmación de email se guardan directamente en `Users` para simplificar la búsqueda (`WHERE PasswordResetToken = @token`). Si estuvieran en una tabla separada necesitaría un JOIN adicional en cada validación de token.

### ¿Por qué AuditLogs usa BIGINT IDENTITY y no UNIQUEIDENTIFIER?

AuditLogs es append-only y de altísima frecuencia. El orden de inserción es importante para auditoría. Un BIGINT autoincremental garantiza el orden cronológico y es mucho más eficiente en disco que un GUID para tablas con millones de filas.

### ¿Por qué los Stored Procedures en vez de solo EF Core?

El proyecto requiere 4 SPs obligatorios. Se usan para las operaciones más críticas (disponibilidad y cálculo de costo) porque permiten optimización a nivel de SQL, son auditables directamente en SSMS, y pueden ser revisados por el equipo de DBA sin necesidad de entender el ORM.

---

*Documento generado en: 2026-05-17*  
*Próxima actualización: cuando se agreguen migraciones EF Core o nuevas tablas.*
