namespace FlyGates.Application.Entities.Sdks;

public sealed record SdkReleaseResponseDto(
    string App,
    string Version,
    string FileName,
    long SizeBytes,
    string? Sha256,
    DateTimeOffset ReleaseDate);

public sealed record SdkAppResponseDto(
    string App,
    string? LatestVersion,
    List<SdkReleaseResponseDto> Releases);

public sealed record SdkDownloadResponseDto(
    string Url,
    string FileName,
    DateTimeOffset ExpiresAt);
