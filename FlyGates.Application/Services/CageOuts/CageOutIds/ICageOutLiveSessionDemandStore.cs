namespace FlyGates.Application.Services.CageOuts.CageOutIds;

public interface ICageOutLiveSessionDemandStore
{
    void Subscribe(Guid cageOutId, string connectionId);
    void Unsubscribe(Guid cageOutId, string connectionId);
    void UnsubscribeAll(string connectionId);
    void TouchHttpViewer(Guid cageOutId, DateTime now, TimeSpan ttl);
    int GetActiveViewerCount(Guid cageOutId, DateTime now);
}

public sealed class InMemoryCageOutLiveSessionDemandStore : ICageOutLiveSessionDemandStore
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, HashSet<string>> _signalrByCageOut = new();
    private readonly Dictionary<string, HashSet<Guid>> _cageOutsByConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, DateTime> _httpViewerLeaseUntil = new();

    public void Subscribe(Guid cageOutId, string connectionId)
    {
        if (cageOutId == Guid.Empty || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        lock (_sync)
        {
            if (!_signalrByCageOut.TryGetValue(cageOutId, out var connections))
            {
                connections = new HashSet<string>(StringComparer.Ordinal);
                _signalrByCageOut[cageOutId] = connections;
            }
            connections.Add(connectionId);

            if (!_cageOutsByConnection.TryGetValue(connectionId, out var cageOuts))
            {
                cageOuts = [];
                _cageOutsByConnection[connectionId] = cageOuts;
            }
            cageOuts.Add(cageOutId);
        }
    }

    public void Unsubscribe(Guid cageOutId, string connectionId)
    {
        if (cageOutId == Guid.Empty || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        lock (_sync)
        {
            RemoveSignalRSubscriptionUnsafe(cageOutId, connectionId);
        }
    }

    public void UnsubscribeAll(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        lock (_sync)
        {
            if (!_cageOutsByConnection.TryGetValue(connectionId, out var cageOutIds))
            {
                return;
            }

            foreach (var cageOutId in cageOutIds.ToList())
            {
                RemoveSignalRSubscriptionUnsafe(cageOutId, connectionId);
            }

            _cageOutsByConnection.Remove(connectionId);
        }
    }

    public void TouchHttpViewer(Guid cageOutId, DateTime now, TimeSpan ttl)
    {
        if (cageOutId == Guid.Empty || ttl <= TimeSpan.Zero)
        {
            return;
        }

        lock (_sync)
        {
            CleanupExpiredHttpLeasesUnsafe(now);
            _httpViewerLeaseUntil[cageOutId] = now.Add(ttl);
        }
    }

    public int GetActiveViewerCount(Guid cageOutId, DateTime now)
    {
        if (cageOutId == Guid.Empty)
        {
            return 0;
        }

        lock (_sync)
        {
            CleanupExpiredHttpLeasesUnsafe(now);
            var signalrCount = _signalrByCageOut.TryGetValue(cageOutId, out var connections)
                ? connections.Count
                : 0;

            var hasHttpViewer = _httpViewerLeaseUntil.ContainsKey(cageOutId);
            if (!hasHttpViewer)
            {
                return signalrCount;
            }

            return signalrCount > 0 ? signalrCount : 1;
        }
    }

    private void CleanupExpiredHttpLeasesUnsafe(DateTime now)
    {
        foreach (var entry in _httpViewerLeaseUntil.Where(entry => entry.Value <= now).ToList())
        {
            _httpViewerLeaseUntil.Remove(entry.Key);
        }
    }

    private void RemoveSignalRSubscriptionUnsafe(Guid cageOutId, string connectionId)
    {
        if (_signalrByCageOut.TryGetValue(cageOutId, out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _signalrByCageOut.Remove(cageOutId);
            }
        }

        if (_cageOutsByConnection.TryGetValue(connectionId, out var cageOuts))
        {
            cageOuts.Remove(cageOutId);
            if (cageOuts.Count == 0)
            {
                _cageOutsByConnection.Remove(connectionId);
            }
        }
    }
}
