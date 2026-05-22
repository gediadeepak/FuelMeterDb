using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class MeterReadingConfiguration : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> builder)
    {
        builder.ToTable("MeterReadings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FuelType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReadingValue).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.FuelType, x.ReadingDate });
    }
}
