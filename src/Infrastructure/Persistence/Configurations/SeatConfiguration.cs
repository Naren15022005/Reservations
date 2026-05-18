namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => new { x.AccommodationId, x.SeatNumber }).IsUnique();
        
        builder.Property(x => x.SeatNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        
        builder.HasMany(x => x.Tariffs)
            .WithOne(x => x.Seat)
            .HasForeignKey(x => x.SeatId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.Blackouts)
            .WithOne(x => x.Seat)
            .HasForeignKey(x => x.SeatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
