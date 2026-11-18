namespace FlyGates.Application.Entities.Storage;

public sealed record ProductImageReferenceStorageObject(
    string Key,
    long Length,
    DateTimeOffset LastModifiedUtc);

public sealed record ProductImageReferenceStorageImage(
    byte[] Content,
    DateTimeOffset LastModifiedUtc);

public interface IProductImageReferenceStorage
{
    Task<IReadOnlyList<ProductImageReferenceStorageObject>> ListAsync(CancellationToken cancellationToken);

    Task<ProductImageReferenceStorageImage?> GetImageAsync(
        string sku,
        string fileName,
        CancellationToken cancellationToken);

    Task<bool> IsDeletedAsync(string sku, string fileName, CancellationToken cancellationToken);

    Task<bool> PutImageIfAbsentAsync(
        string sku,
        string fileName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task CreateTombstoneIfAbsentAsync(string sku, string fileName, CancellationToken cancellationToken);

    Task DeleteImageAsync(string sku, string fileName, CancellationToken cancellationToken);
}
