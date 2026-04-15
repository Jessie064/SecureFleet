using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize(Policy = "AnyAuthenticated")]
public class RoutesController : ControllerBase
{
    private readonly IDataStore _store;
    public RoutesController(IDataStore store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _store.GetRoutesAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var r = await _store.GetRouteAsync(id);
        return r is null ? NotFound() : Ok(r);
    }
}
