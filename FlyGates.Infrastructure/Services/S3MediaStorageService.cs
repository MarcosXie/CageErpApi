using Amazon.S3;
using Amazon.S3.Model;
using FlyGates.Application.Entities.Storage;
using FlyGates.Infraestructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlyGates.Infraestructure.Services;

public class S3MediaStorageService(
    IAmazonS3 s3Client,
    IOptionsMonitor<AwsS3Options> options,
    ILogger<S3MediaStorageService> logger) : IMediaStorageService
{
    public string? GeneratePresignedUrl(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var current = options.CurrentValue;
        try
        {
            return s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = current.BucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(Math.Max(1, current.PresignedUrlExpirationMinutes)),
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao gerar URL pré-assinada para a chave {Key}.", objectKey);
            return null;
        }
    }

    public async Task DeleteObjectAsync(string? objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        var current = options.CurrentValue;
        try
        {
            await s3Client.DeleteObjectAsync(current.BucketName, objectKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao apagar a chave {Key} do bucket S3.", objectKey);
        }
    }

    public async Task SyncVideoLifecycleRuleAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var current = options.CurrentValue;
        var days = Math.Max(1, retentionDays);

        var request = new PutLifecycleConfigurationRequest
        {
            BucketName = current.BucketName,
            Configuration = new LifecycleConfiguration
            {
                Rules =
                [
                    new LifecycleRule
                    {
                        Id = "cageouts-video-retention",
                        Filter = new LifecycleFilter
                        {
                            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = current.VideoPrefix },
                        },
                        Status = LifecycleRuleStatus.Enabled,
                        Expiration = new LifecycleRuleExpiration { Days = days },
                    },
                ],
            },
        };

        try
        {
            await s3Client.PutLifecycleConfigurationAsync(request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Lifecycle rule de retenção de vídeo sincronizada: {Days} dia(s) no prefixo {Prefix}.",
                days, current.VideoPrefix);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao sincronizar a lifecycle rule de retenção do bucket S3.");
        }
    }
}
