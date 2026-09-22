using System.Collections.Concurrent;
using FlyGates.Application.Entities.CageOuts.CageOutIds;

namespace FlyGates.Application.Services.CageOuts.CageOutIds;

public interface ICageOutLiveSessionStore
{
    CageOutLiveSessionResponseDto Upsert(
        CageOutId cageOut,
        CageOutLiveSessionHeartbeatDto session,
        DateTime generatedAt,
        bool isOnline);

    CageOutLiveSessionResponseDto? Get(Guid cageOutId, TimeSpan maxAge, DateTime now);
}

public interface ICageOutLiveSessionNotifier
{
    Task NotifySessionUpdatedAsync(CageOutLiveSessionResponseDto snapshot, CancellationToken cancellationToken = default);
}

public sealed class NoOpCageOutLiveSessionNotifier : ICageOutLiveSessionNotifier
{
    public Task NotifySessionUpdatedAsync(CageOutLiveSessionResponseDto snapshot, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCageOutLiveSessionStore : ICageOutLiveSessionStore
{
    private readonly ConcurrentDictionary<Guid, CageOutLiveSessionResponseDto> _sessions = new();

    public CageOutLiveSessionResponseDto Upsert(
        CageOutId cageOut,
        CageOutLiveSessionHeartbeatDto session,
        DateTime generatedAt,
        bool isOnline)
    {
        var snapshot = BuildSnapshot(cageOut, session, generatedAt, isOnline);
        _sessions[cageOut.Id] = snapshot;
        return snapshot;
    }

    public CageOutLiveSessionResponseDto? Get(Guid cageOutId, TimeSpan maxAge, DateTime now)
    {
        if (!_sessions.TryGetValue(cageOutId, out var snapshot))
        {
            return null;
        }

        if (now - snapshot.GeneratedAt > maxAge)
        {
            _sessions.TryRemove(cageOutId, out _);
            return null;
        }

        return snapshot;
    }

    private static CageOutLiveSessionResponseDto BuildSnapshot(
        CageOutId cageOut,
        CageOutLiveSessionHeartbeatDto session,
        DateTime generatedAt,
        bool isOnline)
    {
        return new CageOutLiveSessionResponseDto
        {
            CageOutId = cageOut.Id,
            UnitId = cageOut.UnitId,
            Identifier = cageOut.Identifier,
            IsOnline = isOnline,
            OperationalStatus = cageOut.OperationalStatus,
            CurrentMode = cageOut.CurrentMode,
            LastSeenAt = cageOut.LastSeenAt,
            GeneratedAt = generatedAt,
            Session = new CageOutLiveSessionPayloadResponseDto
            {
                IsActive = session.IsActive,
                CheckoutId = session.CheckoutId,
                CurrentTotalAmount = session.CurrentTotalAmount,
                ScannedCount = session.ScannedCount,
                ApprovedCount = session.ApprovedCount,
                SessionStartedAt = session.SessionStartedAt,
                LastUpdatedAt = session.LastUpdatedAt,
                PhotoSnapshotBase64 = session.PhotoSnapshotBase64,
                VideoSnapshotBase64 = session.VideoSnapshotBase64,
                Items = session.Items.Select(item => new CageOutLiveSessionItemResponseDto
                {
                    ItemId = item.ItemId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    MatchStatus = item.MatchStatus,
                    ExpectedWeightKg = item.ExpectedWeightKg,
                    RealWeightKg = item.RealWeightKg,
                    UnitPrice = item.UnitPrice,
                    ScannedAt = item.ScannedAt,
                }).ToList(),
            },
        };
    }
}
