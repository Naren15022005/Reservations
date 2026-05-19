using FODUN.Reservations.Domain.Aggregates.Reservation;
using FODUN.Reservations.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWID()");
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(ReservationStatus.Pending);

        builder.Property(r => r.TotalCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.LaundryServiceCost).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(r => r.LaundryService).HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(r => new { r.UserId, r.Status }).HasDatabaseName("IX_Reservations_UserId_Status");

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(ri => ri.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
