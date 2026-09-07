namespace UpdateCenter.Api.Domain;

public enum AppPlatform
{
    Windows = 0,
    Android = 1,
    Pwa = 2,
    Service = 3
}

public enum UpdateType
{
    Optional = 0,
    Mandatory = 1,
    Critical = 2
}

public enum ReleaseStatus
{
    Draft = 0,
    Testing = 1,
    Published = 2,
    Deprecated = 3,
    Archived = 4
}

public enum ReleaseChannel
{
    Stable = 0,
    Beta = 1,
    Development = 2
}

public class AppApplication
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public AppPlatform Platform { get; set; }
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
    public ICollection<AppRelease> Releases { get; set; } = new List<AppRelease>();
}

public class AppRelease
{
    public string Id { get; set; } = "";
    public string ApplicationId { get; set; } = "";
    public AppApplication? Application { get; set; }
    public string Version { get; set; } = "";
    public int BuildNumber { get; set; }
    public string ReleaseNotes { get; set; } = "";
    public string? MinimumVersion { get; set; }
    public UpdateType UpdateType { get; set; } = UpdateType.Optional;
    public ReleaseStatus Status { get; set; } = ReleaseStatus.Draft;
    public ReleaseChannel Channel { get; set; } = ReleaseChannel.Stable;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int DownloadCount { get; set; }
    public ICollection<ReleaseFile> Files { get; set; } = new List<ReleaseFile>();
}

public class ReleaseFile
{
    public int Id { get; set; }
    public string ReleaseId { get; set; } = "";
    public AppRelease? Release { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string Sha256 { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
}
