namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class AccommodationConfiguration : IEntityTypeConfiguration<Accommodation>
{
    public void Configure(EntityTypeBuilder<Accommodation> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.Code).IsUnique();
        
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        
        builder.HasMany(x => x.Seats)
            .WithOne(x => x.Accommodation)
            .HasForeignKey(x => x.AccommodationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
