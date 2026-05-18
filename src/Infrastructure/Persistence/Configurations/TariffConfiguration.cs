namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => new { x.SeatId, x.Season });
        
        builder.Property(x => x.PricePerNight).HasPrecision(18, 2);
        builder.Property(x => x.AdditionalPersonPrice).HasPrecision(18, 2);
    }
}
