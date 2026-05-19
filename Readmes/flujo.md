# FODUN - Flujo de Avance del Proyecto

Registro cronológico de cada avance del sistema de reservas FODUN.  
Cada entrada indica qué se hizo, qué archivos se tocaron y el estado al cierre.

---

## [2026-05-17] — Inicio del proyecto

**Estado:** Planificación completada, proyecto vacío.

### Qué se hizo
- Definición del plan técnico completo (ver [README.md](README.md))
- Stack decidido: .NET 8, SQL Server, EF Core 8, Razor Pages, FluentValidation, Serilog, xUnit
- Arquitectura definida: DDD + capas (Domain / Application / Infrastructure / Api)
- Schema de base de datos diseñado (8 tablas)
- 4 Stored Procedures diseñados
- Timeline de 7 días establecido

---

## [2026-05-17 — Sesión 2] — Día 1 completo: Estructura, Código y Scripts SQL

**Estado:** Solución .NET creada, compilando con 0 errores, 0 advertencias. 9 tests pasando.

### Entorno y herramientas
- **.NET 8 SDK** instalado en `/home/alfon/.dotnet` via `dotnet-install.sh` (sin sudo)
- Versión instalada: `8.0.421`
- No había .NET previo en el sistema (Linux Mint 22.3)

---

### Lo que se construyó

#### Solución y proyectos (6 proyectos)

```
FODUN.Reservations.sln
├── src/
│   ├── Domain/         → FODUN.Reservations.Domain        (classlib, net8.0)
│   ├── Application/    → FODUN.Reservations.Application   (classlib, net8.0)
│   ├── Infrastructure/ → FODUN.Reservations.Infrastructure (classlib, net8.0)
│   └── Api/            → FODUN.Reservations.Api           (web, net8.0)
├── tests/
│   ├── Unit/           → FODUN.Reservations.Tests.Unit    (xunit, net8.0)
│   └── Integration/    → FODUN.Reservations.Tests.Integration (xunit, net8.0)
└── scripts/sql/        → 4 scripts SQL
```

**Referencias entre proyectos:**
```
Api → Application → Domain
Api → Infrastructure → Domain
Infrastructure → Application
Tests.Unit → Domain + Application
Tests.Integration → Infrastructure
```

#### Paquetes NuGet instalados

| Proyecto       | Paquete                                        | Versión   |
|----------------|------------------------------------------------|-----------|
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer        | 8.0.0     |
| Infrastructure | Microsoft.EntityFrameworkCore.Tools            | 8.0.0     |
| Infrastructure | Microsoft.EntityFrameworkCore.Design           | 8.0.0     |
| Application    | FluentValidation                               | 11.9.0    |
| Api            | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0  |
| Api            | Serilog.AspNetCore                             | 8.0.1     |
| Api            | Serilog.Sinks.Console                          | 5.0.1     |
| Api            | Serilog.Sinks.File                             | 5.0.0     |
| Api            | Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation | 8.0.0  |
| Tests.Unit     | Microsoft.NET.Test.Sdk                         | 17.11.1   |
| Tests.Unit     | xunit                                          | 2.6.6     |

---

### Capa Domain (`src/Domain/`)

**Patrones usados:** Aggregates, Value Objects, Domain Exceptions, Interfaces de repositorios.

#### Enums
- `ReservationStatus` → Pending, Confirmed, CheckedIn, CheckedOut, Cancelled
- `AccommodationType` → RecreationalSite, Apartment
- `SeasonType` → Low, High, Special

#### Value Objects (inmutables, con igualdad por valor)
- `Money` — Monto en COP. Valida negativos, soporta suma y multiplicación.
- `DateRange` — Check-in / Check-out. Valida orden de fechas y fechas pasadas. Calcula noches y solapamiento.
- `PersonCount` — Número de personas (1–100). Calcula personas adicionales sobre capacidad.

