using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelMeter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPushAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PushAlertSentAt",
                table: "Budgets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PushInstallations",
                columns: table => new
                {
                    InstallationId = table.Column<string>(type: "nvarchar(22)", maxLength: 22, nullable: false, collation: "Latin1_General_100_BIN2"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushInstallations", x => x.InstallationId);
                    table.ForeignKey(
                        name: "FK_PushInstallations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushInstallations_UserId",
                table: "PushInstallations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushInstallations");

            migrationBuilder.DropColumn(
                name: "PushAlertSentAt",
                table: "Budgets");
        }
    }
}
