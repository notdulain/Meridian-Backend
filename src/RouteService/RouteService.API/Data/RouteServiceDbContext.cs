using Microsoft.EntityFrameworkCore;
using RouteService.API.Models;

namespace RouteService.API.Data;

public sealed class RouteServiceDbContext(DbContextOptions<RouteServiceDbContext> options) : DbContext(options)
{
    public DbSet<RouteHistory> RouteHistories => Set<RouteHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var routeHistory = modelBuilder.Entity<RouteHistory>();

        routeHistory.ToTable("RouteHistories");
        routeHistory.HasKey(x => x.RouteId);
        routeHistory.HasIndex(x => new { x.Origin, x.Destination });
        routeHistory.Property(x => x.FuelCostLkr).HasPrecision(18, 2);
        routeHistory.Property(x => x.FuelConsumptionLitres).HasPrecision(18, 2);
        routeHistory.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}
