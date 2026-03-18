using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteService.API.Migrations
{
    public partial class AddRouteHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteHistories",
                columns: table => new
                {
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DistanceKm = table.Column<double>(type: "float", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    FuelCostLkr = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FuelConsumptionLitres = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Polyline = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Selected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteHistories", x => x.RouteId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteHistories_Origin_Destination",
                table: "RouteHistories",
                columns: new[] { "Origin", "Destination" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteHistories");
        }
    }
}
