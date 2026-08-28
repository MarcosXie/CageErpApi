using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Services.CageOuts.CageOutClients;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutClientController(ICageOutClientService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageOutClientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var clients = await service.GetAllAsync();
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CageOutClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var client = await service.GetByIdAsync(id);
        return Ok(client);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageOutClientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutClientDto request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CageOutClientDto request)
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
