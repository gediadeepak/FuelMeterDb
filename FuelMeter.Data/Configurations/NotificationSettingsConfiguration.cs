using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("NotificationSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ElectricityFrequency).HasMaxLength(20).HasDefaultValue("Weekly");
        builder.Property(x => x.GasFrequency).HasMaxLength(20).HasDefaultValue("Weekly");

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
