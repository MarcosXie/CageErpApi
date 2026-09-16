namespace FlyGates.Application.Services.CageOuts.CageClusters;

public interface ICageClusterStatusNotifier
{
    Task NotifyClusterStatusChangedAsync(Guid clusterId, CancellationToken cancellationToken = default);
}

public sealed class NoOpCageClusterStatusNotifier : ICageClusterStatusNotifier
{
    public Task NotifyClusterStatusChangedAsync(Guid clusterId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}