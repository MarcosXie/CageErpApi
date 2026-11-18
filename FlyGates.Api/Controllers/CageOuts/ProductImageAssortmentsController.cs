using System.Security.Cryptography;
using System.Text;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Services.ProductImageReferences;
using FlyGates.Filters;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/cageouts/product-image-assortments")]
[TypeFilter(typeof(ProductImageReferenceAuthorizationFilter))]
public sealed class ProductImageAssortmentsController(
    IProductImageCatalog catalog, IProductImageReferenceService references, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{store:int}/references")]
    [EndpointSummary("Lists reference changes in the imported store assortment.")]
    [ProducesResponseType<ProductImageCatalogPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductImageCatalogPage>> Read(
        int store, CancellationToken cancellationToken, long after = 0, int limit = 250)
    {
        if (store <= 0 || after < 0 || limit is < 1 or > 1000)
            return Problem(statusCode: 400, title: "Loja, cursor ou tamanho de página inválido.");
        var page = await catalog.ReadAsync(store, after, limit, cancellationToken);
        if (page is null)
            return Problem(statusCode: 404, title: "Não há sortimento importado para esta loja.");
        return Ok(page);
    }

    [HttpPut("{store:int}")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [EndpointSummary("Replaces a store assortment with the imported list of SKUs.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Replace(int store, [FromBody] ImportProductImageAssortmentRequest request, CancellationToken cancellationToken)
    {
        if (CheckAdminKey() is { } error) return error;
        if (store <= 0 || request.Skus is null || request.Skus.Length > 100_000
            || request.Skus.Any(sku => !ProductImageReferenceService.IsValidSku(sku)))
            return Problem(statusCode: 400, title: "Informe uma loja válida e até 100 mil SKUs válidos.");

        await catalog.ReplaceAssortmentAsync(store, request.Skus.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("backfill")]
    [EndpointSummary("Indexes references and deletions already present in the legacy S3 catalog.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Backfill(CancellationToken cancellationToken)
    {
        if (CheckAdminKey() is { } error) return error;
        foreach (var image in await references.GetAllAsync(cancellationToken))
            await catalog.RecordAsync(image.Sku, image.FileName, image.Deleted, image.Length,
                image.LastModifiedUtc, cancellationToken);
        return NoContent();
    }

    private IActionResult? CheckAdminKey()
    {
        var key = configuration["AwsS3:ProductImageAssortmentAdminKey"];
        if (string.IsNullOrWhiteSpace(key))
            return Problem(statusCode: 503, title: "Importação de sortimento não configurada.");
        if (!Request.Headers.TryGetValue("X-Product-Assortment-Key", out var supplied) || supplied.Count != 1
            || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(key)),
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString()))))
            return Problem(statusCode: 401, title: "Chave de importação inválida.");
        return null;
    }
}

/// <summary>Complete list of active SKUs for one store; leading zeroes are preserved.</summary>
public sealed record ImportProductImageAssortmentRequest(string[] Skus);
