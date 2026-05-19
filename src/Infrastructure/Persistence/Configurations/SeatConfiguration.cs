using FODUN.Reservations.Domain.Aggregates.Accommodation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWID()");
        builder.Property(s => s.SeatNumber).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Type).HasMaxLength(100).IsRequired();
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(s => new { s.AccommodationId, s.SeatNumber }).IsUnique();

        builder.HasMany(s => s.Tariffs)
            .WithOne()
            .HasForeignKey(t => t.SeatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Blackouts)
            .WithOne()
            .HasForeignKey(b => b.SeatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
