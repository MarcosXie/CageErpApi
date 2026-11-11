using System.Text.Json;
using FlyGates.Application.Entities.Sdks;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.Sdks;

public interface ISdkReleaseService
{
    Task<List<SdkAppResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SdkDownloadResponseDto> GetDownloadAsync(string app, string version, CancellationToken cancellationToken = default);
}

public class SdkReleaseService(IMediaStorageService storage) : ISdkReleaseService
{
    /// <summary>Aplicativos sempre exibidos na listagem, mesmo sem versões publicadas.</summary>
    public static readonly IReadOnlyList<string> KnownApps = ["CageOuts", "CageCluster"];

    private static readonly TimeSpan DownloadUrlExpiration = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<SdkAppResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await ReadManifestAsync(cancellationToken);

        var apps = KnownApps
            .Concat(manifest.Releases.Select(r => r.App))
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return apps.Select(app =>
        {
            var releases = manifest.Releases
                .Where(r => string.Equals(r.App, app, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => ParseVersion(r.Version))
                .ThenByDescending(r => r.ReleaseDate)
                .Select(r => new SdkReleaseResponseDto(app, r.Version, r.FileName, r.SizeBytes, r.Sha256, r.ReleaseDate))
                .ToList();

            return new SdkAppResponseDto(app, releases.FirstOrDefault()?.Version, releases);
        }).ToList();
    }

    public async Task<SdkDownloadResponseDto> GetDownloadAsync(string app, string version, CancellationToken cancellationToken = default)
    {
        var manifest = await ReadManifestAsync(cancellationToken);

        var entry = manifest.Releases.FirstOrDefault(r =>
            string.Equals(r.App, app, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Version, version, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException($"Instalador {app} {version}");

        var url = storage.GeneratePresignedDownloadUrl(entry.ObjectKey, entry.FileName, DownloadUrlExpiration)
            ?? throw new BaseException("Não foi possível gerar o link de download.", 503);

        return new SdkDownloadResponseDto(url, entry.FileName, DateTimeOffset.UtcNow.Add(DownloadUrlExpiration));
    }

    private async Task<SdkManifest> ReadManifestAsync(CancellationToken cancellationToken)
    {
        var json = await storage.ReadObjectTextAsync(SdkManifest.ObjectKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SdkManifest();
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<SdkManifest>(json, JsonOptions) ?? new SdkManifest();
            manifest.Releases = manifest.Releases
                .Where(r => !string.IsNullOrWhiteSpace(r.App) && !string.IsNullOrWhiteSpace(r.Version) && !string.IsNullOrWhiteSpace(r.ObjectKey))
                .ToList();
            return manifest;
        }
        catch (JsonException)
        {
            throw new BaseException("Manifesto de instaladores inválido.", 503);
        }
    }

    private static Version ParseVersion(string value) =>
        System.Version.TryParse(value, out var parsed) ? parsed : new Version(0, 0);
}
