using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
               .IsRequired()
               .HasMaxLength(500);

        builder.HasIndex(t => t.Token)
               .IsUnique()
               .HasDatabaseName("IX_PasswordResetTokens_Token");

        builder.Property(t => t.ExpiresAt)
               .IsRequired();

        builder.Property(t => t.IsUsed)
               .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");
    }
}
