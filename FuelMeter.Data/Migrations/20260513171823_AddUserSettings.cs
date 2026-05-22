using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelMeter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ElectricityProvider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ElectricityPricePerKwh = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ElectricityMonthlyBudget = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    GasProvider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GasPricePerUnit = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    GasMonthlyBudget = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "GBP"),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false, defaultValue: "£"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
