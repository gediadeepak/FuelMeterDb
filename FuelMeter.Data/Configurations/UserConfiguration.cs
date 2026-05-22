using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(u => u.IsEmailVerified)
               .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
               .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(u => u.PasswordResetTokens)
               .WithOne(t => t.User)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
