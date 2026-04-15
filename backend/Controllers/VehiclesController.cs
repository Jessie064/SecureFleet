using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFleet.Api.Models;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Policy = "AnyAuthenticated")]
public class VehiclesController : ControllerBase
{
    private readonly IDataStore _store;
    public VehiclesController(IDataStore store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _store.GetVehiclesAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var v = await _store.GetVehicleAsync(id);
        return v is null ? NotFound() : Ok(v);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Plate))
            return BadRequest(new { error = "plate required" });
        var v = await _store.CreateVehicleAsync(req);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, v);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] StatusUpdate body)
    {
        var allowed = new[] { "idle", "active", "maintenance", "offline" };
        if (!allowed.Contains(body.Status))
            return BadRequest(new { error = "invalid status" });
        var ok = await _store.UpdateVehicleStatusAsync(id, body.Status);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/position")]
    public async Task<IActionResult> UpdatePosition(Guid id, [FromBody] VehiclePositionUpdate body)
    {
        var ok = await _store.UpdateVehiclePositionAsync(id, body.Lat, body.Lng);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _store.DeleteVehicleAsync(id);
        return ok ? NoContent() : NotFound();
    }

    public record StatusUpdate(string Status);
}
