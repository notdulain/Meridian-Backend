using RouteService.API.Models;

namespace RouteService.API.Helpers;

public static class FuelCostCalculator
{
    public static double CalculateFuelConsumption(double distanceKm, double fuelEfficiency)
    {
        if (fuelEfficiency <= 0 || !double.IsFinite(fuelEfficiency))
            throw new ArgumentException("Fuel efficiency must be greater than zero.", nameof(fuelEfficiency));

        if (!double.IsFinite(distanceKm))
            throw new ArgumentException("Distance must be a finite number.", nameof(distanceKm));

        if (distanceKm <= 0)
            return 0;

        var fuelConsumption = distanceKm / fuelEfficiency;
        if (!double.IsFinite(fuelConsumption))
            throw new ArgumentException("Fuel consumption overflowed during calculation.", nameof(distanceKm));

        return fuelConsumption;
    }

    public static double CalculateFuelCost(double fuelConsumption, double fuelPrice)
    {
        if (fuelPrice < 0 || !double.IsFinite(fuelPrice))
            throw new ArgumentException("Fuel price must be zero or greater.", nameof(fuelPrice));

        if (!double.IsFinite(fuelConsumption))
            throw new ArgumentException("Fuel consumption must be a finite number.", nameof(fuelConsumption));

        if (fuelConsumption <= 0)
            return 0;

        var fuelCost = fuelConsumption * fuelPrice;
        if (!double.IsFinite(fuelCost))
            throw new ArgumentException("Fuel cost overflowed during calculation.", nameof(fuelConsumption));

        return fuelCost;
    }

    public static FuelMetrics CalculateFuelMetrics(double distanceMeters, double fuelEfficiency, double fuelPrice)
    {
        if (!double.IsFinite(distanceMeters))
            throw new ArgumentException("Distance meters must be a finite number.", nameof(distanceMeters));

        if (fuelEfficiency <= 0 || !double.IsFinite(fuelEfficiency))
            throw new ArgumentException("Fuel efficiency must be greater than zero.", nameof(fuelEfficiency));

        if (fuelPrice < 0 || !double.IsFinite(fuelPrice))
            throw new ArgumentException("Fuel price must be zero or greater.", nameof(fuelPrice));

        if (distanceMeters <= 0)
        {
            return new FuelMetrics
            {
                DistanceKm = 0,
                FuelConsumptionLitres = 0,
                FuelCostLKR = 0
            };
        }

        var distanceKm = distanceMeters / 1000d;
        if (!double.IsFinite(distanceKm))
            throw new ArgumentException("Distance conversion overflowed during calculation.", nameof(distanceMeters));

        var fuelConsumption = CalculateFuelConsumption(distanceKm, fuelEfficiency);
        var fuelCost = CalculateFuelCost(fuelConsumption, fuelPrice);

        return new FuelMetrics
        {
            DistanceKm = Math.Round(distanceKm, 2, MidpointRounding.AwayFromZero),
            FuelConsumptionLitres = Math.Round(fuelConsumption, 2, MidpointRounding.AwayFromZero),
            FuelCostLKR = Math.Round(fuelCost, 2, MidpointRounding.AwayFromZero)
        };
    }
}
