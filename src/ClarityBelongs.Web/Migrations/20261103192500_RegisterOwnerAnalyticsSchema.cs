using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

// The physical AcquisitionEvents extensions are already managed by
// AcquisitionAnalyticsSchema.EnsureAsync (SQLite) and the versioned
// database/migrations SQL scripts (production SQL Server).
// Register the model state in EF without duplicating ALTER TABLE operations.
[DbContext(typeof(ClarityDbContext))]
[Migration("20261103192500_RegisterOwnerAnalyticsSchema")]
public partial class RegisterOwnerAnalyticsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
