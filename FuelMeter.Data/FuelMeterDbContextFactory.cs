using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FuelMeter.Data;

/// <summary>
/// Used by EF Core tools (dotnet ef migrations add / update)
/// </summary>
public class FuelMeterDbContextFactory : IDesignTimeDbContextFactory<FuelMeterDbContext>
{
    public FuelMeterDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FuelMeterDbContext>()
            .UseSqlServer(
                "Server=tcp:UOR01UGT23083FB,1433;Database=FuelMeterDb;User ID=sa;Password=P@ssw0rd;" +
                "TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=30;")
            .Options;

        return new FuelMeterDbContext(options);
    }
}
