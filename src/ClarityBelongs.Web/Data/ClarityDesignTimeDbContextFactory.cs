using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClarityBelongs.Web.Data;

public sealed class ClarityDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClarityDbContext>
{
    public ClarityDbContext CreateDbContext(string[] args)
    {
        var path = Environment.GetEnvironmentVariable(DatabasePathProvider.EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(path))
        {
            path = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    Directory.GetCurrentDirectory(),
                    ".data",
                    "clarity-design.db"));
        }

        var directory = System.IO.Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("The design-time database path has no parent directory.");
        Directory.CreateDirectory(directory);

        var options = new DbContextOptionsBuilder<ClarityDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        return new ClarityDbContext(options);
    }
}
