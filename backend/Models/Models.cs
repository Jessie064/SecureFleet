namespace SecureFleet.Api.Models;

public record Vehicle(
    Guid Id,
    string Plate,
    string? Make,
    string? Model,
    int? Year,
    string FuelType,
    decimal FuelEfficiencyKmpl,
    string Status,
    Guid? DriverId,
    decimal? CurrentLat,
    decimal? CurrentLng,
    DateTime? LastUpdate
);

public record FleetRoute(
    Guid Id,
    string Name,
    string Origin,
    decimal OriginLat,
    decimal OriginLng,
    string Destination,
    decimal DestLat,
    decimal DestLng,
    decimal DistanceKm
);

public record Trip(
    Guid Id,
    Guid VehicleId,
    Guid? RouteId,
    Guid? DriverId,
    string Status,
    decimal? EstimatedFuelCost,
    decimal? EstimatedLiters,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record FuelPrice(string FuelType, decimal PricePerLiter, string Currency);

public record FuelEstimate(
    decimal DistanceKm,
    decimal FuelEfficiencyKmpl,
    decimal LitersNeeded,
    decimal PricePerLiter,
    decimal TotalCost,
    string Currency
);

public record VehiclePositionUpdate(decimal Lat, decimal Lng);

public record CreateVehicleRequest(
    string Plate,
    string? Make,
    string? Model,
    int? Year,
    string FuelType,
    decimal FuelEfficiencyKmpl
);

public record FuelEstimateRequest(Guid VehicleId, Guid RouteId);

public record UpdateFuelPriceRequest(decimal PricePerLiter, string? Currency);

public record Profile(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? Phone,
    DateTime CreatedAt
);

public record CreateUserRequest(string Email, string FullName, string Role, string? Phone);
