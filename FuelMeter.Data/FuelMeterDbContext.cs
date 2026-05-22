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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FuelMeterDbContext).Assembly);
    }
}
