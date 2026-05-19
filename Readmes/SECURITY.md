# Seguridad - FODUN Reservations

**Documento:** Medidas de seguridad implementadas en el sistema  
**Última actualización:** 2026-05-19  
**Estado:** En implementación

---

## Tabla de contenidos

1. [Overview de Seguridad](#1-overview-de-seguridad)
2. [Autenticación](#2-autenticación)
3. [Autorización](#3-autorización)
4. [Encriptación y Contraseñas](#4-encriptación-y-contraseñas)
5. [Rate Limiting](#5-rate-limiting)
6. [Security Headers](#6-security-headers)
7. [Validación de Entrada](#7-validación-de-entrada)
8. [Logging y Auditoría](#8-logging-y-auditoría)
9. [Gestión de Secretos](#9-gestión-de-secretos)
10. [Buenas Prácticas](#10-buenas-prácticas)

---

## 1. Overview de Seguridad

### Modelo de Seguridad

```
┌─────────────────────────────────────────────────┐
│         USUARIO (Navegador)                     │
└──────────────────┬──────────────────────────────┘
                   │
        ┌──────────▼──────────────┐
        │  HTTPS + TLS 1.2+       │
        │  Certificado válido     │
        └──────────────┬──────────┘
                       │
      ┌────────────────▼────────────────┐
      │  SECURITY HEADERS               │
      │  - CSP (Content Security Policy)│
      │  - X-Frame-Options: DENY        │
      │  - HSTS (HTTP Strict Transport) │
      │  - X-Content-Type-Options       │
      └────────────────┬────────────────┘
                       │
       ┌───────────────▼───────────────┐
       │  RATE LIMITING                │
       │  - IP-based throttling         │
       │  - Endpoint-specific limits    │
       │  - 429 Too Many Requests       │
       └───────────────┬───────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  COOKIE AUTHENTICATION       │
        │  - HttpOnly flag             │
        │  - Secure flag (HTTPS only)  │
        │  - SameSite=Lax              │
        │  - 30 min expiration         │
        │  - Sliding expiration        │
        └──────────────┬──────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  CSRF PROTECTION            │
        │  - Antiforgery tokens        │
        │  - Token validation          │
        └──────────────┬──────────────┘
                       │
   ┌───────────────────▼───────────────┐
   │  INPUT VALIDATION (2 capas)        │
   │  - FluentValidation (formato)      │
   │  - Domain Validation (negocio)     │
   └───────────────────┬───────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  AUTHORIZATION               │
        │  - [Authorize] attributes    │
        │  - Role-based access         │
        │  - Claim-based policies      │
        └──────────────┬──────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  DATABASE LAYER              │
        │  - Parameterized queries     │
        │  - No SQL injection          │
        │  - Encrypted fields (prod)   │
        └──────────────────────────────┘
```

---

## 2. Autenticación

### Métodos soportados

#### A. Autenticación con Cookies (Principal)

**Configuración:**

```csharp
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie("External") // Para OAuth
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"];
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
        options.SignInScheme = "External";
        options.SaveTokens = false; // No guardar tokens de acceso
    });
```

**Ventajas:**
- ✅ Protección CSRF automática
- ✅ HttpOnly previene XSS
- ✅ Secure flag (HTTPS only)
- ✅ SameSite previene CSRF cross-site
- ✅ Expiración automática
- ✅ Renovación automática (sliding)

#### B. OAuth2 Google

**Flujo:**

```
1. Usuario clickea "Conectar con Google"
2. Redirige a Google OAuth
3. Usuario autoriza permisos
4. Google redirige con authorization code
5. Backend intercambia code por ID token
6. Crea cuenta o loguea usuario existente
7. Setea cookie de sesión
```

**Seguridad:**
- ✅ No almacenamos tokens de acceso de Google
- ✅ Validación de CSRF state param
- ✅ HTTPS requerido
- ✅ Usuarios requieren confirmar email

---

## 3. Autorización

### Niveles de autorización

```csharp
// Nivel 1: Requerir autenticación
[Authorize]
public IActionResult Dashboard() => View();

// Nivel 2: Requerir rol específico
[Authorize(Roles = "Admin")]
public IActionResult AdminPanel() => View();

// Nivel 3: Policy-based (claims)
[Authorize(Policy = "CanManageReservations")]
public IActionResult ManageReservations() => View();

// Nivel 4: Validación en servicio
public async Task<Result<ReservationDto>> CancelAsync(
    Guid reservationId, 
    Guid requestingUserId) // Must match!
{
    var reservation = await _repo.GetByIdAsync(reservationId);
    if (reservation.UserId != requestingUserId)
        return Result.Failure("Unauthorized");
    
    // ... proceed
}
```

### Rutas protegidas

**Configuradas en Razor Pages:**

```csharp
// en Program.cs
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AuthorizeFolder("/Dashboard");
        options.Conventions.AuthorizeFolder("/Reservations");
        options.Conventions.AllowAnonymousToPage("/Auth/Login");
        options.Conventions.AllowAnonymousToPage("/Auth/Register");
    });
```

---

## 4. Encriptación y Contraseñas

### Hashing de Contraseñas

**Algoritmo:** PBKDF2-SHA256

```csharp
public class PasswordHasherService : IPasswordHasher
{
    private const int SaltSize = 32;         // 32 bytes
    private const int HashSize = 32;         // 32 bytes  
    private const int Iterations = 100_000;  // PBKDF2 rounds
    
    public (string hash, string salt) Hash(string password)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] saltBytes = new byte[SaltSize];
            rng.GetBytes(saltBytes);
            
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                return (
                    Convert.ToBase64String(hashBytes),
                    Convert.ToBase64String(saltBytes)
                );
            }
        }
    }
    
    public bool Verify(string password, string hash, string salt)
    {
        byte[] hashBytes = Convert.FromBase64String(hash);
        byte[] saltBytes = Convert.FromBase64String(salt);
        
        using (var pbkdf2 = new Rfc2898DeriveBytes(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] computedHash = pbkdf2.GetBytes(HashSize);
            
            // Protección contra timing attacks
            return CryptographicOperations.FixedTimeEquals(
                hashBytes, computedHash);
        }
    }
}
```

**Seguridad:**
- ✅ Salt aleatorio de 32 bytes por usuario
- ✅ 100,000 iteraciones PBKDF2
- ✅ Protección contra timing attacks (FixedTimeEquals)
- ✅ Nunca almacenar contraseñas en plaintext

### Validación de Contraseñas

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Contraseña requerida")
            .MinimumLength(8).WithMessage("Mínimo 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("Requiere mayúscula")
            .Matches(@"[0-9]").WithMessage("Requiere número")
            .Matches(@"[!@#$%^&*]").WithMessage("Requiere carácter especial");
    }
}
```

**Políticas:**
- ✅ Mínimo 8 caracteres
- ✅ Mínimo 1 mayúscula
- ✅ Mínimo 1 número
- ✅ Mínimo 1 carácter especial

---

## 5. Rate Limiting

### Configuración implementada

```json
{
  "IpRateLimitOptions": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "ClientIdHeader": "X-ClientId",
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  },
  "IpRateLimitPolicies": {
    "auth-strict": {
      "/Auth/Login": { "Period": "1m", "Limit": 5 },
      "/Auth/Register": { "Period": "1m", "Limit": 3 },
      "/Auth/ForgotPassword": { "Period": "1m", "Limit": 3 }
    },
    "api": {
      "/api/*": { "Period": "1m", "Limit": 100 }
    }
  }
}
```

### Límites por ruta

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `/Auth/Login` | 5 intentos | 1 minuto | Prevenir fuerza bruta |
| `/Auth/Register` | 3 intentos | 1 minuto | Prevenir spam de cuentas |
| `/Auth/ForgotPassword` | 3 intentos | 1 minuto | Prevenir abuso de reset |
| `/api/reservations` | 50 requests | 1 minuto | Proteger recursos |
| Resto de rutas | 100 requests | 1 minuto | Límite general |

### Respuesta 429

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 58
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1234567890

{
  "error": "Demasiadas solicitudes. Intenta más tarde.",
  "retryAfter": 58
}
```

---

## 6. Security Headers

### Headers implementados

```csharp
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self'; " +
        "connect-src 'self'");
    
    // Prevenir clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // Prevenir MIME type sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    
    // Protección XSS en navegadores antiguos
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Política de referrer
    context.Response.Headers.Add("Referrer-Policy", 
        "strict-origin-when-cross-origin");
    
    // HSTS (HTTP Strict Transport Security)
    context.Response.Headers.Add("Strict-Transport-Security", 
        "max-age=31536000; includeSubDomains; preload");
    
    // Deshabilitar caching de datos sensibles
    context.Response.Headers.Add("Cache-Control", 
        "no-store, no-cache, must-revalidate, proxy-revalidate");
    context.Response.Headers.Add("Pragma", "no-cache");
    context.Response.Headers.Add("Expires", "0");
    
    await next();
});
```

---

## 7. Validación de Entrada

### Estrategia de defensa en profundidad

**Capa 1: Validación de formato (FluentValidation)**

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .MaximumLength(255);

RuleFor(x => x.Notes)
    .MaximumLength(1000)
    .Matches(@"^[a-zA-Z0-9\s\.\,\-\!]*$") // Solo caracteres permitidos
    .WithMessage("Contiene caracteres inválidos");
```

**Capa 2: Validación de negocio (Dominio)**

```csharp
public static Reservation Create(
    Guid userId,
    Guid seatId,
    DateOnly checkIn,
    DateOnly checkOut,
    int totalPersons,
    string? notes)
{
    if (checkIn >= checkOut)
        throw new DomainException("CheckIn debe ser anterior a CheckOut");
    
    if (totalPersons < 1 || totalPersons > 100)
        throw new DomainException("Entre 1 y 100 personas");
    
    if (checkIn <= DateOnly.FromDateTime(DateTime.UtcNow))
        throw new DomainException("CheckIn no puede ser pasado");
    
    // ... crear reserva
}
```

**Capa 3: Sanitización (si es necesario)**

```csharp
// Sanitizar HTML (si se renderiza en HTML)
var sanitizer = new HtmlSanitizer();
var clean = sanitizer.Sanitize(userInput);

// URL encoding para parámetros
var encoded = Uri.EscapeDataString(userInput);

// SQL Injection: No aplicable (usamos EF Core + parameterized queries)
```

---

## 8. Logging y Auditoría

### Eventos de seguridad

```csharp
public interface ISecurityLogger
{
    Task LogFailedLoginAsync(string email, string ipAddress);
    Task LogSuccessfulLoginAsync(Guid userId, string ipAddress);
    Task LogAccessDeniedAsync(Guid? userId, string resource, string ipAddress);
    Task LogRateLimitExceededAsync(string ipAddress, string endpoint);
    Task LogSuspiciousActivityAsync(Guid? userId, string activity, string ipAddress);
}
```

### Ejemplos de logs

```json
{
  "timestamp": "2026-05-19T10:30:45Z",
  "level": "Warning",
  "category": "Security.Login",
  "message": "Failed login attempt",
  "userId": null,
  "email": "attacker@example.com",
  "ipAddress": "192.168.1.100",
  "attemptCount": 5,
  "isLocked": true
}

{
  "timestamp": "2026-05-19T10:35:20Z",
  "level": "Warning",
  "category": "Security.RateLimit",
  "message": "Rate limit exceeded",
  "ipAddress": "203.0.113.50",
  "endpoint": "/Auth/Login",
  "limit": 5,
  "period": "1m"
}

{
  "timestamp": "2026-05-19T11:00:00Z",
  "level": "Information",
  "category": "Security.Audit",
  "message": "Reservation cancelled",
  "userId": "12345678-1234-5678-1234-567812345678",
  "entityType": "Reservation",
  "entityId": "abcdef01-2345-6789-abcd-ef0123456789",
  "ipAddress": "192.168.1.200"
}
```

### Ubicación de logs

- **Desarrollo:** `logs/fodun-{fecha}.log`
- **Producción:** Azure Application Insights / ELK Stack

---

## 9. Gestión de Secretos

### NO HACER ❌

```csharp
// ❌ Nunca en appsettings.json
{
  "Database": {
    "Password": "MyPassword123!"
  }
}

// ❌ Nunca hardcodeado
var password = "MyPassword123!";

// ❌ Nunca en comentarios
// TODO: Use password: TuPassword123!
```

### HACER ✅

```bash
# Desarrollo: User Secrets
dotnet user-secrets set "Database:Password" "MyPassword123!"

# Producción: Azure Key Vault
az keyvault secret set --name db-password --value MyPassword123!
```

### Archivos a excluir de Git

```gitignore
# Secretos
appsettings.*.json
!appsettings.example.json
secrets.json
*.pfx
*.pem

# User Secrets
.vscode/settings.json

# Environment
.env
.env.local
.env.*.local
```

---

## 10. Buenas Prácticas

### ✅ SÍ hacer

- ✅ Validar SIEMPRE entrada de usuario en 2+ capas
- ✅ Usar HTTPS en todas partes
- ✅ Loguear eventos de seguridad
- ✅ Mantener secretos fuera del source code
- ✅ Revisar regularmente logs de seguridad
- ✅ Actualizar dependencies mensualmente
- ✅ Implementar backups regulares
- ✅ Monitorear intentos de ataque
- ✅ Implementar WAF en producción
- ✅ Hacer penetration testing anualmente

### ❌ NO hacer

- ❌ Loguear contraseñas o tokens
- ❌ Confiar en validación solo del cliente
- ❌ Usar credenciales débiles
- ❌ Exponer stack traces a usuarios
- ❌ Hacer SQL injection vulnerable
- ❌ Deshabilitar HTTPS
- ❌ Ignorar warnings de seguridad
- ❌ Usar cookies sin HttpOnly/Secure
- ❌ Almacenar datos sensibles sin encriptar
- ❌ Permitir CORS de cualquier origen

---

## Checklist de Seguridad Previo a Producción

- [ ] HTTPS configurado con certificado válido
- [ ] Security headers implementados
- [ ] Rate limiting activo
- [ ] Secretos en Azure Key Vault (no en código)
- [ ] Logs de seguridad configurados
- [ ] CORS restringido a dominios permitidos
- [ ] AllowedHosts no es "*"
- [ ] SQL Injection: No hay queries SQL manuales
- [ ] XSS: HTML sanitizado si es necesario
- [ ] CSRF: Tokens antiforgery validados
- [ ] Autenticación: Requiere HTTPS
- [ ] Autorización: Implementada en servicios
- [ ] Password policy: Mínimo 8 chars + requisitos
- [ ] Backup automático de BD
- [ ] Monitoreo y alertas configuradas
- [ ] Tests de seguridad ejecutados
- [ ] Penetration testing planificado

---

**Documentación actualizada:** 2026-05-19  
**Versión:** 1.0  
**Status:** En implementación
