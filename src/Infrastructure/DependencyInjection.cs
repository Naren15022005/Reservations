using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Domain.Interfaces;
using FODUN.Reservations.Infrastructure.External;
using FODUN.Reservations.Infrastructure.Persistence;
using FODUN.Reservations.Infrastructure.Persistence.Repositories;
using FODUN.Reservations.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FODUN.Reservations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReservationsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccommodationRepository, AccommodationRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IStoredProcedureRepository, StoredProcedureRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Servicios
        services.AddScoped<ITariffService, TariffService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
