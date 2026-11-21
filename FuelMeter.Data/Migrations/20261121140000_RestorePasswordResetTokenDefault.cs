using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelMeter.Data.Migrations;

[DbContext(typeof(FuelMeterDbContext))]
[Migration("20261121140000_RestorePasswordResetTokenDefault")]
public class RestorePasswordResetTokenDefault : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @tableId int = OBJECT_ID(N'PasswordResetTokens', N'U');
            IF @tableId IS NULL OR COL_LENGTH(N'PasswordResetTokens', N'IsUsed') IS NULL
                THROW 51000, 'The password reset token table or IsUsed column is missing.', 1;

            -- EF omits the initial false value because the model declares a database default.
            -- Some legacy installations have the NOT NULL column without that default.
            IF EXISTS (SELECT 1 FROM sys.columns
                       WHERE object_id = @tableId AND name = N'IsUsed' AND default_object_id = 0)
            BEGIN
                DECLARE @sql nvarchar(max) = N'ALTER TABLE '
                    + QUOTENAME(OBJECT_SCHEMA_NAME(@tableId)) + N'.' + QUOTENAME(OBJECT_NAME(@tableId))
                    + N' ADD DEFAULT (CONVERT(bit, 0)) FOR [IsUsed];';
                EXEC sys.sp_executesql @sql;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve the default required by the original schema and older app versions.
    }
}
