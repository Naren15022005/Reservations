using FODUN.Reservations.Domain.Aggregates.Accommodation;
using FODUN.Reservations.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("Tariffs");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");
        builder.Property(t => t.Season)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.PricePerNight).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.AdditionalPersonPrice).HasColumnType("decimal(18,2)");
        builder.Property(t => t.DaysOfWeek).HasMaxLength(50);
        builder.Property(t => t.IsExceptional).HasDefaultValue(false);
        builder.Property(t => t.MinPersons).HasDefaultValue(1);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(t => new { t.SeatId, t.Season }).HasDatabaseName("IX_Tariffs_SeatId_Season");
    }
}
