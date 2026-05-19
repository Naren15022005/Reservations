using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Aggregates.Accommodation;
using FODUN.Reservations.Domain.Aggregates.Notification;
using FODUN.Reservations.Domain.Aggregates.Reservation;
using FODUN.Reservations.Domain.Aggregates.User;
using FODUN.Reservations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Infrastructure.Persistence;

public sealed class ReservationsDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Accommodation> Accommodations => Set<Accommodation>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Blackout> Blackouts => Set<Blackout>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public ReservationsDbContext(DbContextOptions<ReservationsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccommodationConfiguration());
        modelBuilder.ApplyConfiguration(new SeatConfiguration());
        modelBuilder.ApplyConfiguration(new TariffConfiguration());
        modelBuilder.ApplyConfiguration(new BlackoutConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationItemConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
    }
}
