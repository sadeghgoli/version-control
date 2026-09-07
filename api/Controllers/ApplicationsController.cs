using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpdateCenter.Api.Contracts;
using UpdateCenter.Api.Services;

namespace UpdateCenter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly UpdateService _updates;

    public ApplicationsController(UpdateService updates)
    {
        _updates = updates;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _updates.ListAppsAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request)
    {
        try
        {
            var app = await _updates.CreateAppAsync(request);
            return Ok(DtoMap.App(app, null));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/releases")]
    public async Task<IActionResult> Releases(string id)
    {
        var items = await _updates.ListReleasesAsync(id);
        return Ok(items.Select(DtoMap.Release));
    }
}
