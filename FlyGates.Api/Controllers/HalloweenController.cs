using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Services.Halloween;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FlyGates.Api.Hubs;

namespace FlyGates.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/halloween/sessions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HalloweenController(
    IHalloweenSessionService service, IHubContext<HalloweenHub> hub, TimeProvider clock) : ControllerBase
{
    [HttpGet("/api/halloween/clock")]
    [EndpointSummary("Returns a UTC clock sample for simultaneous starts.")]
    [ProducesResponseType(typeof(HalloweenClockResponse), StatusCodes.Status200OK)]
    public ActionResult<HalloweenClockResponse> GetClock(CancellationToken cancellationToken) =>
        Ok(new HalloweenClockResponse(clock.GetUtcNow()));

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
        Ok(await PublishAsync(await service.UpdateScoresAsync(id, request, cancellationToken), cancellationToken));

    [HttpPost("{id:guid}/actions")]
    [EndpointSummary("Runs an idempotent game action: timer, drink votes, results, Instagram or survival.")]
    [ProducesResponseType(typeof(HalloweenSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HalloweenSessionResponse>> Execute(
        Guid id, HalloweenActionRequest request, CancellationToken cancellationToken) =>
        Ok(await PublishAsync(await service.ExecuteAsync(id, request, cancellationToken), cancellationToken));

    private async Task<HalloweenSessionResponse> PublishAsync(
        HalloweenSessionResponse session, CancellationToken cancellationToken)
    {
        await hub.Clients.Group(HalloweenHub.GroupName(session.Id))
            .SendAsync("RoomUpdated", session, cancellationToken);
        return session;
    }
}
