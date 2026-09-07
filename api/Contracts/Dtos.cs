using UpdateCenter.Api.Domain;

namespace UpdateCenter.Api.Contracts;

public record CheckUpdateRequest(
    string AppId,
    string Version,
    string? Platform = null,
    int? Build = null,
    string Channel = "Stable"
);

public record CheckUpdateResponse(
    bool HasUpdate,
    bool UpdateRequired,
    bool BlockApplication,
    string? Reason,
    string? Message,
    string CurrentVersion,
    ReleaseDto? Release
);

public record FileDto(
    int Id,
    string Name,
    long Size,
    string Sha256,
    string ContentType,
    string DownloadUrl
);

public record ReleaseDto(
    string Id,
    string ApplicationId,
    string Version,
    int BuildNumber,
    string ReleaseNotes,
    string? MinimumVersion,
    string UpdateType,
    string Status,
    string Channel,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    int DownloadCount,
    IReadOnlyList<FileDto> Files
);

public record ApplicationDto(
    string Id,
    string Name,
    string Platform,
    string? Description,
    bool Active,
    string? LatestVersion
);

public record CreateApplicationRequest(string Id, string Name, string Platform, string? Description);

public record CreateReleaseRequest(
    string ApplicationId,
    string Version,
    int BuildNumber,
    string? ReleaseNotes,
    string? MinimumVersion,
    string UpdateType,
    string Channel
);

public static class DtoMap
{
    public static ApplicationDto App(AppApplication app, string? latest) =>
        new(app.Id, app.Name, app.Platform.ToString(), app.Description, app.Active, latest);

    public static ReleaseDto Release(AppRelease release) => new(
        release.Id,
        release.ApplicationId,
        release.Version,
        release.BuildNumber,
        release.ReleaseNotes,
        release.MinimumVersion,
        release.UpdateType.ToString(),
        release.Status.ToString(),
        release.Channel.ToString(),
        release.CreatedBy,
        release.CreatedAt,
        release.PublishedAt,
        release.DownloadCount,
        release.Files.Select(File).ToList()
    );

    public static FileDto File(ReleaseFile file) => new(
        file.Id,
        file.FileName,
        file.FileSize,
        file.Sha256,
        file.ContentType,
        $"/api/update/{file.ReleaseId}/download"
    );
}
