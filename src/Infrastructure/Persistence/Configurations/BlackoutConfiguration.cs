using FODUN.Reservations.Domain.Aggregates.Accommodation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class BlackoutConfiguration : IEntityTypeConfiguration<Blackout>
{
    public void Configure(EntityTypeBuilder<Blackout> builder)
    {
        builder.ToTable("Blackouts");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("NEWID()");
        builder.Property(b => b.Reason).HasMaxLength(255);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(b => new { b.SeatId, b.StartDate, b.EndDate })
            .HasDatabaseName("IX_Blackouts_SeatId_Dates");
    }
}
