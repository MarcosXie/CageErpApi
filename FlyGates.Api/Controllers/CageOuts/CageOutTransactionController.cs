using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.CageOuts;

[ApiController]
[Route("api/[controller]")]
public class CageOutTransactionController(ICageOutTransactionService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CageOutTransactionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await service.GetAllAsync();
        return Ok(transactions);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CageOutTransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var transaction = await service.GetByIdAsync(id);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CageOutTransactionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CageOutTransactionDto request)
    {
        try
        {
            var transaction = await service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}