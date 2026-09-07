using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpdateCenter.Api.Contracts;
using UpdateCenter.Api.Domain;
using UpdateCenter.Api.Services;

namespace UpdateCenter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/releases")]
public class ReleasesController : ControllerBase
{
    private readonly UpdateService _updates;

    public ReleasesController(UpdateService updates)
    {
        _updates = updates;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReleaseRequest request)
    {
        try
        {
            var user = User.Identity?.Name ?? "unknown";
            var release = await _updates.CreateReleaseAsync(request, user);
            return Ok(DtoMap.Release(release));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var release = await _updates.GetReleaseAsync(id);
        return release is null ? NotFound() : Ok(DtoMap.Release(release));
    }

    [HttpPost("{id}/file")]
    [RequestSizeLimit(1024L * 1024L * 1024L)]
    public async Task<IActionResult> Upload(string id, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "فایل انتخاب نشده است." });
        }

        try
        {
            var saved = await _updates.SaveFileAsync(id, file);
            return Ok(DtoMap.File(saved));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> Publish(string id)
    {
        try
        {
            await _updates.PublishAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/rollback")]
    public async Task<IActionResult> Rollback(string id)
    {
        try
        {
            await _updates.RollbackAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id)
    {
        var release = await _updates.GetReleaseAsync(id);
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

        return PhysicalFile(path, file.ContentType, file.FileName);
    }

    [HttpPut("{id}/status/{status}")]
    public async Task<IActionResult> Status(string id, string status)
    {
        if (!Enum.TryParse<ReleaseStatus>(status, true, out var parsed))
        {
            return BadRequest(new { message = "وضعیت نامعتبر است." });
        }

        try
        {
            await _updates.SetStatusAsync(id, parsed);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
