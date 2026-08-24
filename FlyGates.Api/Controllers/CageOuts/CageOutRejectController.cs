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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutRejectDto request)
    {
        await service.CreateAsync(request);
        return Created(string.Empty, null);
    }
}
