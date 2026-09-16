using FlyGates.Application.Services.CageOuts.CageClusters;
using FlyGates.Application.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace FlyGates.Api.Hubs;

public class CageClusterStatusNotifier(
    ICageClusterService clusterService,
    IHubContext<CageClusterMonitorHub, ICageClusterMonitorClient> hubContext,
    ILogger<CageClusterStatusNotifier> logger) : ICageClusterStatusNotifier
{
    public async Task NotifyClusterStatusChangedAsync(Guid clusterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await clusterService.GetStatusSnapshotAsync(clusterId);
            await hubContext
                .Clients
                .Group(CageClusterMonitorHub.GetGroupName(clusterId))
                .ClusterStatusUpdated(snapshot);
        }
        catch (NotFoundException)
        {
            logger.LogDebug("Cluster {ClusterId} não encontrado ao notificar monitoramento.", clusterId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao notificar atualização de monitoramento do cluster {ClusterId}.", clusterId);
        }
    }
}