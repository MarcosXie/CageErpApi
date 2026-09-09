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
}