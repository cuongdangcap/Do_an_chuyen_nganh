using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Admissions.Infrastructure.Persistence;

public sealed class AdmissionsDbContextFactory : IDesignTimeDbContextFactory<AdmissionsDbContext>
{
    public AdmissionsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=AdmissionsAiSystem;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AdmissionsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AdmissionsDbContext(options);
    }
}
