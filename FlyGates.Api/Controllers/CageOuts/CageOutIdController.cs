using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Application.Services.CageOuts.CageOutIds;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutIdController(ICageOutIdService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create(CageOutIdDto request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CageOutIdDto request)
    {
        await service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Chamado periodicamente pelo terminal CageOuts para sinalizar que está online.</summary>
    [HttpPost("{identifier}/heartbeat")]
    public async Task<IActionResult> Heartbeat(string identifier)
    {
        await service.HeartbeatAsync(identifier);
        return NoContent();
    }

    /// <summary>Vincula permanentemente este Cage ID ao terminal que fez a requisição.</summary>
    [HttpPost("{id:guid}/bind")]
    public async Task<IActionResult> Bind(Guid id)
    {
        await service.BindAsync(id);
        return NoContent();
    }

    /// <summary>Libera o v\u00ednculo, tornando o Cage ID dispon\u00edvel novamente.</summary>
    [HttpPost("{id:guid}/unbind")]
    public async Task<IActionResult> Unbind(Guid id)
    {
        await service.UnbindAsync(id);
        return NoContent();
    }
}