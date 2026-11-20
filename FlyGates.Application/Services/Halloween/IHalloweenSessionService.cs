using FlyGates.Application.Entities.Halloween;

namespace FlyGates.Application.Services.Halloween;

public interface IHalloweenSessionService
{
    Task<HalloweenSessionResponse> CreateAsync(CancellationToken cancellationToken);
    Task<HalloweenSessionResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<HalloweenSessionResponse> UpdateScoresAsync(
        Guid id, UpdateHalloweenScoresRequest request, CancellationToken cancellationToken);
    Task<HalloweenSessionResponse> ExecuteAsync(
        Guid id, HalloweenActionRequest request, CancellationToken cancellationToken);
}
