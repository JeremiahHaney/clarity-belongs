using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

[DbContext(typeof(ClarityDbContext))]
[Migration("20260919210000_AddAcquisitionAnalytics")]
public partial class AddAcquisitionAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AcquisitionEvents",
            columns: table => new
            {
                Id = table.Column<long>(
                        type: "INTEGER",
                        nullable: false)
                    .Annotation(
                        "Sqlite:Autoincrement",
                        true),
                VisitorId = table.Column<string>(
                    type: "TEXT",
                    maxLength: 64,
                    nullable: false),
                UserId = table.Column<long>(
                    type: "INTEGER",
                    nullable: true),
                WorkspaceId = table.Column<long>(
                    type: "INTEGER",
                    nullable: true),
                EventType = table.Column<string>(
                    type: "TEXT",
                    maxLength: 64,
                    nullable: false),
                Path = table.Column<string>(
                    type: "TEXT",
                    maxLength: 500,
                    nullable: true),
                ProductSlug = table.Column<string>(
                    type: "TEXT",
                    maxLength: 100,
                    nullable: true),
                FollowId = table.Column<long>(
                    type: "INTEGER",
                    nullable: true),
                Source = table.Column<string>(
                    type: "TEXT",
                    maxLength: 100,
                    nullable: true),
                Medium = table.Column<string>(
                    type: "TEXT",
                    maxLength: 100,
                    nullable: true),
                Campaign = table.Column<string>(
                    type: "TEXT",
                    maxLength: 150,
                    nullable: true),
                OccurredAtUtc = table.Column<DateTime>(
                    type: "TEXT",
                    nullable: false)
            },
            constraints: table => table.PrimaryKey(
                "PK_AcquisitionEvents",
                x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AcquisitionEvents_EventType_OccurredAtUtc",
            table: "AcquisitionEvents",
            columns: new[]
            {
                "EventType",
                "OccurredAtUtc"
            });

        migrationBuilder.CreateIndex(
            name: "IX_AcquisitionEvents_UserId_OccurredAtUtc",
            table: "AcquisitionEvents",
            columns: new[]
            {
                "UserId",
                "OccurredAtUtc"
            });

        migrationBuilder.CreateIndex(
            name: "IX_AcquisitionEvents_VisitorId_OccurredAtUtc",
            table: "AcquisitionEvents",
            columns: new[]
            {
                "VisitorId",
                "OccurredAtUtc"
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AcquisitionEvents");
    }
}
