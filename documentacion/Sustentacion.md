# Guía de Sustentación — Sistema de Reservas

> Documento de preparación para la revisión técnica.
> Cada sección tiene: la pregunta probable → la respuesta para decir en voz alta → el respaldo técnico detrás.

---

## Presentación del proyecto (di esto al inicio si te piden que lo expliques)

> "Desarrollé un sistema web de reservas de alojamientos. Los usuarios pueden explorar sedes y apartamentos disponibles, ver disponibilidad por fechas, calcular el costo de su estadía, hacer la reserva y subir el comprobante de pago. El sistema les envía un correo de confirmación con un ticket PDF adjunto.
>
> Está construido con .NET 8, ASP.NET Core Razor Pages, SQL Server y Docker. Apliqué Clean Architecture y principios de DDD para organizar el código."

---

## BLOQUE 1 — Arquitectura

### ¿Qué arquitectura usaste?

**Respuesta:**
> "Usé Clean Architecture, que organiza el sistema en 4 capas: Domain, Application, Infrastructure y Api. La regla principal es que las capas internas no dependen de las externas. El Domain, que contiene toda la lógica de negocio, no sabe nada de la base de datos ni del framework web. Todo lo demás depende de él, nunca al revés."

**Respaldo técnico — las 4 capas:**

| Capa | Contiene | Depende de |
|------|----------|-----------|
| **Domain** | Entidades, reglas de negocio, interfaces, excepciones | Nada |
| **Application** | Casos de uso, Commands, Queries, DTOs, validadores | Domain |
| **Infrastructure** | Repositorios, DbContext, SMTP, hashing, SPs | Domain + Application |
| **Api** | Razor Pages, Program.cs, autenticación | Application + Infrastructure |

---

### ¿Por qué Razor Pages y no MVC?

**Respuesta:**
> "Razor Pages organiza el código por página — cada pantalla tiene su propio archivo .cshtml con su lógica asociada en un .cshtml.cs. Es más natural para una aplicación como esta donde cada pantalla tiene una responsabilidad clara. MVC centralizaría la lógica en controladores con muchas acciones distintas, lo que para este tamaño de proyecto sería más complejo sin beneficio."

---

### ¿Qué es DDD y cómo lo aplicaste?

**Respuesta:**
> "DDD, Domain-Driven Design, es un enfoque donde el código refleja el lenguaje del negocio. Lo apliqué principalmente con tres aggregate roots: Reservation, Accommodation y User. Un aggregate root es una entidad principal que protege su propio estado — nadie puede modificarla directamente, solo a través de sus métodos. Además usé Value Objects como DateRange para encapsular las fechas con sus validaciones propias."

---

## BLOQUE 2 — Base de Datos

### ¿Cómo está diseñada la base de datos?

**Respuesta:**
> "La base tiene 9 tablas. Las principales son: Users, Accommodations, Seats, Tariffs, Reservations y ReservationItems. Una sede tiene varias habitaciones, cada habitación tiene tarifas según temporada, y una reserva puede incluir varias habitaciones a través de la tabla ReservationItems que actúa como tabla intermedia. También hay tablas para Blackouts, que son fechas bloqueadas por administración, Notifications y AuditLogs."

**Respaldo — relaciones clave:**
```
Accommodations  →  Seats  →  Tariffs
                          →  Blackouts
Users  →  Reservations  ←→  Seats  (vía ReservationItems)
Users  →  Notifications
```

**¿Por qué GUIDs como clave primaria?**
> "Los GUIDs evitan que alguien pueda predecir los IDs en las URLs, no exponen cuántos registros hay en el sistema, y funcionan bien en ambientes distribuidos. La única excepción es AuditLogs que usa BIGINT autoincremental, porque esa tabla solo recibe inserciones y el orden natural del índice coincide con el orden cronológico, lo que es más eficiente."

---

### ¿Cómo determinás si una habitación está disponible?

