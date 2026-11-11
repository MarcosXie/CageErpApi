namespace FlyGates.Application.Entities.Sdks;

/// <summary>Manifesto de instaladores publicados no S3 (sdks/manifest.json), mantido pelo script publish-sdk.ps1.</summary>
public class SdkManifest
{
    public const string ObjectKey = "sdks/manifest.json";

    public int SchemaVersion { get; set; } = 1;
    public List<SdkManifestEntry> Releases { get; set; } = [];
}

public class SdkManifestEntry
{
    public string App { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTimeOffset ReleaseDate { get; set; }
}
