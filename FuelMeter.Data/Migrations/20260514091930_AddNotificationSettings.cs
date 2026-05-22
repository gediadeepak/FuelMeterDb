using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelMeter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ElectricityEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ElectricityFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Weekly"),
                    ElectricityHour = table.Column<int>(type: "int", nullable: false),
                    ElectricityMinute = table.Column<int>(type: "int", nullable: false),
                    ElectricityDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    ElectricityDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    GasEnabled = table.Column<bool>(type: "bit", nullable: false),
                    GasFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Weekly"),
                    GasHour = table.Column<int>(type: "int", nullable: false),
                    GasMinute = table.Column<int>(type: "int", nullable: false),
                    GasDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    GasDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserId",
                table: "NotificationSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationSettings");
        }
    }
}
