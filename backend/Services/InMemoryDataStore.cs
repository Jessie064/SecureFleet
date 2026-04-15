using System.Collections.Concurrent;
using SecureFleet.Api.Models;

namespace SecureFleet.Api.Services;

public class InMemoryDataStore : IDataStore
{
    private readonly ConcurrentDictionary<Guid, Vehicle> _vehicles = new();
    private readonly ConcurrentDictionary<Guid, FleetRoute> _routes = new();
    private readonly ConcurrentDictionary<Guid, Trip> _trips = new();
    private readonly ConcurrentDictionary<string, FuelPrice> _fuel = new();
    private readonly ConcurrentDictionary<Guid, Profile> _profiles = new();

    public InMemoryDataStore()
    {
        Seed();
    }

    private void Seed()
    {
        _fuel["diesel"] = new FuelPrice("diesel", 1.45m, "PHP");
        _fuel["gasoline"] = new FuelPrice("gasoline", 1.60m, "PHP");

        var routes = new[]
        {
            new FleetRoute(Guid.NewGuid(), "Cebu Port to IT Park", "Cebu Port", 10.3000m, 123.9200m, "IT Park Lahug", 10.3303m, 123.9059m, 3.6m),
            new FleetRoute(Guid.NewGuid(), "Ayala Center to Mactan Airport", "Ayala Center Cebu", 10.3181m, 123.9055m, "Mactan-Cebu Airport", 10.3076m, 123.9790m, 8.9m),
            new FleetRoute(Guid.NewGuid(), "North Bus to South Bus", "North Bus Terminal", 10.3436m, 123.9229m, "South Bus Terminal", 10.2937m, 123.8948m, 6.1m)
        };
        foreach (var r in routes) _routes[r.Id] = r;

        var vehicles = new[]
        {
            new Vehicle(Guid.NewGuid(), "FLT-001", "Ford", "Transit", 2022, "diesel", 11.5m, "active", null, 10.3000m, 123.9200m, DateTime.UtcNow),
            new Vehicle(Guid.NewGuid(), "FLT-002", "Mercedes", "Sprinter", 2023, "diesel", 10.8m, "active", null, 10.3181m, 123.9055m, DateTime.UtcNow),
            new Vehicle(Guid.NewGuid(), "FLT-003", "Toyota", "Hiace", 2021, "gasoline", 9.4m, "idle", null, 10.3303m, 123.9059m, DateTime.UtcNow),
            new Vehicle(Guid.NewGuid(), "FLT-004", "Volvo", "FH16", 2020, "diesel", 4.2m, "maintenance", null, 10.3231m, 123.9225m, DateTime.UtcNow)
        };
        foreach (var v in vehicles) _vehicles[v.Id] = v;

        var bootstrapAdmin = new Profile(Guid.NewGuid(), "admin@securefleet.io", "Bootstrap Admin", "admin", null, DateTime.UtcNow);
        _profiles[bootstrapAdmin.Id] = bootstrapAdmin;
    }

    public Task<IReadOnlyList<Vehicle>> GetVehiclesAsync() =>
        Task.FromResult<IReadOnlyList<Vehicle>>(_vehicles.Values.OrderBy(v => v.Plate).ToList());

    public Task<Vehicle?> GetVehicleAsync(Guid id) =>
        Task.FromResult(_vehicles.TryGetValue(id, out var v) ? v : null);

    public Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest req)
    {
        var v = new Vehicle(Guid.NewGuid(), req.Plate, req.Make, req.Model, req.Year,
            req.FuelType, req.FuelEfficiencyKmpl, "idle", null, null, null, DateTime.UtcNow);
        _vehicles[v.Id] = v;
        return Task.FromResult(v);
    }

    public Task<bool> UpdateVehicleStatusAsync(Guid id, string status)
    {
        if (!_vehicles.TryGetValue(id, out var v)) return Task.FromResult(false);
        _vehicles[id] = v with { Status = status, LastUpdate = DateTime.UtcNow };
        return Task.FromResult(true);
    }

    public Task<bool> UpdateVehiclePositionAsync(Guid id, decimal lat, decimal lng)
    {
        if (!_vehicles.TryGetValue(id, out var v)) return Task.FromResult(false);
        _vehicles[id] = v with { CurrentLat = lat, CurrentLng = lng, LastUpdate = DateTime.UtcNow };
        return Task.FromResult(true);
    }

    public Task<bool> DeleteVehicleAsync(Guid id) =>
        Task.FromResult(_vehicles.TryRemove(id, out _));

    public Task<IReadOnlyList<FleetRoute>> GetRoutesAsync() =>
        Task.FromResult<IReadOnlyList<FleetRoute>>(_routes.Values.OrderBy(r => r.Name).ToList());

    public Task<FleetRoute?> GetRouteAsync(Guid id) =>
        Task.FromResult(_routes.TryGetValue(id, out var r) ? r : null);

    public Task<IReadOnlyList<Trip>> GetTripsAsync() =>
        Task.FromResult<IReadOnlyList<Trip>>(_trips.Values.OrderByDescending(t => t.StartedAt ?? DateTime.MinValue).ToList());

    public Task<Trip> CreateTripAsync(Guid vehicleId, Guid routeId, Guid? driverId, decimal liters, decimal cost)
    {
        var t = new Trip(Guid.NewGuid(), vehicleId, routeId, driverId, "planned", cost, liters, null, null);
        _trips[t.Id] = t;
        return Task.FromResult(t);
    }

    public Task<FuelPrice?> GetFuelPriceAsync(string fuelType) =>
        Task.FromResult(_fuel.TryGetValue(fuelType, out var f) ? f : null);

    public Task<IReadOnlyList<FuelPrice>> GetFuelPricesAsync() =>
        Task.FromResult<IReadOnlyList<FuelPrice>>(_fuel.Values.ToList());

    public Task<FuelPrice> UpsertFuelPriceAsync(string fuelType, decimal pricePerLiter, string currency)
    {
        var fp = new FuelPrice(fuelType, pricePerLiter, currency);
        _fuel[fuelType] = fp;
        return Task.FromResult(fp);
    }

    public Task<IReadOnlyList<Profile>> GetProfilesAsync() =>
        Task.FromResult<IReadOnlyList<Profile>>(_profiles.Values.OrderBy(p => p.Email).ToList());

    public Task<Profile?> GetProfileByEmailAsync(string email) =>
        Task.FromResult(_profiles.Values.FirstOrDefault(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<Profile> CreateProfileAsync(string email, string fullName, string role, string? phone)
    {
        if (_profiles.Values.Any(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("email already exists");
        var p = new Profile(Guid.NewGuid(), email, fullName, role, phone, DateTime.UtcNow);
        _profiles[p.Id] = p;
        return Task.FromResult(p);
    }

    public Task<bool> DeleteProfileAsync(Guid id) =>
        Task.FromResult(_profiles.TryRemove(id, out _));
}