**Respuesta:**
> "La disponibilidad se consulta con stored procedures en SQL Server. El SP recibe la sede, las fechas y el número de personas. Para cada habitación verifica dos cosas: que no haya un Blackout administrativo que cubra esas fechas, y que no haya una reserva Confirmada o en CheckIn que se solape con el rango pedido.
>
> La fórmula de solapamiento es: el check-in nuevo tiene que ser anterior al check-out de la reserva existente, Y el check-out nuevo tiene que ser posterior al check-in de la reserva existente. Si eso se cumple, hay conflicto y la habitación no aparece."

**Respaldo — SQL del solapamiento:**
```sql
AND r.[CheckInDate]  < @CheckOutDate
AND r.[CheckOutDate] > @CheckInDate
```
Solo reservas en estado `Confirmed` o `CheckedIn` bloquean disponibilidad. Las `Pending` y `Cancelled` no cuentan.

---

## BLOQUE 3 — Stored Procedures

### ¿Cuántos stored procedures hay y para qué sirve cada uno?

**Respuesta:**
> "Hay 4 stored procedures. El primero devuelve habitaciones disponibles por rango de fechas con capacidad mínima. El segundo hace lo mismo pero calcula adicionalmente cuántas habitaciones del mismo tipo se necesitan para alojar a todas las personas. El tercero devuelve las tarifas aplicables para una habitación según la fecha y número de personas, con prioridad para tarifas especiales de lunes a jueves. El cuarto calcula el costo total de la reserva con el desglose completo."

**Respaldo — los 4 SPs:**

| SP | Nombre | Devuelve |
|----|--------|---------|
| SP1 | `sp_GetAvailableRooms_ByDateRange` | Habitaciones disponibles por capacidad mínima |
| SP2 | `sp_GetAvailableRooms_ByDateRangeAndPersons` | Disponibles + RoomsNeeded + FitsInOneRoom |
| SP3 | `sp_GetApplicableTariffs` | Tarifas vigentes ordenadas por prioridad |
| SP4 | `sp_CalculateReservationCost` | Costo total desglosado |

---

### ¿Por qué usaste stored procedures y no solo EF Core?

**Respuesta:**
> "EF Core lo uso para las operaciones normales: guardar una reserva, buscar un usuario, listar alojamientos. Pero para las consultas de disponibilidad y cálculo de costos, que tienen lógica condicional compleja en SQL, usé stored procedures con ADO.NET. Esto me da control total sobre el resultado, especialmente para columnas calculadas y parámetros de salida que EF Core no mapea bien de forma nativa. Y reutilizo la misma conexión del DbContext, así no genero conexiones extra."

---

### ¿Cómo se crean los stored procedures?

**Respuesta:**
> "Creé una clase llamada StoredProcedureInitializer que se ejecuta automáticamente al arrancar la aplicación. Consulta la tabla sys.procedures de SQL Server para ver cuáles faltan, y los crea con CREATE OR ALTER PROCEDURE desde C#. Así el desarrollador solo necesita hacer dotnet run y el sistema queda listo, sin ejecutar scripts SQL manualmente."

---

### ¿Cómo calculás el costo de una reserva?

**Respuesta:**
> "El SP4 recibe la habitación, las fechas, número de personas y si incluye lavandería. Calcula las noches con DATEDIFF, busca la tarifa que aplica según temporada y personas, y aplica la fórmula: precio base por noches, más 16.000 pesos por cada persona que supere la capacidad de la habitación por cada noche, más 18.000 pesos fijos si incluye lavandería."

**Fórmula:**
```
TotalCost = (PricePerNight × Nights) + (16.000 × PersonasAdicionales × Nights) + (18.000 si lavandería)
```

---

## BLOQUE 4 — Lógica de Negocio

### Explicame el flujo completo de una reserva

