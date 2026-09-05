using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

public partial class AddContactSubmissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContactSubmissions",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<long>(type: "INTEGER", nullable: true),
                Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                ContactEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                SourcePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactSubmissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContactSubmissions_CreatedUtc",
            table: "ContactSubmissions",
            column: "CreatedUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContactSubmissions");
    }
}
