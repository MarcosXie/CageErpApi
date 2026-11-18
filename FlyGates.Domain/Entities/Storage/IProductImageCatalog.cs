namespace FlyGates.Application.Entities.Storage;

public interface IProductImageCatalog
{
    Task RecordAsync(string sku, string fileName, bool deleted, long length, DateTimeOffset modified, CancellationToken ct);
    Task ReplaceAssortmentAsync(int store, IReadOnlyCollection<string> skus, CancellationToken ct);
    Task<ProductImageCatalogPage?> ReadAsync(int store, long after, int limit, CancellationToken ct);
}

/// <summary>Latest state of a reference, ordered by its committed server revision.</summary>
public sealed record ProductImageCatalogEntry(long Revision, string Sku, string FileName, bool Deleted, long Length,
    DateTimeOffset LastModifiedUtc);

/// <summary>A bounded page of reference changes for a single assortment generation.</summary>
public sealed record ProductImageCatalogPage(Guid Generation, IReadOnlyList<ProductImageCatalogEntry> Items, bool HasMore, long NextAfter);