**Respuesta:**
> "El usuario navega el catálogo, elige un alojamiento y selecciona fechas y personas. El sistema consulta las habitaciones disponibles con el SP2. El usuario elige una habitación y el SP4 calcula el costo en tiempo real. Al confirmar, el sistema crea la reserva en estado Pendiente y la guarda en la base de datos. Luego el usuario sube el comprobante de pago, el sistema guarda el archivo, genera un ticket PDF con QuestPDF y envía un correo de confirmación con ese PDF adjunto."

**Estados de la reserva:**
```
Pendiente → Confirmado → En CheckIn → Finalizado
    ↓              ↓
Cancelado       Cancelado
```

---

### ¿Qué validaciones tiene la reserva?

**Respuesta:**
> "Las validaciones están en dos niveles. Primero FluentValidation valida el formulario antes de que llegue al servicio: que los campos estén completos, que las fechas sean válidas, que el número de personas sea mayor a cero. Segundo, el aggregate Reservation valida las reglas de negocio: no se puede cancelar una reserva ya finalizada, no se puede hacer check-in sin estar confirmada, no se puede agregar ítems si ya no está en estado Pendiente. Si alguna de esas reglas se viola, se lanza una DomainException."

---

### ¿Qué es el patrón Result<T>?

**Respuesta:**
> "Es un objeto que puede representar éxito o fracaso. En lugar de lanzar excepciones cuando algo falla de forma esperada, los servicios retornan Result.Success con el valor, o Result.Failure con el mensaje de error. La página web luego verifica si IsSuccess es true o false y actúa en consecuencia. Esto hace el flujo más predecible y evita el uso de excepciones para control de flujo."

---

## BLOQUE 5 — Seguridad y Autenticación

### ¿Cómo manejaste la autenticación?

**Respuesta:**
> "Hay dos formas de autenticarse: con usuario y contraseña, y con Google OAuth. Para contraseñas uso PBKDF2 con SHA-256 y 100.000 iteraciones, más un salt aleatorio por usuario. Esto hace que el brute force sea extremadamente lento. Al hacer login exitoso, ASP.NET Core emite una cookie de sesión cifrada que expira en 30 minutos con renovación automática por actividad."

---

### ¿Qué protecciones de seguridad implementaste?

**Respuesta:**
> "Varias capas. Rate limiting por IP: máximo 10 intentos por minuto en las rutas de login, para evitar ataques de fuerza bruta automatizados. Bloqueo de cuenta tras 5 intentos fallidos por 15 minutos. Headers de seguridad en todas las respuestas: X-Frame-Options, Content-Security-Policy, X-Content-Type-Options. Las cookies son HttpOnly, lo que significa que JavaScript no puede leerlas. Y las contraseñas se comparan con tiempo constante para evitar ataques de timing."

---

### ¿Cómo funciona el Google OAuth?

**Respuesta:**
> "El usuario hace clic en 'Continuar con Google', la app redirige a Google con las credenciales de la aplicación. Google autentica y redirige de vuelta con un código. La app intercambia ese código por la información del usuario de Google, extrae el email, y busca o crea la cuenta en nuestra base de datos. Luego emite la cookie de sesión propia del sistema. El token de Google no se guarda, solo nos importa el email para identificar al usuario."

---

## BLOQUE 6 — Tecnologías

### ¿Qué tecnologías usaste?

**Respuesta:**
> "El backend es .NET 8 con ASP.NET Core Razor Pages. La base de datos es SQL Server 2022 corriendo en Docker. Para el ORM uso Entity Framework Core 8 con configuración Fluent API. Para validaciones uso FluentValidation. Los PDF los genero con QuestPDF. Los correos los envío con MailKit a través de SMTP. El logging lo manejo con Serilog con archivos que rotan diariamente. Y para la autenticación externa uso Google OAuth 2.0."

---

## BLOQUE 7 — Preguntas difíciles

### ¿Qué fue lo más difícil del proyecto?

