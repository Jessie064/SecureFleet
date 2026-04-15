using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize(Policy = "AnyAuthenticated")]
public class TripsController : ControllerBase
{
    private readonly IDataStore _store;
    private readonly FuelCalculationService _fuel;
    public TripsController(IDataStore store, FuelCalculationService fuel)
    {
        _store = store;
        _fuel = fuel;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _store.GetTripsAsync());

    public record CreateTripRequest(Guid VehicleId, Guid RouteId, Guid? DriverId);

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTripRequest req)
    {
        var est = await _fuel.EstimateAsync(req.VehicleId, req.RouteId);
        if (est is null) return BadRequest(new { error = "invalid vehicle or route" });
        var trip = await _store.CreateTripAsync(req.VehicleId, req.RouteId, req.DriverId, est.LitersNeeded, est.TotalCost);
        return Ok(trip);
    }
}
