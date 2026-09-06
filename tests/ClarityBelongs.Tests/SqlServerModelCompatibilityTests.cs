using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Tests;

public sealed class SqlServerModelCompatibilityTests
{
    [Fact]
    public void IndexedStringProperties_AreBoundedForSqlServer()
    {
        var options = new DbContextOptionsBuilder<ClarityDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=ClarityBelongsModelTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var db = new ClarityDbContext(options);
        var model = db.Model;
        var unboundedIndexedStrings = model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes()
                .SelectMany(index => index.Properties))
            .Where(property => property.ClrType == typeof(string))
            .Where(property => property.GetMaxLength() is null)
            .Select(property => $"{property.DeclaringType.Name}.{property.Name}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unboundedIndexedStrings);
    }
}
