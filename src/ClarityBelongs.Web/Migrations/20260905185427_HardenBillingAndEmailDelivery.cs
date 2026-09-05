using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations
{
    /// <inheritdoc />
    public partial class HardenBillingAndEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "Notifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetterAtUtc",
                table: "Notifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                table: "Notifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "Notifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStripeEventCreatedUtc",
                table: "Memberships",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DigestDeliveryStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    DigestDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestDeliveryStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    StripeCreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel_Status_NextAttemptAtUtc",
                table: "Notifications",
                columns: new[] { "Channel", "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DigestDeliveryStates_UserId_DigestDateUtc",
                table: "DigestDeliveryStates",
                columns: new[] { "UserId", "DigestDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_EventId",
                table: "StripeWebhookEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_StripeCreatedUtc",
                table: "StripeWebhookEvents",
                column: "StripeCreatedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigestDeliveryStates");

            migrationBuilder.DropTable(
                name: "StripeWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Channel_Status_NextAttemptAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeadLetterAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastStripeEventCreatedUtc",
                table: "Memberships");
        }
    }
}
