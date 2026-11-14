using FuelMeter.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelMeter.Data;

public class FuelMeterDbContext(DbContextOptions<FuelMeterDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<PushInstallation> PushInstallations => Set<PushInstallation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FuelMeterDbContext).Assembly);
        modelBuilder.Entity<PushInstallation>(entity =>
        {
            entity.HasKey(installation => installation.InstallationId);
            entity.Property(installation => installation.InstallationId)
                .HasMaxLength(22)
                .UseCollation("Latin1_General_100_BIN2");
            entity.HasOne(installation => installation.User)
                .WithMany()
                .HasForeignKey(installation => installation.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
