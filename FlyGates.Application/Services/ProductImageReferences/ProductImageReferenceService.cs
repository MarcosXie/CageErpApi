using FlyGates.Application.Entities.ProductImageReferences;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.ProductImageReferences;

public sealed class ProductImageReferenceService(
    IProductImageReferenceStorage storage) : IProductImageReferenceService
{
    public const int MaximumUploadBytes = 10 * 1024 * 1024;
    private const string ReservedSku = ".shared-sync";
    private const string PngContentType = "image/png";
    private const string JpegContentType = "image/jpeg";
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] JpegSignature = [255, 216, 255];

    public async Task<IReadOnlyList<ProductImageReferenceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var storedObjects = await storage.ListAsync(cancellationToken).ConfigureAwait(false);
        var references = new Dictionary<(string Sku, string FileName), ProductImageReferenceResponse>();

        foreach (var storedObject in storedObjects)
        {
            if (!TryParseStorageKey(storedObject.Key, out var sku, out var fileName, out var deleted))
            {
                continue;
            }

            var key = (sku, fileName);
            if (deleted)
            {
                references[key] = new ProductImageReferenceResponse(
                    sku, fileName, true, 0, storedObject.LastModifiedUtc);
            }
            else if (!references.ContainsKey(key))
            {
                references[key] = new ProductImageReferenceResponse(
                    sku, fileName, false, storedObject.Length, storedObject.LastModifiedUtc);
            }
        }

        return references.Values
            .OrderBy(reference => reference.Sku, StringComparer.Ordinal)
            .ThenBy(reference => reference.FileName, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<byte[]?> GetImageAsync(
        string sku,
        string fileName,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(sku, fileName);
        if (await storage.IsDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false))
        {
            throw Gone();
        }

        var image = await storage.GetImageAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
        if (await storage.IsDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false))
        {
            throw Gone();
        }

        return image?.Content;
    }

    public async Task<(ProductImageReferenceResponse Reference, bool Created)> PutAsync(
        string sku,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(sku, fileName);
        var expectedContentType = GetContentType(fileName);
        if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw BadRequest("O Content-Type deve corresponder à extensão do arquivo.");
        }

        ValidateImage(content, expectedContentType);
        if (await storage.IsDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false))
        {
            throw Gone();
        }

        var existing = await storage.GetImageAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            EnsureSameContent(existing.Content, content);
            await ThrowIfDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
            return (ToResponse(sku, fileName, existing), false);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (await storage.PutImageIfAbsentAsync(
                    sku, fileName, content, expectedContentType, cancellationToken).ConfigureAwait(false))
            {
                await ThrowIfDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
                return (
                    new ProductImageReferenceResponse(sku, fileName, false, content.LongLength, DateTimeOffset.UtcNow),
                    true);
            }

            existing = await storage.GetImageAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                EnsureSameContent(existing.Content, content);
                await ThrowIfDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
                return (ToResponse(sku, fileName, existing), false);
            }
        }

        throw new BaseException("Não foi possível concluir o upload da imagem de referência.", 503);
    }

    public async Task DeleteAsync(string sku, string fileName, CancellationToken cancellationToken)
    {
        ValidateIdentity(sku, fileName);
        await storage.CreateTombstoneIfAbsentAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
        await storage.DeleteImageAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
    }

    private async Task ThrowIfDeletedAsync(string sku, string fileName, CancellationToken cancellationToken)
    {
        if (await storage.IsDeletedAsync(sku, fileName, cancellationToken).ConfigureAwait(false))
        {
            await storage.DeleteImageAsync(sku, fileName, cancellationToken).ConfigureAwait(false);
            throw Gone();
        }
    }

    private static ProductImageReferenceResponse ToResponse(
        string sku,
        string fileName,
        ProductImageReferenceStorageImage image) =>
        new(sku, fileName, false, image.Content.LongLength, image.LastModifiedUtc);

    private static void EnsureSameContent(byte[] existing, byte[] requested)
    {
        if (!existing.AsSpan().SequenceEqual(requested))
        {
            throw new BaseException("Já existe uma imagem diferente com esse SKU e nome de arquivo.", 409);
        }
    }

    private static void ValidateIdentity(string sku, string fileName)
    {
        if (!IsValidName(sku, 128)
            || IsReservedSku(sku)
            || !IsValidName(fileName, 180)
            || !(fileName.EndsWith(".foreground.png", StringComparison.OrdinalIgnoreCase)
                 || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
        {
            throw BadRequest("SKU ou nome de arquivo inválido.");
        }
    }

    private static bool IsValidName(string value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value)
            || value.Length > maximumLength
            || value is "." or ".."
            || value.EndsWith('.') || value.EndsWith(' '))
        {
            return false;
        }

        return value.All(character =>
            character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '_' or '.' or '-');
    }

    private static string GetContentType(string fileName) =>
        fileName.EndsWith(".foreground.png", StringComparison.OrdinalIgnoreCase)
            ? PngContentType
            : JpegContentType;

    private static void ValidateImage(byte[] content, string contentType)
    {
        if (content.Length == 0)
        {
            throw BadRequest("O arquivo de imagem está vazio.");
        }

        if (content.Length > MaximumUploadBytes)
        {
            throw new BaseException("O arquivo excede o limite de 10 MiB.", 413);
        }

        var matchesSignature = contentType == PngContentType
            ? content.AsSpan().StartsWith(PngSignature)
            : content.AsSpan().StartsWith(JpegSignature);
        if (!matchesSignature)
        {
            throw BadRequest("A assinatura do arquivo não corresponde ao formato declarado.");
        }
    }

    private static bool TryParseStorageKey(
        string key,
        out string sku,
        out string fileName,
        out bool deleted)
    {
        sku = string.Empty;
        fileName = string.Empty;
        deleted = false;
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || (segments[0] != "images" && segments[0] != "tombstones"))
        {
            return false;
        }

        sku = segments[1];
        fileName = segments[2];
        deleted = segments[0] == "tombstones";
        return IsValidName(sku, 128)
            && !IsReservedSku(sku)
            && IsValidName(fileName, 180)
            && (fileName.EndsWith(".foreground.png", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
    }

    private static BaseException BadRequest(string message) => new(message, 400);
    private static BaseException Gone() => new("A imagem de referência foi removida e não pode ser restaurada.", 410);
    private static bool IsReservedSku(string sku) =>
        string.Equals(sku, ReservedSku, StringComparison.OrdinalIgnoreCase);
}
