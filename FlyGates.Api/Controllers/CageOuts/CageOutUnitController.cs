using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Application.Services.CageOuts.CageOutUnits;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutUnitController(ICageOutUnitService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageOutUnitResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var units = await service.GetAllAsync();
        return Ok(units);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CageOutUnitResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var unit = await service.GetByIdAsync(id);
        return Ok(unit);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageOutUnitResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutUnitDto request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CageOutUnitDto request)
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
