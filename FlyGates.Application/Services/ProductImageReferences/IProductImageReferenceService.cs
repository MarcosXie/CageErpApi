using FlyGates.Application.Entities.ProductImageReferences;

namespace FlyGates.Application.Services.ProductImageReferences;

public interface IProductImageReferenceService
{
    Task<IReadOnlyList<ProductImageReferenceResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<byte[]?> GetImageAsync(
        string sku,
        string fileName,
        CancellationToken cancellationToken);

    Task<(ProductImageReferenceResponse Reference, bool Created)> PutAsync(
        string sku,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken);

    Task DeleteAsync(string sku, string fileName, CancellationToken cancellationToken);
}
