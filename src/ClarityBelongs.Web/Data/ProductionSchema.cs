using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Data;

public static class ProductionSchema
{
    public const string RequiredMigrationId = "20261001-000-manual-sql-baseline";

    public static async Task VerifyAsync(
        ClarityDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException("Clarity Belongs database is not reachable.");

        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var tableCommand = db.Database.GetDbConnection().CreateCommand();
            tableCommand.CommandText =
                "SELECT CASE WHEN OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL THEN 0 ELSE 1 END;";

            var tableExists = Convert.ToInt32(
                await tableCommand.ExecuteScalarAsync(cancellationToken)) == 1;

            if (!tableExists)
            {
                throw new InvalidOperationException(
                    $"Clarity Belongs production schema is not current. Run database/migrations/{RequiredMigrationId}.sql before publishing this build.");
            }

            await using var migrationCommand = db.Database.GetDbConnection().CreateCommand();
            migrationCommand.CommandText =
                "SELECT COUNT(1) FROM dbo.SchemaMigrations WHERE MigrationId = @migrationId;";

            var parameter = migrationCommand.CreateParameter();
            parameter.ParameterName = "@migrationId";
            parameter.Value = RequiredMigrationId;
            migrationCommand.Parameters.Add(parameter);

            var applied = Convert.ToInt32(
                await migrationCommand.ExecuteScalarAsync(cancellationToken)) == 1;

            if (!applied)
            {
                throw new InvalidOperationException(
                    $"Clarity Belongs production schema is not current. Run database/migrations/{RequiredMigrationId}.sql before publishing this build.");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
