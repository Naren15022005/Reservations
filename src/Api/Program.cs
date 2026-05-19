using System.Threading.RateLimiting;
using FluentValidation;
using FODUN.Reservations.Application.Commands.CancelReservation;
using FODUN.Reservations.Application.Commands.CreateReservation;
using FODUN.Reservations.Application.Commands.CreateUser;
using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Application.Validators;
using FODUN.Reservations.Api.Services;
using FODUN.Reservations.Infrastructure;
using FODUN.Reservations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/fodun-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure (Repositories, Services + DbContext)
builder.Services.AddInfrastructure(builder.Configuration);

// Application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// FluentValidation
builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserValidator>();
builder.Services.AddScoped<IValidator<CreateReservationCommand>, CreateReservationValidator>();
builder.Services.AddScoped<IValidator<CancelReservationCommand>, CancelReservationValidator>();

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Dashboard");
    options.Conventions.AuthorizeFolder("/Reservations");
}).AddRazorRuntimeCompilation();

// Autenticación por cookies + Google OAuth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30));
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = ".Ext";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]     ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.SignInScheme = "External";
        options.SaveTokens = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip   = ctx.Connection.RemoteIpAddress?.ToString()
                ?? ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? "anon";
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Rutas de autenticación: límite estricto
        if (path.StartsWith("/auth/"))
            return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window      = TimeSpan.FromMinutes(1),
                    QueueLimit  = 0
                });

        // General
        return RateLimitPartition.GetFixedWindowLimiter($"global:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync(
            "Demasiadas solicitudes. Intenta de nuevo en un minuto.");
    };
});

var app = builder.Build();

// Crear esquema y sembrar datos en Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ReservationsDbContext>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await context.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(context, seedLogger);
    await StoredProcedureInitializer.EnsureCreatedAsync(context, seedLogger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseSerilogRequestLogging();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Frame-Options"]           = "DENY";
    ctx.Response.Headers["X-Content-Type-Options"]    = "nosniff";
    ctx.Response.Headers["X-XSS-Protection"]          = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"]           = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]        = "geolocation=(), microphone=(), camera=()";
    ctx.Response.Headers["Content-Security-Policy"]   =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://fonts.gstatic.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https://maps.gstatic.com https://maps.googleapis.com; " +
        "frame-src https://maps.google.com; " +
        "connect-src 'self';";
    await next();
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
