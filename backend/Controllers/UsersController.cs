using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFleet.Api.Models;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IDataStore _store;
    public UsersController(IDataStore store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _store.GetProfilesAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var role = (req.Role ?? "").ToLowerInvariant();
        if (role != "manager" && role != "driver")
            return BadRequest(new { error = "admin can only create manager or driver accounts" });
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { error = "email and fullName are required" });

        try
        {
            var existing = await _store.GetProfileByEmailAsync(req.Email);
            if (existing is not null) return Conflict(new { error = "email already exists" });
            var created = await _store.CreateProfileAsync(req.Email, req.FullName, role, req.Phone);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _store.DeleteProfileAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
