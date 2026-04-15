using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFleet.Api.Models;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/fuel")]
[Authorize(Policy = "AnyAuthenticated")]
public class FuelController : ControllerBase
{
    private readonly FuelCalculationService _fuel;
    private readonly IDataStore _store;
    public FuelController(FuelCalculationService fuel, IDataStore store)
    {
        _fuel = fuel;
        _store = store;
    }

    [HttpGet("prices")]
    public async Task<IActionResult> Prices() => Ok(await _store.GetFuelPricesAsync());

    [HttpPut("prices/{fuelType}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePrice(string fuelType, [FromBody] UpdateFuelPriceRequest req)
    {
        if (req.PricePerLiter < 0) return BadRequest(new { error = "price must be >= 0" });
        var updated = await _store.UpsertFuelPriceAsync(fuelType, req.PricePerLiter, req.Currency ?? "PHP");
        return Ok(updated);
    }

    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate([FromBody] FuelEstimateRequest req)
    {
        var est = await _fuel.EstimateAsync(req.VehicleId, req.RouteId);
        return est is null ? NotFound(new { error = "vehicle or route not found" }) : Ok(est);
    }
}
