using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpdateCenter.Api.Contracts;
using UpdateCenter.Api.Services;

namespace UpdateCenter.Api.Controllers;

[ApiController]
[Route("api/update")]
public class UpdateController : ControllerBase
{
    private readonly UpdateService _updates;

    public UpdateController(UpdateService updates)
    {
        _updates = updates;
    }

    [AllowAnonymous]
    [HttpPost("check")]
    public async Task<ActionResult<CheckUpdateResponse>> Check([FromBody] CheckUpdateRequest request) =>
        await CheckCore(request);

    [AllowAnonymous]
    [HttpGet("check")]
    public async Task<ActionResult<CheckUpdateResponse>> CheckGet(
        [FromQuery] string appId,
        [FromQuery] string version,
        [FromQuery] string? platform,
        [FromQuery] int? build,
        [FromQuery] string channel = "Stable") =>
        await CheckCore(new CheckUpdateRequest(appId, version, platform, build, channel));

    private async Task<ActionResult<CheckUpdateResponse>> CheckCore(CheckUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppId) || string.IsNullOrWhiteSpace(request.Version))
        {
            return BadRequest(new { message = "appId و version لازم است." });
        }

        return Ok(await _updates.CheckAsync(request));
    }

    [AllowAnonymous]
    [HttpGet("{releaseId}")]
    public async Task<IActionResult> Get(string releaseId)
    {
        var release = await _updates.GetPublishedReleaseAsync(releaseId);
        return release is null ? NotFound() : Ok(DtoMap.Release(release));
    }

    [AllowAnonymous]
    [HttpGet("{releaseId}/download")]
    public async Task<IActionResult> Download(string releaseId)
    {
        var release = await _updates.GetPublishedReleaseAsync(releaseId);
        var file = release?.Files.FirstOrDefault();
        if (file is null)
        {
            return NotFound();
        }

        var path = Path.Combine(_updates.StorageRoot, Path.GetFileName(file.FilePath));
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        await _updates.IncrementDownloadAsync(releaseId);
        return PhysicalFile(path, file.ContentType, file.FileName);
    }
}
