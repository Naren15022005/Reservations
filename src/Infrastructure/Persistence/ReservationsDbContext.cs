namespace FODUN.Reservations.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

public class ReservationsDbContext(DbContextOptions<ReservationsDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Accommodation> Accommodations { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Tariff> Tariffs { get; set; }
    public DbSet<Blackout> Blackouts { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationItem> ReservationItems { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationsDbContext).Assembly);
    }
}
