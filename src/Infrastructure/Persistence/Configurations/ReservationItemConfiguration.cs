using FODUN.Reservations.Domain.Aggregates.Reservation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("ReservationItems");
        builder.HasKey(ri => ri.Id);
        builder.Property(ri => ri.Id).HasDefaultValueSql("NEWID()");
        builder.Property(ri => ri.PricePerNight).HasColumnType("decimal(18,2)").IsRequired();

        builder.Ignore(ri => ri.Subtotal);

        builder.HasIndex(ri => ri.SeatId)
            .HasDatabaseName("IX_ReservationItems_SeatId");
    }
}
