using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RouteService.API.Data;

#nullable disable

namespace RouteService.API.Migrations
{
    [DbContext(typeof(RouteServiceDbContext))]
    [Migration("20260403102000_AddRouteHistoryVehicleDriverColumns")]
    public partial class AddRouteHistoryVehicleDriverColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'[dbo].[RouteHistories]', N'VehicleId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RouteHistories] ADD [VehicleId] int NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'[dbo].[RouteHistories]', N'DriverId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RouteHistories] ADD [DriverId] int NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_RouteHistories_VehicleId_DriverId_CreatedAt'
                      AND object_id = OBJECT_ID(N'[dbo].[RouteHistories]')
                )
                BEGIN
                    CREATE INDEX [IX_RouteHistories_VehicleId_DriverId_CreatedAt]
                    ON [dbo].[RouteHistories]([VehicleId], [DriverId], [CreatedAt]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_RouteHistories_VehicleId_DriverId_CreatedAt'
                      AND object_id = OBJECT_ID(N'[dbo].[RouteHistories]')
                )
                BEGIN
                    DROP INDEX [IX_RouteHistories_VehicleId_DriverId_CreatedAt] ON [dbo].[RouteHistories];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'[dbo].[RouteHistories]', N'VehicleId') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[RouteHistories] DROP COLUMN [VehicleId];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'[dbo].[RouteHistories]', N'DriverId') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[RouteHistories] DROP COLUMN [DriverId];
                END
                """);
        }
    }
}
