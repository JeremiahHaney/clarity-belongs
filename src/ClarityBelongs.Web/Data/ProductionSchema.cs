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
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT CASE
                    WHEN OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM dbo.SchemaMigrations
                        WHERE MigrationId = @migrationId
                    )
                    THEN 1
                    ELSE 0
                END;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@migrationId";
            parameter.Value = RequiredMigrationId;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (Convert.ToInt32(result) != 1)
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
