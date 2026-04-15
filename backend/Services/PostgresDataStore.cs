using Dapper;
using Npgsql;
using SecureFleet.Api.Models;

namespace SecureFleet.Api.Services;

public class PostgresDataStore : IDataStore
{
    private readonly string _conn;
    public PostgresDataStore(string conn) => _conn = conn;

    private NpgsqlConnection Open() => new(_conn);

    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync()
    {
        using var c = Open();
        var rows = await c.QueryAsync<Vehicle>(@"
            select id, plate, make, model, year, fuel_type as FuelType,
                   fuel_efficiency_kmpl as FuelEfficiencyKmpl, status,
                   driver_id as DriverId, current_lat as CurrentLat,
                   current_lng as CurrentLng, last_update as LastUpdate
            from vehicles order by plate");
        return rows.ToList();
    }

    public async Task<Vehicle?> GetVehicleAsync(Guid id)
    {
        using var c = Open();
        return await c.QuerySingleOrDefaultAsync<Vehicle>(@"
            select id, plate, make, model, year, fuel_type as FuelType,
                   fuel_efficiency_kmpl as FuelEfficiencyKmpl, status,
                   driver_id as DriverId, current_lat as CurrentLat,
                   current_lng as CurrentLng, last_update as LastUpdate
            from vehicles where id = @id", new { id });
    }

    public async Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest req)
    {
        using var c = Open();
        var id = await c.ExecuteScalarAsync<Guid>(@"
            insert into vehicles (plate, make, model, year, fuel_type, fuel_efficiency_kmpl, status)
            values (@Plate, @Make, @Model, @Year, @FuelType, @FuelEfficiencyKmpl, 'idle')
            returning id", req);
        return (await GetVehicleAsync(id))!;
    }

    public async Task<bool> UpdateVehicleStatusAsync(Guid id, string status)
    {
        using var c = Open();
        var n = await c.ExecuteAsync(
            "update vehicles set status = @status, last_update = now() where id = @id",
            new { id, status });
        return n > 0;
    }

    public async Task<bool> UpdateVehiclePositionAsync(Guid id, decimal lat, decimal lng)
    {
        using var c = Open();
        var n = await c.ExecuteAsync(@"
            update vehicles set current_lat = @lat, current_lng = @lng, last_update = now()
            where id = @id", new { id, lat, lng });
        return n > 0;
    }

    public async Task<bool> DeleteVehicleAsync(Guid id)
    {
        using var c = Open();
        var n = await c.ExecuteAsync("delete from vehicles where id = @id", new { id });
        return n > 0;
    }

    public async Task<IReadOnlyList<FleetRoute>> GetRoutesAsync()
    {
        using var c = Open();
        var rows = await c.QueryAsync<FleetRoute>(@"
            select id, name, origin, origin_lat as OriginLat, origin_lng as OriginLng,
                   destination, dest_lat as DestLat, dest_lng as DestLng,
                   distance_km as DistanceKm
            from routes order by name");
        return rows.ToList();
    }

    public async Task<FleetRoute?> GetRouteAsync(Guid id)
    {
        using var c = Open();
        return await c.QuerySingleOrDefaultAsync<FleetRoute>(@"
            select id, name, origin, origin_lat as OriginLat, origin_lng as OriginLng,
                   destination, dest_lat as DestLat, dest_lng as DestLng,
                   distance_km as DistanceKm
            from routes where id = @id", new { id });
    }

    public async Task<IReadOnlyList<Trip>> GetTripsAsync()
    {
        using var c = Open();
        var rows = await c.QueryAsync<Trip>(@"
            select id, vehicle_id as VehicleId, route_id as RouteId, driver_id as DriverId,
                   status, estimated_fuel_cost as EstimatedFuelCost,
                   estimated_liters as EstimatedLiters,
                   started_at as StartedAt, completed_at as CompletedAt
            from trips order by created_at desc");
        return rows.ToList();
    }

    public async Task<Trip> CreateTripAsync(Guid vehicleId, Guid routeId, Guid? driverId, decimal liters, decimal cost)
    {
        using var c = Open();
        var id = await c.ExecuteScalarAsync<Guid>(@"
            insert into trips (vehicle_id, route_id, driver_id, status, estimated_liters, estimated_fuel_cost)
            values (@vehicleId, @routeId, @driverId, 'planned', @liters, @cost)
            returning id", new { vehicleId, routeId, driverId, liters, cost });
        return (await c.QuerySingleAsync<Trip>(@"
            select id, vehicle_id as VehicleId, route_id as RouteId, driver_id as DriverId,
                   status, estimated_fuel_cost as EstimatedFuelCost,
                   estimated_liters as EstimatedLiters,
                   started_at as StartedAt, completed_at as CompletedAt
            from trips where id = @id", new { id }));
    }

    public async Task<FuelPrice?> GetFuelPriceAsync(string fuelType)
    {
        using var c = Open();
        return await c.QuerySingleOrDefaultAsync<FuelPrice>(@"
            select fuel_type as FuelType, price_per_liter as PricePerLiter, currency
            from fuel_prices where fuel_type = @fuelType", new { fuelType });
    }

    public async Task<IReadOnlyList<FuelPrice>> GetFuelPricesAsync()
    {
        using var c = Open();
        var rows = await c.QueryAsync<FuelPrice>(@"
            select fuel_type as FuelType, price_per_liter as PricePerLiter, currency
            from fuel_prices");
        return rows.ToList();
    }

    public async Task<FuelPrice> UpsertFuelPriceAsync(string fuelType, decimal pricePerLiter, string currency)
    {
        using var c = Open();
        return await c.QuerySingleAsync<FuelPrice>(@"
            insert into fuel_prices (fuel_type, price_per_liter, currency)
            values (@fuelType, @pricePerLiter, @currency)
            on conflict (fuel_type) do update
                set price_per_liter = excluded.price_per_liter,
                    currency = excluded.currency,
                    updated_at = now()
            returning fuel_type as FuelType, price_per_liter as PricePerLiter, currency",
            new { fuelType, pricePerLiter, currency });
    }

    public async Task<IReadOnlyList<Profile>> GetProfilesAsync()
    {
        using var c = Open();
        var rows = await c.QueryAsync<Profile>(@"
            select id, email, full_name as FullName, role, phone, created_at as CreatedAt
            from profiles order by email");
        return rows.ToList();
    }

    public async Task<Profile?> GetProfileByEmailAsync(string email)
    {
        using var c = Open();
        return await c.QuerySingleOrDefaultAsync<Profile>(@"
            select id, email, full_name as FullName, role, phone, created_at as CreatedAt
            from profiles where lower(email) = lower(@email)", new { email });
    }

    public async Task<Profile> CreateProfileAsync(string email, string fullName, string role, string? phone)
    {
        using var c = Open();
        return await c.QuerySingleAsync<Profile>(@"
            insert into profiles (email, full_name, role, phone)
            values (@email, @fullName, @role, @phone)
            returning id, email, full_name as FullName, role, phone, created_at as CreatedAt",
            new { email, fullName, role, phone });
    }

    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        using var c = Open();
        var n = await c.ExecuteAsync("delete from profiles where id = @id", new { id });
        return n > 0;
    }
}
