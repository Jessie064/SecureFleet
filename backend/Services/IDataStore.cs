using SecureFleet.Api.Models;

namespace SecureFleet.Api.Services;

public interface IDataStore
{
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync();
    Task<Vehicle?> GetVehicleAsync(Guid id);
    Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest req);
    Task<bool> UpdateVehicleStatusAsync(Guid id, string status);
    Task<bool> UpdateVehiclePositionAsync(Guid id, decimal lat, decimal lng);
    Task<bool> DeleteVehicleAsync(Guid id);

    Task<IReadOnlyList<FleetRoute>> GetRoutesAsync();
    Task<FleetRoute?> GetRouteAsync(Guid id);

    Task<IReadOnlyList<Trip>> GetTripsAsync();
    Task<Trip> CreateTripAsync(Guid vehicleId, Guid routeId, Guid? driverId, decimal liters, decimal cost);

    Task<FuelPrice?> GetFuelPriceAsync(string fuelType);
    Task<IReadOnlyList<FuelPrice>> GetFuelPricesAsync();
    Task<FuelPrice> UpsertFuelPriceAsync(string fuelType, decimal pricePerLiter, string currency);

    Task<IReadOnlyList<Profile>> GetProfilesAsync();
    Task<Profile?> GetProfileByEmailAsync(string email);
    Task<Profile> CreateProfileAsync(string email, string fullName, string role, string? phone);
    Task<bool> DeleteProfileAsync(Guid id);
}
