using FlyGates.Application.Entities.Storage;
using FlyGates.Infraestructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlyGates.Infraestructure.Services;

/// <summary>Mantém a lifecycle rule de vídeo do S3 sincronizada com MediaRetentionOptions no startup e a cada 24h.</summary>
public class S3LifecycleSyncHostedService(
    IMediaStorageService mediaStorage,
    IOptionsMonitor<MediaRetentionOptions> retentionOptions,
    ILogger<S3LifecycleSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await mediaStorage
                    .SyncVideoLifecycleRuleAsync(retentionOptions.CurrentValue.VideoRetentionDays, stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao sincronizar retenção de vídeo do S3.");
            }

            try
            {
                await Task.Delay(SyncInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
