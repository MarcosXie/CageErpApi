namespace FlyGates.Application.Entities.ProductImageReferences;

/// <summary>Metadata for a shared product reference image, including permanent deletion markers.</summary>
public sealed record ProductImageReferenceResponse(
    string Sku,
    string FileName,
    bool Deleted,
    long Length,
    DateTimeOffset LastModifiedUtc);
