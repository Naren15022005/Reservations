namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PricePerNight).HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
    }
}
