using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutRejectController(ICageOutRejectService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageOutRejectResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var rejects = await service.GetAllAsync();
        return Ok(rejects);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageOutRejectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutRejectDto request)
    {
        var created = await service.CreateAsync(request);
        return Created(string.Empty, created);
    }

    [HttpPatch("{id:guid}/resolve")]
    [ProducesResponseType(typeof(CageOutRejectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var updated = await service.ResolveAsync(id);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/video")]
    [ProducesResponseType(typeof(CageOutRejectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVideo(Guid id, [FromBody] UpdateCageOutRejectVideoDto request)
    {
        var updated = await service.UpdateVideoAsync(id, request.ProductVideo);
        return Ok(updated);
    }
}
