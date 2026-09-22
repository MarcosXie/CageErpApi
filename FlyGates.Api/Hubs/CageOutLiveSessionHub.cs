using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Application.Services.CageOuts.CageOutIds;
using Microsoft.AspNetCore.SignalR;

namespace FlyGates.Api.Hubs;

public interface ICageOutLiveSessionClient
{
    Task SessionUpdated(CageOutLiveSessionResponseDto snapshot);
}

public sealed class CageOutLiveSessionHub : Hub<ICageOutLiveSessionClient>
{
    public Task SubscribeCageOut(Guid cageOutId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(cageOutId));
    }

    public Task UnsubscribeCageOut(Guid cageOutId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(cageOutId));
    }

    public static string GetGroupName(Guid cageOutId)
    {
        return $"cageout:{cageOutId:D}";
    }
}

public sealed class CageOutLiveSessionNotifier(
    IHubContext<CageOutLiveSessionHub, ICageOutLiveSessionClient> hubContext,
    ILogger<CageOutLiveSessionNotifier> logger) : ICageOutLiveSessionNotifier
{
    public async Task NotifySessionUpdatedAsync(CageOutLiveSessionResponseDto snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext
                .Clients
                .Group(CageOutLiveSessionHub.GetGroupName(snapshot.CageOutId))
                .SessionUpdated(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao notificar sessão ao vivo do CageOut {CageOutId}.", snapshot.CageOutId);
        }
    }
}