#### Aggregates (entidades raíz con lógica de negocio)
- `User` — Registro, login, lockout (5 intentos → 15 min bloqueado), reset de contraseña con token temporal.
- `Accommodation` — Sede o apartamento. Contiene colección de `Seat`.
- `Seat` — Alojamiento/habitación dentro de una sede. Contiene `Tariff[]` y `Blackout[]`.
- `Tariff` — Tarifa por temporada (Low/High/Special). Lógica de tarifa especial lunes-jueves.
- `Blackout` — Bloqueo de fechas para un alojamiento.
- `Reservation` — Reserva completa con `ReservationItem[]`. Ciclo de vida: Pending → Confirmed → CheckedIn → CheckedOut / Cancelled.
- `ReservationItem` — Línea de detalle (alojamiento + precio + noches).
- `AuditLog` — Registro de auditoría (acción, entidad, usuario, IP).

#### Excepciones de dominio
- `DomainException` (base)
- `ReservationNotFoundException`
- `AccommodationNotAvailableException`

#### Interfaces de repositorios
- `IUserRepository` — CRUD + búsqueda por email, documento, token de reset
- `IAccommodationRepository` — listado activo, habitaciones disponibles por fechas
- `IReservationRepository` — por usuario, verificación de conflictos de fechas
- `IAuditLogRepository`
- `IUnitOfWork` — transacciones con Begin/Commit/Rollback

---

### Capa Application (`src/Application/`)

**Patrones usados:** CQRS (Commands/Queries), Service Interfaces, DTOs, FluentValidation, Result pattern.

#### Result pattern
```csharp
Result<T>.Success(value)   // operación exitosa con datos
Result<T>.Failure("msg")   // operación fallida con mensaje de error
Result.Success()            // sin datos
Result.Failure("msg")
```

#### DTOs (objetos de transferencia de datos)
- `UserDto` — datos públicos del usuario (sin contraseña)
- `AccommodationDto` + `SeatDto`
- `AvailableRoomDto` — habitación disponible con precio y temporada
- `TariffDto`
- `ReservationDto` + `ReservationItemDto`
- `ReservationCostDto` — desglose del costo (base, adicionales, lavandería, total)

#### Commands (escritura)
- `CreateUserCommand` — registro de usuario
- `CreateReservationCommand` — crear reserva
- `CancelReservationCommand` — cancelar reserva

#### Queries (lectura)
- `GetAvailableRoomsQuery`
- `GetUserReservationsQuery`
- `CalculateReservationCostQuery`

#### Validators (FluentValidation)
- `CreateUserValidator` — documento solo números, email válido, contraseña mínimo 8 chars con mayúscula y número
- `CreateReservationValidator` — fechas válidas, personas > 0
- `CancelReservationValidator` — motivo de cancelación requerido

#### Interfaces de servicios
- `IUserService` — Register, Login, RequestPasswordReset, ResetPassword, GetById
- `IReservationService` — Create, Cancel, GetById, GetByUser
- `ITariffService` — CalculateCost
- `IAvailabilityService` — GetAvailableRooms
- `IEmailService` — SendPasswordReset, SendEmailConfirmation, SendReservationConfirmation
- `IPasswordHasher` — Hash(password) → (hash, salt), Verify(password, hash, salt)

---

### Capa Infrastructure (`src/Infrastructure/`)

**Patrones usados:** Repository pattern, Unit of Work, EF Core Configurations.

#### Persistencia
- `ReservationsDbContext` — DbContext con todos los DbSets y configuraciones aplicadas
- `UnitOfWork` — implementación con transacciones reales via `IDbContextTransaction`