**Respuesta:**
> "Lo más desafiante fue el manejo correcto de tipos de datos entre SQL Server y .NET al ejecutar los stored procedures con ADO.NET. Por ejemplo, la función CEILING en SQL retorna tipo FLOAT aunque el resultado sea un número entero. ADO.NET lo mapea como Double de .NET, no como Int32, lo que causaba una excepción de cast. Tuve que usar Convert.ToInt32 en lugar de GetInt32 para tolerarlo. También los campos CASE WHEN que retornan 0 o 1 en SQL no se pueden leer como Boolean directamente sino como Integer."

---

### ¿Qué mejorarías?

**Respuesta:**
> "Agregaría tests unitarios, especialmente para el aggregate Reservation y su máquina de estados, que son perfectos para testear. También consideraría agregar índices compuestos en la tabla ReservationItems sobre SeatId y las fechas, para que la consulta de disponibilidad escale mejor con muchos registros. Y para producción, configuraría el StoredProcedureInitializer para que también corra en producción de forma controlada, en lugar de usar scripts manuales."

---

### ¿Por qué usaste Docker?

**Respuesta:**
> "Docker me permite que cualquier desarrollador levante SQL Server en segundos sin instalarlo localmente. La configuración está en el docker-compose.yml del proyecto. Es una práctica estándar hoy en día para tener ambientes de desarrollo reproducibles y consistentes."

---

### ¿Qué es el Unit of Work?

**Respuesta:**
> "El Unit of Work agrupa múltiples operaciones de base de datos en una sola transacción. Por ejemplo, al crear una reserva necesito guardar el objeto Reservation, sus ReservationItems, y una Notification, todo de forma atómica. Si algo falla a mitad del proceso, nada se persiste. En este proyecto lo implementé como una capa sobre el SaveChangesAsync de Entity Framework Core."

---

## Reglas de negocio — para tenerlas claras

| Regla | Valor |
|-------|-------|
| Costo lavandería | $18.000 por estadía |
| Costo persona adicional | $16.000 por persona por noche |
| Intentos de login antes de bloqueo | 5 intentos |
| Duración del bloqueo | 15 minutos |
| Expiración del token de recuperación de contraseña | 30 minutos |
| Duración de la sesión | 30 minutos con renovación automática |
| Tarifa especial | Lunes a jueves (días 2 al 5 en DATEPART de SQL Server) |
| Estados que bloquean disponibilidad | Solo Confirmed y CheckedIn |

---

## Frases de respaldo si no sabés algo

- *"Eso lo manejé a través de [capa/componente], que es el responsable de esa parte del sistema."*
- *"En ese punto el sistema delega en [nombre del servicio/repositorio]."*
- *"Esa decisión la tomé pensando en separar responsabilidades — que cada parte del código haga una sola cosa."*
- *"Eso está configurado en Infrastructure, que es la capa que se encarga de todo lo relacionado con tecnología externa."*

---

## Resumen en una línea por tema (para repasar rápido)

- **Arquitectura**: Clean Architecture, 4 capas, dependencias apuntan hacia adentro
- **UI**: Razor Pages, no MVC
- **Base de datos**: SQL Server, 9 tablas, GUIDs, EF Core con Fluent API
- **Disponibilidad**: 4 stored procedures, ADO.NET, fórmula checkIn < otroCheckOut && checkOut > otroCheckIn
- **Reserva**: aggregate root, máquina de estados, constructor privado + Create()
- **Costo**: SP4, precio × noches + adicionales + lavandería opcional
- **Autenticación**: cookies + Google OAuth, PBKDF2 100k iteraciones + salt
- **Seguridad**: rate limiting, bloqueo de cuenta, headers HTTP, cookies HttpOnly
- **Correos**: MailKit + SMTP Gmail, tickets PDF con QuestPDF
- **Logging**: Serilog, archivos diarios
- **Validación**: FluentValidation (formulario) + DomainException (negocio)
- **Result<T>**: éxito/fracaso sin excepciones para flujo predecible
