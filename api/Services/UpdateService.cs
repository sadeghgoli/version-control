using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using UpdateCenter.Api.Contracts;
using UpdateCenter.Api.Data;
using UpdateCenter.Api.Domain;

namespace UpdateCenter.Api.Services;

public class UpdateService
{
    private readonly UpdateDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UpdateService(UpdateDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string StorageRoot => Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "storage"));

    public async Task<CheckUpdateResponse> CheckAsync(CheckUpdateRequest request)
    {
        var channel = ParseChannel(request.Channel);
        var latest = await LatestPublishedAsync(request.AppId, channel);
        if (latest is null || VersionCompare.Compare(request.Version, latest.Version) >= 0)
        {
            return new CheckUpdateResponse(false, false, false, "NONE", null, request.Version, null);
        }

        var belowMinimum = !string.IsNullOrWhiteSpace(latest.MinimumVersion)
            && VersionCompare.IsLessThan(request.Version, latest.MinimumVersion);
        var block = latest.UpdateType == UpdateType.Critical || belowMinimum;
        var required = block || latest.UpdateType == UpdateType.Mandatory;
        var reason = latest.UpdateType == UpdateType.Critical
            ? "CRITICAL"
            : belowMinimum
                ? "MINIMUM_VERSION"
                : latest.UpdateType == UpdateType.Mandatory
                    ? "MANDATORY"
                    : "OPTIONAL";
        var message = block
            ? $"این نسخه دارای بروزرسانی ضروری است. لطفاً به نسخه {latest.Version} بروزرسانی کنید."
            : $"نسخه {latest.Version} در دسترس است.";

        return new CheckUpdateResponse(
            true,
            required,
            block,
            reason,
            message,
            request.Version,
            DtoMap.Release(latest)
        );
    }

    public Task<AppRelease?> GetReleaseAsync(string id) =>
        _db.Releases.Include(x => x.Files).FirstOrDefaultAsync(x => x.Id == id);

    public Task<AppRelease?> GetPublishedReleaseAsync(string id) =>
        _db.Releases.Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == ReleaseStatus.Published);

    public async Task IncrementDownloadAsync(string releaseId)
    {
        var release = await _db.Releases.FirstOrDefaultAsync(x => x.Id == releaseId);
        if (release is null)
        {
            return;
        }

        release.DownloadCount += 1;
        await _db.SaveChangesAsync();
    }

    public async Task<List<ApplicationDto>> ListAppsAsync()
    {
        var apps = await _db.Applications.OrderBy(x => x.Name).ToListAsync();
        var result = new List<ApplicationDto>();
        foreach (var app in apps)
        {
            var latest = await LatestPublishedAsync(app.Id, ReleaseChannel.Stable);
            result.Add(DtoMap.App(app, latest?.Version));
        }
        return result;
    }

    public async Task<AppApplication> CreateAppAsync(CreateApplicationRequest request)
    {
        if (await _db.Applications.AnyAsync(x => x.Id == request.Id))
        {
            throw new InvalidOperationException("این شناسه برنامه قبلاً ثبت شده است.");
        }

        var app = new AppApplication
        {
            Id = request.Id.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Platform = Enum.TryParse<AppPlatform>(request.Platform, true, out var platform)
                ? platform
                : throw new InvalidOperationException("پلتفرم نامعتبر است."),
            Description = request.Description,
            Active = true
        };
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();
        return app;
    }

    public async Task<List<AppRelease>> ListReleasesAsync(string applicationId) =>
        await _db.Releases.Include(x => x.Files)
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task<AppRelease> CreateReleaseAsync(CreateReleaseRequest request, string user)
    {
        if (!await _db.Applications.AnyAsync(x => x.Id == request.ApplicationId))
        {
            throw new InvalidOperationException("برنامه پیدا نشد.");
        }

        var id = await NextReleaseIdAsync();
        var release = new AppRelease
        {
            Id = id,
            ApplicationId = request.ApplicationId,
            Version = request.Version.Trim(),
            BuildNumber = request.BuildNumber,
            ReleaseNotes = request.ReleaseNotes?.Trim() ?? "",
            MinimumVersion = string.IsNullOrWhiteSpace(request.MinimumVersion) ? null : request.MinimumVersion.Trim(),
            UpdateType = Enum.TryParse<UpdateType>(request.UpdateType, true, out var updateType)
                ? updateType
                : throw new InvalidOperationException("نوع بروزرسانی نامعتبر است."),
            Channel = ParseChannel(request.Channel),
            Status = ReleaseStatus.Draft,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
        _db.Releases.Add(release);
        await _db.SaveChangesAsync();
        return release;
    }

    public async Task<ReleaseFile> SaveFileAsync(string releaseId, IFormFile file)
    {
        var release = await _db.Releases.Include(x => x.Files).FirstOrDefaultAsync(x => x.Id == releaseId)
            ?? throw new InvalidOperationException("نسخه پیدا نشد.");
        if (release.Status is ReleaseStatus.Published or ReleaseStatus.Deprecated or ReleaseStatus.Archived)
        {
            throw new InvalidOperationException("برای نسخه منتشرشده نمی‌توان فایل عوض کرد.");
        }

        Directory.CreateDirectory(StorageRoot);
        var safeName = Path.GetFileName(file.FileName);
        var storedName = $"{releaseId}_{safeName}";
        var fullPath = Path.Combine(StorageRoot, storedName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        string sha;
        await using (var read = File.OpenRead(fullPath))
        {
            var hash = await SHA256.HashDataAsync(read);
            sha = Convert.ToHexString(hash);
        }

        foreach (var old in release.Files.ToList())
        {
            var oldPath = Path.Combine(StorageRoot, Path.GetFileName(old.FilePath));
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
            _db.ReleaseFiles.Remove(old);
        }

        var entity = new ReleaseFile
        {
            ReleaseId = releaseId,
            FileName = safeName,
            FilePath = storedName,
            FileSize = file.Length,
            Sha256 = sha,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
        };
        _db.ReleaseFiles.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task PublishAsync(string id)
    {
        var release = await _db.Releases
            .Include(x => x.Files)
            .Include(x => x.Application)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("نسخه پیدا نشد.");
        if (release.Application?.Platform != AppPlatform.Pwa && release.Files.Count == 0)
        {
            throw new InvalidOperationException("قبل از انتشار باید فایل نصب آپلود شود.");
        }

        var current = await _db.Releases
            .Where(x => x.ApplicationId == release.ApplicationId && x.Channel == release.Channel && x.Status == ReleaseStatus.Published)
            .ToListAsync();
        foreach (var item in current)
        {
            item.Status = ReleaseStatus.Deprecated;
        }

        release.Status = ReleaseStatus.Published;
        release.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RollbackAsync(string id)
    {
        var release = await _db.Releases.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("نسخه پیدا نشد.");

        var previous = await _db.Releases
            .Where(x => x.ApplicationId == release.ApplicationId
                && x.Channel == release.Channel
                && x.Id != release.Id
                && x.Status != ReleaseStatus.Archived)
            .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync();

        if (previous is null)
        {
            throw new InvalidOperationException("نسخه قبلی برای بازگشت وجود ندارد.");
        }

        release.Status = ReleaseStatus.Deprecated;
        previous.Status = ReleaseStatus.Published;
        previous.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(string id, ReleaseStatus status)
    {
        var release = await _db.Releases.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("نسخه پیدا نشد.");
        release.Status = status;
        await _db.SaveChangesAsync();
    }

    private async Task<AppRelease?> LatestPublishedAsync(string appId, ReleaseChannel channel) =>
        await _db.Releases.Include(x => x.Files)
            .Where(x => x.ApplicationId == appId && x.Channel == channel && x.Status == ReleaseStatus.Published)
            .OrderByDescending(x => x.PublishedAt)
            .FirstOrDefaultAsync();

    private async Task<string> NextReleaseIdAsync()
    {
        var last = await _db.Releases
            .Select(x => x.Id)
            .ToListAsync();
        var max = last
            .Select(id => int.TryParse(id.Replace("REL-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"REL-{(max + 1).ToString().PadLeft(6, '0')}";
    }

    private static ReleaseChannel ParseChannel(string? value) =>
        Enum.TryParse<ReleaseChannel>(value, true, out var channel) ? channel : ReleaseChannel.Stable;
}
