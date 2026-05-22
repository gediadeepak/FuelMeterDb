using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ElectricityProvider).HasMaxLength(150);
        builder.Property(x => x.GasProvider).HasMaxLength(150);
        builder.Property(x => x.ElectricityPricePerKwh).HasColumnType("decimal(10,4)");
        builder.Property(x => x.ElectricityStandingCharge).HasColumnType("decimal(10,4)");
        builder.Property(x => x.GasPricePerUnit).HasColumnType("decimal(10,4)");
        builder.Property(x => x.GasStandingCharge).HasColumnType("decimal(10,4)");
        builder.Property(x => x.ElectricityMonthlyBudget).HasColumnType("decimal(10,2)");
        builder.Property(x => x.GasMonthlyBudget).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("GBP");
        builder.Property(x => x.CurrencySymbol).HasMaxLength(5).HasDefaultValue("£");

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
