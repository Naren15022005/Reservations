namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class BlackoutConfiguration : IEntityTypeConfiguration<Blackout>
{
    public void Configure(EntityTypeBuilder<Blackout> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => new { x.SeatId, x.StartDate, x.EndDate });
    }
}
