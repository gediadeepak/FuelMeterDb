using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelMeter.Data.Migrations;

[DbContext(typeof(FuelMeterDbContext))]
[Migration("20261114120000_EnsureAccountDeletionCascades")]
public class EnsureAccountDeletionCascades : Migration
{
    private static readonly string[] AccountTables =
        ["PasswordResetTokens", "UserSettings", "MeterReadings", "NotificationSettings", "Budgets"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AccountTables)
        {
            migrationBuilder.Sql($"""
                DECLARE @child int = OBJECT_ID(N'{table}', N'U');
                DECLARE @users int = OBJECT_ID(N'Users', N'U');

                IF @child IS NULL OR @users IS NULL
                    THROW 51000, 'An account table is missing from the FuelMeter database.', 1;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys AS fk
                    JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
                    WHERE fk.parent_object_id = @child AND fk.referenced_object_id = @users
                      AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0
                      AND COL_NAME(@child, fkc.parent_column_id) = N'UserId'
                      AND COL_NAME(@users, fkc.referenced_column_id) = N'Id'
                      AND 1 = (SELECT COUNT(*) FROM sys.foreign_key_columns WHERE constraint_object_id = fk.object_id)
                )
                BEGIN
                    DECLARE @sql nvarchar(max) = N'ALTER TABLE '
                        + QUOTENAME(OBJECT_SCHEMA_NAME(@child)) + N'.' + QUOTENAME(OBJECT_NAME(@child))
                        + N' WITH CHECK ADD CONSTRAINT [FK_{table}_Users_AccountDeletion] FOREIGN KEY ([UserId]) REFERENCES '
                        + QUOTENAME(OBJECT_SCHEMA_NAME(@users)) + N'.' + QUOTENAME(OBJECT_NAME(@users))
                        + N' ([Id]) ON DELETE CASCADE;';
                    EXEC sys.sp_executesql @sql;
                END;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AccountTables)
        {
            migrationBuilder.Sql($"""
                DECLARE @child int = OBJECT_ID(N'{table}', N'U');
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = @child AND name = N'FK_{table}_Users_AccountDeletion'
                )
                BEGIN
                    DECLARE @sql nvarchar(max) = N'ALTER TABLE '
                        + QUOTENAME(OBJECT_SCHEMA_NAME(@child)) + N'.' + QUOTENAME(OBJECT_NAME(@child))
                        + N' DROP CONSTRAINT [FK_{table}_Users_AccountDeletion];';
                    EXEC sys.sp_executesql @sql;
                END;
                """);
        }
    }
}