#### Configuraciones EF Core (IEntityTypeConfiguration<T>)
Cada tabla tiene su archivo de configuración:
- `UserConfiguration` — índices únicos en Email y DocumentNumber, valores por defecto
- `AccommodationConfiguration` — conversión de enum a string, cascade delete en Seats
- `SeatConfiguration` — índice compuesto (AccommodationId, SeatNumber), cascades
- `TariffConfiguration` — conversión de SeasonType a string, índice por SeatId+Season
- `BlackoutConfiguration` — índice compuesto (SeatId, StartDate, EndDate)
- `ReservationConfiguration` — conversión de ReservationStatus, índice UserId+Status
- `ReservationItemConfiguration` — índice en SeatId, Subtotal como propiedad ignorada (calculada)
- `AuditLogConfiguration` — IDENTITY en columna Id (BIGINT)

#### Repositorios
- `UserRepository` — incluye búsqueda por PasswordResetToken
- `AccommodationRepository` — consulta de disponibilidad: excluye reservas Confirmed/CheckedIn y Blackouts que solapen las fechas
- `ReservationRepository` — verificación de conflictos de fechas por alojamiento
- `AuditLogRepository`

#### Servicios
- `TariffService` — calcula costo: prioriza tarifa especial (lun-jue), luego High, luego Low. Suma persona adicional ($16.000 default) y lavandería ($18.000)
- `AvailabilityService` — obtiene habitaciones disponibles con tarifa aplicable
- `PasswordHasherService` — PBKDF2-SHA256, 100.000 iteraciones, salt de 32 bytes aleatorios
- `SmtpEmailService` — envío de correos HTML (reset password, confirmación email, confirmación reserva)

#### Extensión DI
- `DependencyInjection.AddInfrastructure(config)` — registra todos los servicios y repositorios en el contenedor

---

### Capa API (`src/Api/`)

**Patrones usados:** Razor Pages, Cookie Authentication, ViewModels, Antiforgery (CSRF).

#### Configuración (`Program.cs`)
- Serilog: log a consola + archivo rotativo diario (`logs/fodun-*.log`)
- EF Core + SQL Server con retry on failure (3 intentos)
- Autenticación por cookies (HttpOnly, Secure, SameSite=Strict)
- Session timeout configurable (30 min por defecto)
- CSRF protection via `AddAntiforgery()`
- Rutas protegidas: `/Dashboard/**` y `/Reservations/**` requieren autenticación

#### Pages implementadas
| Página                    | URL                        | Descripción                          |
|---------------------------|----------------------------|--------------------------------------|
| `Index`                   | `/`                        | Página de inicio, lista de sedes     |
| `Auth/Login`              | `/Auth/Login`              | Formulario de inicio de sesión       |
| `Auth/Register`           | `/Auth/Register`           | Formulario de registro               |
| `Auth/Logout`             | `/Auth/Logout`             | Cierra sesión y redirige al inicio   |
| `Auth/ForgotPassword`     | `/Auth/ForgotPassword`     | Solicitar reset de contraseña        |
| `Dashboard/Index`         | `/Dashboard/Index`         | Mis reservas (requiere login)        |
| `Accommodations/Index`    | `/Accommodations/Index`    | Lista de sedes y apartamentos        |
| `Error`                   | `/Error`                   | Página de error con RequestId        |

#### ViewModels
- `LoginViewModel`, `RegisterViewModel`, `ForgotPasswordViewModel`, `ResetPasswordViewModel`

#### Servicios de aplicación (implementados en Api)
- `UserService` — implementa `IUserService`, orquesta registro/login con validators, email y password hashing
- `ReservationService` — implementa `IReservationService`, crea reservas con transacciones, auditoria y validaciones

---

### Scripts SQL (`scripts/sql/`)

| Archivo                     | Contenido                                          |
|-----------------------------|-----------------------------------------------------|
| `01_CreateDatabase.sql`     | Crea `FODUN_Reservations` si no existe              |
| `02_CreateSchema.sql`       | 8 tablas + índices optimizados                      |
| `03_StoredProcedures.sql`   | 4 Stored Procedures obligatorios                    |
| `04_SeedData.sql`           | 8 sedes/apartamentos + alojamientos + tarifas       |

#### Stored Procedures

