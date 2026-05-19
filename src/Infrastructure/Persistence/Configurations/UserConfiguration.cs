using FODUN.Reservations.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FODUN.Reservations.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("NEWID()");
        builder.Property(u => u.DocumentNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.DocumentNumber).IsUnique();

        builder.Property(u => u.FullName).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsEmailConfirmed).HasDefaultValue(false);
        builder.Property(u => u.LockoutEnabled).HasDefaultValue(false);
        builder.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(u => u.ExternalProvider).HasMaxLength(50);
        builder.Property(u => u.ExternalId).HasMaxLength(255);
    }
}
