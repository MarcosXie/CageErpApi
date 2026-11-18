using Amazon.S3;
using Amazon.S3.Model;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;
using FlyGates.Infraestructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace FlyGates.Infraestructure.Services.ProductImageReferences;

public sealed class S3ProductImageReferenceStorage(
    IAmazonS3 s3Client,
    IOptionsMonitor<AwsS3Options> options,
    ILogger<S3ProductImageReferenceStorage> logger) : IProductImageReferenceStorage
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    public Task<IReadOnlyList<ProductImageReferenceStorageObject>> ListAsync(CancellationToken cancellationToken) =>
        ExecuteAsync("list", async token =>
        {
            var current = GetCurrentOptions();
            var prefix = GetPrefix(current);
            var objects = new List<ProductImageReferenceStorageObject>();
            string? continuationToken = null;

            do
            {
                var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = current.BucketName,
                    Prefix = prefix,
                    ContinuationToken = continuationToken,
                }, token).ConfigureAwait(false);

                objects.AddRange(response.S3Objects.Select(item => new ProductImageReferenceStorageObject(
                    item.Key[prefix.Length..],
                    item.Size ?? 0,
                    AsUtcOffset(item.LastModified))));

                var isTruncated = response.IsTruncated ?? false;
                continuationToken = isTruncated ? response.NextContinuationToken : null;
                if (isTruncated && string.IsNullOrEmpty(continuationToken))
                {
                    throw new InvalidOperationException("The S3 listing did not provide a continuation token.");
                }
            }
            while (continuationToken is not null);

            return (IReadOnlyList<ProductImageReferenceStorageObject>)objects;
        }, cancellationToken);

    public async Task<ProductImageReferenceStorageImage?> GetImageAsync(
        string sku,
        string fileName,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync<ProductImageReferenceStorageImage?>("read", async token =>
        {
            var current = GetCurrentOptions();
            try
            {
                using var response = await s3Client.GetObjectAsync(
                    current.BucketName, ImageKey(current, sku, fileName), token).ConfigureAwait(false);
                using var content = new MemoryStream();
                await response.ResponseStream.CopyToAsync(content, token).ConfigureAwait(false);
                return new ProductImageReferenceStorageImage(content.ToArray(), AsUtcOffset(response.LastModified));
            }
            catch (AmazonS3Exception exception) when (IsMissing(exception))
            {
                return null;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> IsDeletedAsync(string sku, string fileName, CancellationToken cancellationToken) =>
        ExecuteAsync("check tombstone", async token =>
        {
            var current = GetCurrentOptions();
            try
            {
                await s3Client.GetObjectMetadataAsync(
                    current.BucketName, TombstoneKey(current, sku, fileName), token).ConfigureAwait(false);
                return true;
            }
            catch (AmazonS3Exception exception) when (IsMissing(exception))
            {
                return false;
            }
        }, cancellationToken);

    public Task<bool> PutImageIfAbsentAsync(
        string sku,
        string fileName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken) =>
        ExecuteAsync("write image", async token =>
        {
            var current = GetCurrentOptions();
            using var stream = new MemoryStream(content, writable: false);
            try
            {
                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = current.BucketName,
                    Key = ImageKey(current, sku, fileName),
                    InputStream = stream,
                    ContentType = contentType,
                    AutoCloseStream = false,
                    IfNoneMatch = "*",
                }, token).ConfigureAwait(false);
                return true;
            }
            catch (AmazonS3Exception exception) when (IsConditionalConflict(exception))
            {
                return false;
            }
        }, cancellationToken);

    public Task CreateTombstoneIfAbsentAsync(
        string sku,
        string fileName,
        CancellationToken cancellationToken) =>
        ExecuteAsync("write tombstone", async token =>
        {
            var current = GetCurrentOptions();
            for (var attempt = 0; attempt < 3; attempt++)
            {
                using var emptyContent = new MemoryStream(Array.Empty<byte>(), writable: false);
                try
                {
                    await s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = current.BucketName,
                        Key = TombstoneKey(current, sku, fileName),
                        InputStream = emptyContent,
                        ContentType = "application/octet-stream",
                        AutoCloseStream = false,
                        IfNoneMatch = "*",
                    }, token).ConfigureAwait(false);
                    return;
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    return;
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.Conflict)
                {
                    try
                    {
                        await s3Client.GetObjectMetadataAsync(
                            current.BucketName, TombstoneKey(current, sku, fileName), token).ConfigureAwait(false);
                        return;
                    }
                    catch (AmazonS3Exception missing) when (IsMissing(missing) && attempt < 2)
                    {
                    }
                }
            }

            throw new InvalidOperationException("Could not confirm the product image reference tombstone.");
        }, cancellationToken);

    public async Task DeleteImageAsync(string sku, string fileName, CancellationToken cancellationToken)
    {
        await ExecuteAsync("delete image", async token =>
        {
            var current = GetCurrentOptions();
            await s3Client.DeleteObjectAsync(
                current.BucketName, ImageKey(current, sku, fileName), token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteAsync(
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync<object?>(operation, async token =>
        {
            await action(token).ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OperationTimeout);
        try
        {
            return await action(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out during S3 product image reference operation {Operation}.", operation);
            throw new BaseException("O armazenamento de imagens de referência está indisponível no momento.", 503);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "S3 product image reference operation {Operation} failed.", operation);
            throw new BaseException("O armazenamento de imagens de referência está indisponível no momento.", 503);
        }
    }

    private AwsS3Options GetCurrentOptions()
    {
        var current = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(current.BucketName))
        {
            throw new BaseException("O armazenamento de imagens de referência não está configurado.", 503);
        }

        return current;
    }

    private static string GetPrefix(AwsS3Options current)
    {
        var prefix = (current.ProductImageReferencePrefix ?? string.Empty).TrimStart('/');
        if (prefix.Length == 0)
        {
            prefix = "product-image-references/";
        }

        prefix = prefix.EndsWith('/') ? prefix : $"{prefix}/";
        var videoPrefix = (current.VideoPrefix ?? string.Empty).TrimStart('/');
        if (videoPrefix.Length > 0 && !videoPrefix.EndsWith('/'))
        {
            videoPrefix += "/";
        }

        if (videoPrefix.Length > 0
            && (prefix.StartsWith(videoPrefix, StringComparison.Ordinal)
                || videoPrefix.StartsWith(prefix, StringComparison.Ordinal)))
        {
            throw new BaseException(
                "O prefixo de imagens de referência deve ser isolado do prefixo de vídeos.", 503);
        }

        return prefix;
    }

    private static string ImageKey(AwsS3Options current, string sku, string fileName) =>
        $"{GetPrefix(current)}images/{sku}/{fileName}";

    private static string TombstoneKey(AwsS3Options current, string sku, string fileName) =>
        $"{GetPrefix(current)}tombstones/{sku}/{fileName}";

    private static DateTimeOffset AsUtcOffset(DateTime? value) =>
        new(DateTime.SpecifyKind(value ?? DateTime.UtcNow, DateTimeKind.Utc));

    private static bool IsMissing(AmazonS3Exception exception) =>
        exception.StatusCode == HttpStatusCode.NotFound
        || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.Ordinal)
        || string.Equals(exception.ErrorCode, "NotFound", StringComparison.Ordinal);

    private static bool IsConditionalConflict(AmazonS3Exception exception) =>
        exception.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict
        || string.Equals(exception.ErrorCode, "ConditionalRequestConflict", StringComparison.Ordinal);
}