**SP1: `sp_GetAvailableRooms_ByDateRange`**
- Parámetros: AccommodationId, CheckIn, CheckOut, MinCapacity
- Excluye: alojamientos con Blackouts o Reservas (Confirmed/CheckedIn) que solapen las fechas
- Ordena por capacidad y número de alojamiento

**SP2: `sp_GetAvailableRooms_ByDateRangeAndPersons`**
- Igual que SP1 + calcula cuántas habitaciones se necesitan para cubrir el total de personas
- Retorna columna `FitsInOneRoom` (1 si una habitación alcanza, 0 si no)

**SP3: `sp_GetApplicableTariffs`**
- Parámetros: SeatId, CheckInDate, TotalPersons
- Filtra tarifas válidas para las personas y fechas dadas
- Retorna columna calculada `IsSpecialApplicableToday` (tarifa especial lun-jue)
- Prioriza tarifa especial, luego temporada alta

**SP4: `sp_CalculateReservationCost`**
- Parámetros: SeatId, CheckIn, CheckOut, TotalPersons, IncludeLaundry
- Parámetros OUTPUT: OutTotalCost, OutNights, OutAdditionalPersons
- Aplica lógica de prioridad de tarifas (especial > alta > baja)
- Calcula: (precio_base × noches) + (personas_adicionales × precio_adicional × noches) + lavandería
- También retorna SELECT con desglose (para uso desde EF Core / SqlQuery)

---

### Tests (`tests/Unit/`)

**9 tests unitarios, todos pasando:**

| Test                                          | Qué valida                                    |
|-----------------------------------------------|-----------------------------------------------|
| `Money_NegativeAmount_ThrowsArgumentException` | Money rechaza montos negativos               |
| `Money_Add_ReturnsCorrectSum`                  | Suma de Money funciona correctamente          |
| `Money_Multiply_ReturnsCorrectResult`           | Multiplicación de Money funciona              |
| `DateRange_SameDates_ThrowsArgumentException`   | DateRange rechaza check-in == check-out       |
| `DateRange_ValidRange_CalculatesNightsCorrectly`| DateRange calcula noches correctamente        |
| `DateRange_OverlapsWith_DetectsConflict`        | Detección de solapamiento entre rangos        |
| `User_Create_WithValidData_ReturnsUser`         | Creación de usuario válido                    |
| `User_RegisterFailedLogin_LocksAfterFiveAttempts`| Lockout tras 5 intentos fallidos             |
| `User_Create_EmptyEmail_ThrowsDomainException`  | Validación de email vacío en dominio          |

---

### Resultado de compilación
```
dotnet build FODUN.Reservations.sln
→ Compilación correcta. 0 Errores, 0 Advertencias.

dotnet test tests/Unit/
→ Correctas: 9 | Con error: 0 | Omitido: 0 | Total: 9
```

---

### Reglas de negocio implementadas
- Contraseña hasheada con PBKDF2-SHA256 (100.000 iteraciones) — jamás en texto plano
- Lockout automático tras 5 intentos fallidos (15 minutos)
- Token de reset de contraseña: único, expira en 30 minutos, single-use
- Tarifa especial lun-jue: prioridad máxima sobre temporadas alta/baja
- Persona adicional: $16.000 COP por persona por noche (configurable por tarifa)
- Lavandería: $18.000 COP fijo por reserva
- Verificación de disponibilidad: doble check (Blackouts + Reservas activas)
- Auditoría: toda acción crítica (crear/cancelar reserva) queda registrada

---

### Completado en Día 1
- [x] Estructura de solución .NET 8 (6 proyectos)
- [x] Capa Domain completa (Aggregates, Value Objects, Interfaces)
- [x] Capa Application completa (CQRS, DTOs, Validators, Services)
- [x] Capa Infrastructure completa (EF Core, Repos, TariffService, PasswordHasher, SMTP)
- [x] Capa Api: Program.cs, autenticación por cookies, páginas Auth/Dashboard/Accommodations básicas
- [x] Scripts SQL (01-04): Schema, SPs, Seed
- [x] 9 tests unitarios pasando

