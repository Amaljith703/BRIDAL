using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MakeupArt.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION")
                         ?? "Server=127.0.0.1,1434;Database=MakeupArtDb;User Id=sa;Password=MakeupArt!Dev2026Sql;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3))
            .Options;
        return new AppDbContext(options);
    }
}
