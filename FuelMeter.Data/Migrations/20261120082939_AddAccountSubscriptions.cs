using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelMeter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptedPurchaseToken = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutoRenewing = table.Column<bool>(type: "bit", nullable: false),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestOrderId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountTrials",
                columns: table => new
                {
                    IdentityHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetainUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTrials", x => x.IdentityHash);
                    table.ForeignKey(
                        name: "FK_AccountTrials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AiScanRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DayKey = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReadingValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiScanRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiScanRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "ApplicationSettings",
                columns: new[] { "Key", "UpdatedAtUtc", "Value" },
                values: new object[,]
                {
                    { "Subscription.Android.PremiumAccessEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "true" },
                    { "Subscription.Android.PurchasesEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "true" },
                    { "Subscription.Google.BasePlanId", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "monthly" },
                    { "Subscription.Google.NotificationAudience", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "Subscription.Google.NotificationServiceAccount", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { "Subscription.Google.PackageName", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "com.fuelmeter.app" },
                    { "Subscription.Google.ProductId", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "fuelmeter_premium_monthly" },
                    { "Subscription.Google.ReverifyMinutes", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "5" },
                    { "Subscription.iOS.PremiumAccessEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "false" },
                    { "Subscription.iOS.PurchasesEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "false" },
                    { "Subscription.PriceGBP", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "1.99" },
                    { "Subscription.Scan.InputUsdPerMillionTokens", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "0.25" },
                    { "Subscription.Scan.Model", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "gpt-5-mini" },
                    { "Subscription.Scan.OutputUsdPerMillionTokens", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "2.00" },
                    { "Subscription.Scans.AttemptsDaily", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "10" },
                    { "Subscription.Scans.AttemptsMonthly", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "60" },
                    { "Subscription.Scans.Daily", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "5" },
                    { "Subscription.Scans.Monthly", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "30" },
                    { "Subscription.Scans.ReservationSeconds", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "120" },
                    { "Subscription.Scans.TimeZone", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Europe/London" },
                    { "Subscription.Trial.Hours", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "24" },
                    { "Subscription.Trial.RetentionYears", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "1" },
                    { "Subscription.Trial.Scans", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "1" },
                    { "Subscription.Web.PremiumAccessEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "true" },
                    { "Subscription.Web.PurchasesEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "false" },
                    { "Subscription.Windows.PremiumAccessEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "true" },
                    { "Subscription.Windows.PurchasesEnabled", new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc), "false" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubscriptions_TokenHash",
                table: "AccountSubscriptions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubscriptions_UserId_PeriodEndUtc",
                table: "AccountSubscriptions",
                columns: new[] { "UserId", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTrials_RetainUntilUtc",
                table: "AccountTrials",
                column: "RetainUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTrials_UserId",
                table: "AccountTrials",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiScanRequests_UserId_DayKey_Status",
                table: "AiScanRequests",
                columns: new[] { "UserId", "DayKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AiScanRequests_UserId_IdempotencyKey",
                table: "AiScanRequests",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiScanRequests_UserId_PeriodKey_Status",
                table: "AiScanRequests",
                columns: new[] { "UserId", "PeriodKey", "Status" });

            // The account owner requested complimentary access for this existing account.
            // Registration never grants access based on an unverified email address.
            migrationBuilder.Sql("""
                INSERT INTO [ApplicationSettings] ([Key], [Value], [UpdatedAtUtc])
                SELECT CONCAT('Subscription.ComplimentaryUser.', [Id]), 'true', SYSUTCDATETIME()
                FROM [Users]
                WHERE LOWER([Email]) = 'support@shrijiitservices.com' AND [IsActive] = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountSubscriptions");

            migrationBuilder.DropTable(
                name: "AccountTrials");

            migrationBuilder.DropTable(
                name: "AiScanRequests");

            migrationBuilder.DropTable(
                name: "ApplicationSettings");
        }
    }
}