---

## [2026-05-17 — Sesión 3] — Día 2: Frontend completo + Páginas pendientes

**Estado:** Compilación correcta. 0 Errores, 0 Advertencias. 9 tests pasando. UI completamente implementada.

### Qué se hizo

#### wwwroot — Bootstrap y assets estáticos
- Bootstrap 5.3.3 descargado localmente (`wwwroot/lib/bootstrap/dist/`)
- `wwwroot/css/site.css` — estilos personalizados: hero section, tarjetas de sede, badges de estado, formularios, precio destacado, footer
- `wwwroot/js/site.js` — confirmación de cancelación, cálculo de noches en tiempo real

#### Páginas nuevas implementadas

| Página | URL | Descripción |
|--------|-----|-------------|
| `Auth/ResetPassword` | `/Auth/ResetPassword?token=...` | Formulario para establecer nueva contraseña |
| `Auth/ConfirmEmail` | `/Auth/ConfirmEmail?userId=...&token=...` | Confirmar correo electrónico (éxito/error visual) |
| `Accommodations/Availability` | `/Accommodations/Availability/{id}` | Buscar disponibilidad por fechas + personas + costos estimados |
| `Reservations/Create` | `/Reservations/Create` | Confirmar y crear reserva desde el alojamiento elegido |
| `Dashboard/Cancel` | `/Dashboard/Cancel/{id}` | Cancelar reserva con motivo obligatorio |

#### Mejoras a páginas existentes
- `Accommodations/Index` — rediseñada con tarjetas, badge de código, enlace corregido a `asp-route-accommodationId`
- `Dashboard/Index` — rediseñada con cards por reserva, badges de estado en español, botón cancelar
- `Index.cshtml` — hero section con gradiente, CTA dinámico según autenticación

#### Cambios al dominio/application
- `IUserService` + `UserService` — nuevo método `ConfirmEmailAsync(userId, token)` para confirmar email vía enlace
- `CreateReservationCommand` — campo `SeatId?` opcional: si el usuario elige alojamiento específico desde Availability, se respeta esa selección
- `ReservationService.CreateAsync` — prioriza el `SeatId` elegido por el usuario si se envía

#### Flujo completo de usuario
```
[Index] → [Accommodations/Index] → [Accommodations/Availability/{id}]
       → (elegir alojamiento) → [Reservations/Create] → [Dashboard/Index]
                                                      ← [Dashboard/Cancel]
```

### Completado en Sesión 3
- [x] Bootstrap 5.3.3 local en wwwroot + site.css/js
- [x] 5 páginas nuevas: ResetPassword, ConfirmEmail, Availability, Create, Cancel
- [x] Flujo completo Index → Accommodations → Availability → Reservations/Create → Dashboard

---

## [2026-05-18 — Sesión 4] — BD InMemory para desarrollo

**Estado:** App corriendo en http://localhost:5000. 0 errores. Flujo completo funcional sin SQL Server.

### Qué se hizo

#### InMemory Database
- Paquete `Microsoft.EntityFrameworkCore.InMemory 8.0.0` agregado a Infrastructure
- `Program.cs` — `AddDbContext` condicional: `UseInMemoryDatabase` en Development, `UseSqlServer` en Production
- `DependencyInjection.AddInfrastructure` — acepta `Action<DbContextOptionsBuilder>?` opcional para inyectar config de BD desde afuera
- `UnitOfWork` — transacciones no-op cuando el provider es InMemory (evita `InvalidOperationException`)
- `DbSeeder` — 4 sedes + 11 alojamientos + 33 tarifas (Low/High/Special) sembrados al arrancar en Development

