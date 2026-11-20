using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Services.Halloween;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/halloween/sessions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HalloweenController(IHalloweenSessionService service) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Creates a Halloween room with four predefined players.")]
    [ProducesResponseType(typeof(HalloweenSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<HalloweenSessionResponse>> Create(CancellationToken cancellationToken)
    {
        var session = await service.CreateAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, session);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Reads the shared scoreboard using its private room identifier.")]
    [ProducesResponseType(typeof(HalloweenSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalloweenSessionResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/scores")]
    [EndpointSummary("Atomically adjusts up to four scores; repeated operation IDs are not reapplied.")]
    [ProducesResponseType(typeof(HalloweenSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HalloweenSessionResponse>> UpdateScores(
        Guid id, UpdateHalloweenScoresRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateScoresAsync(id, request, cancellationToken));
}
