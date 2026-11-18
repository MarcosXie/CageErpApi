using FlyGates.Application.Entities.ProductImageReferences;
using FlyGates.Application.Exceptions;
using FlyGates.Application.Services.ProductImageReferences;
using FlyGates.Filters;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

/// <summary>Shares immutable product reference images and their permanent deletion markers across checkouts.</summary>
[ApiController]
[Route("api/cageouts/product-image-references")]
[TypeFilter(typeof(ProductImageReferenceAuthorizationFilter))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class ProductImageReferencesController(IProductImageReferenceService service) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [EndpointSummary("Lists the complete shared product image snapshot.")]
    [EndpointDescription("Returns active image references and permanent tombstones. Storage failures are reported, never as an empty list.")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageReferenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<ProductImageReferenceResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var references = await service.GetAllAsync(cancellationToken);
        return Ok(references);
    }

    [HttpGet("{sku}/{fileName}")]
    [Produces("image/png", "image/jpeg")]
    [EndpointSummary("Downloads a shared product reference image.")]
    [EndpointDescription("Returns the original PNG or JPEG bytes; deleted references return 410 and missing references return 404.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetImage(
        [FromRoute] string sku,
        [FromRoute] string fileName,
        CancellationToken cancellationToken)
    {
        var content = await service.GetImageAsync(sku, fileName, cancellationToken);
        if (content is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        var contentType = fileName.EndsWith(".foreground.png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";
        return File(content, contentType);
    }

    [HttpPut("{sku}/{fileName}")]
    [Consumes("image/png", "image/jpeg")]
    [Produces("application/json")]
    [EndpointSummary("Creates an immutable shared product reference image.")]
    [EndpointDescription("Accepts raw PNG or JPEG bytes up to 10 MiB. Identical retries succeed; a different object at the same identity returns 409.")]
    [ProducesResponseType(typeof(ProductImageReferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProductImageReferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PutImage(
        [FromRoute] string sku,
        [FromRoute] string fileName,
        CancellationToken cancellationToken)
    {
        var contentType = Request.ContentType?.Split(';', 2)[0].Trim();
        if (contentType is null
            || (!string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            throw new BaseException("Use Content-Type image/png ou image/jpeg.", StatusCodes.Status415UnsupportedMediaType);
        }

        if (Request.ContentLength is > ProductImageReferenceService.MaximumUploadBytes)
        {
            throw new BaseException("O arquivo excede o limite de 10 MiB.", StatusCodes.Status413PayloadTooLarge);
        }

        var content = await ReadUploadAsync(cancellationToken);
        var (reference, created) = await service.PutAsync(sku, fileName, contentType, content, cancellationToken);
        if (!created)
        {
            return Ok(reference);
        }

        return CreatedAtAction(nameof(GetImage), new { sku, fileName }, reference);
    }

    [HttpDelete("{sku}/{fileName}")]
    [EndpointSummary("Permanently deletes a shared product reference image.")]
    [EndpointDescription("Creates a durable tombstone before removing the image. The tombstone remains in every checkout snapshot.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteImage(
        [FromRoute] string sku,
        [FromRoute] string fileName,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(sku, fileName, cancellationToken);
        return NoContent();
    }

    private async Task<byte[]> ReadUploadAsync(CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        var total = 0;
        int read;
        while ((read = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > ProductImageReferenceService.MaximumUploadBytes)
            {
                throw new BaseException("O arquivo excede o limite de 10 MiB.", StatusCodes.Status413PayloadTooLarge);
            }

            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return content.ToArray();
    }
}
