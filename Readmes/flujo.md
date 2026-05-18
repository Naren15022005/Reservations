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

### Estructura objetivo
```
FODUN.Reservations/
├── src/
│   ├── Domain/        → Entidades, ValueObjects, Interfaces
│   ├── Application/   → Commands, Queries, Services, Validators, DTOs
│   ├── Infrastructure/→ DbContext, Repositories, Email, Migrations
│   └── Api/           → Razor Pages, Controllers, ViewModels, Program.cs
└── tests/
    ├── Unit/
    └── Integration/
```

### Pendiente (Día 1 — Lunes)
- [ ] Crear solución `.sln` con los 6 proyectos
- [ ] Configurar referencias entre proyectos
- [ ] Instalar NuGet packages
- [ ] Crear base de datos en SQL Server
- [ ] Configurar `appsettings.json`
- [ ] Crear Domain Models (Entidades, ValueObjects, Agregados)
- [ ] Crear `ReservationsDbContext`
- [ ] Crear migraciones iniciales
- [ ] Crear los 4 Stored Procedures
- [ ] Crear índices en BD

---

<!-- Próximas entradas se añaden aquí siguiendo el mismo formato -->
