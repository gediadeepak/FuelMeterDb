using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelMeter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityStandingCharge",
                table: "UserSettings",
                type: "decimal(10,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GasStandingCharge",
                table: "UserSettings",
                type: "decimal(10,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityStandingCharge",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "GasStandingCharge",
                table: "UserSettings");
        }
    }
}
