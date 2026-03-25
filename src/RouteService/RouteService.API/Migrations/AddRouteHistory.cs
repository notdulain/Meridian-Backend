using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RouteService.API.Data;

#nullable disable

namespace RouteService.API.Migrations
{
    [DbContext(typeof(RouteServiceDbContext))]
    [Migration("20260319083939_AddRouteHistory")]
    public partial class AddRouteHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[RouteHistories]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[RouteHistories]
                    (
                        [RouteId] uniqueidentifier NOT NULL,
                        [Origin] nvarchar(256) NOT NULL,
                        [Destination] nvarchar(256) NOT NULL,
                        [DistanceKm] float NOT NULL,
                        [DurationMinutes] int NOT NULL,
                        [FuelCostLkr] decimal(18,2) NOT NULL,
                        [FuelConsumptionLitres] decimal(18,2) NOT NULL,
                        [Polyline] nvarchar(4000) NOT NULL,
                        [Selected] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_RouteHistories_CreatedAt] DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_RouteHistories] PRIMARY KEY ([RouteId])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_RouteHistories_Origin_Destination'
                      AND object_id = OBJECT_ID(N'[dbo].[RouteHistories]')
                )
                BEGIN
                    CREATE INDEX [IX_RouteHistories_Origin_Destination]
                    ON [dbo].[RouteHistories]([Origin], [Destination]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[RouteHistories]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[RouteHistories];
                END
                """);
        }
    }
}
