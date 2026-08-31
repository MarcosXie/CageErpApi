using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutEmployeeController(ICageOutEmployeeService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageOutEmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var employees = await service.GetAllAsync();
        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CageOutEmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var employee = await service.GetByIdAsync(id);
        return Ok(employee);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageOutEmployeeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutEmployeeDto request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CageOutEmployeeUpdateDto request)
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

    [HttpPost("authenticate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Authenticate([FromBody] CageOutEmployeeAuthDto request)
    {
        var result = await service.AuthenticateAsync(request);
        if (result is null)
            return NotFound();

        return Ok(new { id = result.Id, allowedProcedures = result.AllowedProcedures });
    }
}
