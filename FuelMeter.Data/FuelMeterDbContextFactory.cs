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
                "Server=fuelmeter.shrijiitservices.com;Database=fuelmeterdb_live;User Id=fuelmeterdb;Password=Nn_S?id5jKb7e1mg;TrustServerCertificate=false;MultipleActiveResultSets=True;Encrypt=False;")
            .Options;

        return new FuelMeterDbContext(options);
    }
}
