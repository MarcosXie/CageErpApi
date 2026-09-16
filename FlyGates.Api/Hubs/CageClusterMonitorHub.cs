using FlyGates.Application.Entities.CageOuts.CageClusters;
using Microsoft.AspNetCore.SignalR;

namespace FlyGates.Api.Hubs;

public interface ICageClusterMonitorClient
{
    Task ClusterStatusUpdated(CageClusterStatusSnapshotDto snapshot);
}

public class CageClusterMonitorHub : Hub<ICageClusterMonitorClient>
{
    public Task SubscribeCluster(Guid clusterId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(clusterId));
    }

    public Task UnsubscribeCluster(Guid clusterId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(clusterId));
    }

    public static string GetGroupName(Guid clusterId)
    {
        return $"cluster:{clusterId:D}";
    }
}