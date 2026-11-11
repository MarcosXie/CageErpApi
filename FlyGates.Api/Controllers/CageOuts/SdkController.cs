using FlyGates.Application.Entities.Sdks;
using FlyGates.Application.Services.Sdks;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

/// <summary>Lista e disponibiliza para download os instaladores (CageOuts, CageCluster) publicados no S3.</summary>
[ApiController]
[Route("api/[controller]")]
public class SdkController(ISdkReleaseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<SdkAppResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var apps = await service.GetAllAsync(cancellationToken);
        return Ok(apps);
    }

    [HttpGet("{app}/{version}/download")]
    [ProducesResponseType(typeof(SdkDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownload([FromRoute] string app, [FromRoute] string version, CancellationToken cancellationToken)
    {
        var download = await service.GetDownloadAsync(app, version, cancellationToken);
        return Ok(download);
    }
}
