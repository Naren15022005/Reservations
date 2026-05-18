namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => new { x.UserId, x.Status });
        
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.LaundryServiceCost).HasPrecision(18, 2);
        
        builder.HasMany(x => x.Items)
            .WithOne(x => x.Reservation)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
