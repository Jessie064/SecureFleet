using SecureFleet.Api.Models;

namespace SecureFleet.Api.Services;

public class FuelCalculationService
{
    private readonly IDataStore _store;
    public FuelCalculationService(IDataStore store) => _store = store;

    public async Task<FuelEstimate?> EstimateAsync(Guid vehicleId, Guid routeId)
    {
        var vehicle = await _store.GetVehicleAsync(vehicleId);
        var route = await _store.GetRouteAsync(routeId);
        if (vehicle is null || route is null) return null;

        var price = await _store.GetFuelPriceAsync(vehicle.FuelType)
                    ?? new FuelPrice(vehicle.FuelType, 1.50m, "PHP");

        var efficiency = vehicle.FuelEfficiencyKmpl <= 0 ? 10m : vehicle.FuelEfficiencyKmpl;
        var liters = Math.Round(route.DistanceKm / efficiency, 2);
        var cost = Math.Round(liters * price.PricePerLiter, 2);

        return new FuelEstimate(
            route.DistanceKm,
            efficiency,
            liters,
            price.PricePerLiter,
            cost,
            price.Currency
        );
    }

    public static decimal HaversineKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double R = 6371.0;
        var dLat = ToRad((double)(lat2 - lat1));
        var dLng = ToRad((double)(lng2 - lng1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad((double)lat1)) * Math.Cos(ToRad((double)lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (decimal)(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