#### Correcciones técnicas
- `AccommodationRepository.GetAllActiveAsync` — agrega `Include(a => a.Seats)` (antes devolvía colección vacía)
- `CookieSecurePolicy` — cambia a `SameAsRequest` en Development (antes `Always` bloqueaba cookies en HTTP)
- `UseHttpsRedirection` — desactivado en Development (evita redirección a puerto HTTPS no configurado)
- `launchSettings.json` — puerto fijado en `http://localhost:5000`

#### Datos de prueba (seeder)
| Código | Nombre | Tipo | Ciudad | Aloj. |
|--------|--------|------|--------|-------|
| SVL | Sede Villeta | RecreationalSite | Villeta | 3 |
| SFS | Sede Fusagasugá | RecreationalSite | Fusagasugá | 3 |
| APM | Apartamentos Medellín | Apartment | Medellín | 3 |
| APB | Apartamentos Santa Marta | Apartment | Santa Marta | 2 |

Cada alojamiento tiene 3 tarifas: Low (base), High (+40%), Special lun-jue (+20%)

### Cómo correr
```bash
export PATH="/home/alfon/.dotnet:$PATH"
dotnet run --project src/Api
# → http://localhost:5000
```

---

## [2026-05-18 — Sesión 5] — Réplica visual del sistema original FODUN

**Estado:** 0 errores, app corriendo en http://localhost:5000. UI idéntica al sistema original del PDF.

### Qué se hizo

#### Tema visual completo (`site.css` — reescritura total)
- Paleta extraída de las capturas PDF del cliente:
  - Header: gradiente crimson `#6B0000 → #8B1A1A → #B22222`
  - Tab activo: naranja `#D9671E`
  - Botones: rojo `#CC0000`
  - Navegación: gris `#D2D2D2`
  - Footer: `#2C2C2C`
- Variables CSS (`--fodun-*`) para coherencia en toda la UI
- Logo circular con X (CSS puro, sin imagen)
- Tabla FODUN con thead gris oscuro y filas alternadas
- Miniaturas de sede: degradados marrones (recreativa) y azules (apartamento)
- Badges de estado: Pendiente/Confirmada/En curso/Finalizada/Cancelada

#### Páginas rediseñadas
| Página | Cambios |
|--------|---------|
| `_Layout.cshtml` | Header rojo + logo + tabs de navegación grises/naranja |
| `Accommodations/Index` | Tabla con miniatura, nombre, descripción, tipo, ubicación, botón rojo "Seleccionar" |
| `Accommodations/Availability` | Dos paneles: izquierdo "Mis Fechas + Total Reserva", derecho tabla de habitaciones con ✓/✗, modal de detalles |
| `Auth/Login` | Layout dos columnas: foto/logo izquierda + formulario derecha con teclado virtual numérico shuffled + banner "Usuario Nuevo" |
| `Auth/Register` | Formulario de dos columnas exactamente como el original (todos los campos visibles) |
| `Dashboard/Index` | Tabla con Ampliar, Lugar, Fechas, Personas, Habitaciones, Valor, Estado + modal de detalles |

#### Características especiales
- **Teclado virtual** en Login: números 0-9 shuffled aleatoriamente en cada carga, teclado rojo con botón "Limpiar"
- **Tabs de página** dentro de Accommodations: "Sedes Recreativas" (inactivo) / "Seleccione sus Fechas" (activo naranja)
- **Modal "Detalle Habitación"** al clicar el icono lupa en la tabla de disponibilidad
- **Modal "Detalle Reserva"** al clicar el icono lupa en Mis Reservas
- **Panel Total Reserva** actualiza en tiempo real al seleccionar habitación vía checkbox

### Pendiente (Día 3 en adelante)
- [ ] Conectar a SQL Server real y crear las tablas (ejecutar scripts 01-04)
- [ ] Crear migraciones EF Core (`dotnet ef migrations add InitialCreate`)
- [ ] Tests unitarios adicionales: validators, TariffService, AvailabilityService
- [ ] Tests de integración con SQL Server (stored procedures)
- [ ] Configurar SMTP real (Gmail / SendGrid)
- [ ] Documento técnico PDF final
