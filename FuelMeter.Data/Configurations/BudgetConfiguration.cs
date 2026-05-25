using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelMeter.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FuelType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Year)
            .IsRequired();

        builder.Property(b => b.Month)
            .IsRequired();

        builder.Property(b => b.BudgetLimit)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.Notes)
            .HasMaxLength(500);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt);

        // Relationships
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("IX_Budget_UserId");

        builder.HasIndex(b => new { b.UserId, b.FuelType, b.Year, b.Month })
            .IsUnique()
            .HasDatabaseName("IX_Budget_User_FuelType_Year_Month");
    }
}
