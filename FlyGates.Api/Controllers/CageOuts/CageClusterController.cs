using FlyGates.Application.Entities.CageOuts.CageClusters;
using FlyGates.Application.Services.CageOuts.CageClusters;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageClusterController(ICageClusterService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageClusterResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var clusters = await service.GetAllAsync();
        return Ok(clusters);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CageClusterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var cluster = await service.GetByIdAsync(id);
        return Ok(cluster);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(CageClusterStatusSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus([FromRoute] Guid id)
    {
        var snapshot = await service.GetStatusSnapshotAsync(id);
        return Ok(snapshot);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageClusterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageClusterDto request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CageClusterDto request)
    {
        await service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}