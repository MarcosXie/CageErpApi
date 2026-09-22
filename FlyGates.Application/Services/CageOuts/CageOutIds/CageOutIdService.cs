using AutoMapper;
using FlyGates.Application.Services.CageOuts.CageClusters;
using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.CageOuts.CageOutIds;

public interface ICageOutIdService
{
    Task<CageOutIdResponseDto> CreateAsync(CageOutIdDto dto);
    Task UpdateAsync(Guid id, CageOutIdDto dto);
    Task DeleteAsync(Guid id);
    Task<CageOutIdResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutIdResponseDto>> GetAllAsync();
    Task HeartbeatAsync(string identifier, CageOutIdHeartbeatDto? heartbeatDto = null);
    Task<CageOutLiveSessionResponseDto> GetLiveSessionAsync(Guid id);
    Task<CageOutLiveSessionDemandResponseDto> GetLiveSessionDemandAsync(string identifier);
    Task BindAsync(Guid id);
    Task UnbindAsync(Guid id);
}

public class CageOutIdService(
    ICageOutIdRepository repository,
    ICageOutUnitRepository unitRepository,
    ICageClusterStatusNotifier clusterStatusNotifier,
    ICageOutLiveSessionStore liveSessionStore,
    ICageOutLiveSessionDemandStore liveSessionDemandStore,
    ICageOutLiveSessionNotifier liveSessionNotifier,
    IMapper mapper) : ICageOutIdService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LiveSessionStalenessThreshold = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan HttpViewerLeaseTtl = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan HeartbeatPersistenceInterval = TimeSpan.FromSeconds(5);

    public async Task<CageOutIdResponseDto> CreateAsync(CageOutIdDto dto)
    {
        await ValidateAsync(dto);
        var entity = new CageOutId
        {
            UnitId = dto.UnitId,
            IsActive = dto.IsActive,
            Identifier = string.Empty
        };

        var id = await repository.CreateWithGeneratedIdentifierAsync(entity);
        return mapper.Map<CageOutIdResponseDto>(await repository.GetByIdAsync(id));
    }

    public async Task UpdateAsync(Guid id, CageOutIdDto dto)
    {
        await ValidateAsync(dto);
        var entity = await repository.GetByIdAsync(id);
        var previousUnitId = entity.UnitId;
        entity.UnitId = dto.UnitId;
        entity.IsActive = dto.IsActive;

        if (previousUnitId != entity.UnitId)
        {
            entity.CageClusterId = null;
            entity.ClusterBoxNumber = null;
        }

        await repository.UpdateAsync(entity);
    }

    public Task DeleteAsync(Guid id) => repository.DeleteAsync(id);

    public async Task<CageOutIdResponseDto> GetByIdAsync(Guid id) =>
        mapper.Map<CageOutIdResponseDto>(await repository.GetByIdAsync(id));

    public async Task<List<CageOutIdResponseDto>> GetAllAsync() =>
        mapper.Map<List<CageOutIdResponseDto>>(await repository.GetAsync());

    public async Task<CageOutLiveSessionResponseDto> GetLiveSessionAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        var now = DateTime.Now;
        liveSessionDemandStore.TouchHttpViewer(id, DateTime.UtcNow, HttpViewerLeaseTtl);
        var isOnline = entity.LastSeenAt.HasValue && entity.LastSeenAt.Value >= now.Subtract(OnlineThreshold);

        var snapshot = liveSessionStore.Get(id, LiveSessionStalenessThreshold, now);
        if (snapshot is null)
        {
            return BuildFallbackSnapshot(entity, now, isOnline);
        }

        snapshot.CageOutId = entity.Id;
        snapshot.UnitId = entity.UnitId;
        snapshot.Identifier = entity.Identifier;
        snapshot.IsOnline = isOnline;
        snapshot.OperationalStatus = entity.OperationalStatus;
        snapshot.CurrentMode = entity.CurrentMode;
        snapshot.LastSeenAt = entity.LastSeenAt;
        return snapshot;
    }

    public async Task<CageOutLiveSessionDemandResponseDto> GetLiveSessionDemandAsync(string identifier)
    {
        var entity = await repository.FirstOrDefaultAsync(x => x.Identifier == identifier)
            ?? throw new NotFoundException("Cage ID");

        var activeViewerCount = liveSessionDemandStore.GetActiveViewerCount(entity.Id, DateTime.UtcNow);
        return new CageOutLiveSessionDemandResponseDto
        {
            CageOutId = entity.Id,
            Identifier = entity.Identifier,
            ActiveViewerCount = activeViewerCount,
            ShouldPublishLiveSession = activeViewerCount > 0,
        };
    }

    public async Task HeartbeatAsync(string identifier, CageOutIdHeartbeatDto? heartbeatDto = null)
    {
        var entity = await repository.FirstOrDefaultAsync(x => x.Identifier == identifier)
            ?? throw new NotFoundException("Cage ID");
        var now = DateTime.Now;
        var previousLastSeen = entity.LastSeenAt;
        // Mesma convenção de CreatedAt/UpdatedAt (hora local do servidor), não UTC —
        // evita desalinhamento de 3h ao comparar com "agora" no front (América/São Paulo).
        entity.LastSeenAt = now;

        var hasOperationalStatusUpdate = false;
        if (heartbeatDto?.OperationalStatus is not null)
        {
            entity.OperationalStatus = heartbeatDto.OperationalStatus.Value;
            entity.CurrentMode = string.IsNullOrWhiteSpace(heartbeatDto.CurrentMode)
                ? null
                : heartbeatDto.CurrentMode.Trim();
            entity.StatusUpdatedAt = now;
            hasOperationalStatusUpdate = true;
        }

        CageOutLiveSessionResponseDto? liveSessionSnapshot = null;
        if (heartbeatDto?.LiveSession is not null)
        {
            liveSessionSnapshot = liveSessionStore.Upsert(entity, heartbeatDto.LiveSession, now, isOnline: true);
        }

        var shouldPersistHeartbeat = hasOperationalStatusUpdate
            || previousLastSeen is null
            || now - previousLastSeen.Value >= HeartbeatPersistenceInterval;

        if (shouldPersistHeartbeat)
        {
            await repository.UpdateAsync(entity);
        }

        if (shouldPersistHeartbeat && entity.CageClusterId.HasValue)
        {
            await clusterStatusNotifier.NotifyClusterStatusChangedAsync(entity.CageClusterId.Value);
        }

        if (liveSessionSnapshot is not null)
        {
            await liveSessionNotifier.NotifySessionUpdatedAsync(liveSessionSnapshot);
        }
    }

    public async Task BindAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity.BoundAt is not null)
            throw new ConflictException("Este Cage ID já está vinculado a outro terminal.");

        entity.BoundAt = DateTime.Now;
        await repository.UpdateAsync(entity);
    }

    public async Task UnbindAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        entity.BoundAt = null;
        await repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(CageOutIdDto dto)
    {
        _ = await unitRepository.GetByIdAsync(dto.UnitId);
    }

    private static CageOutLiveSessionResponseDto BuildFallbackSnapshot(CageOutId entity, DateTime now, bool isOnline)
    {
        return new CageOutLiveSessionResponseDto
        {
            CageOutId = entity.Id,
            UnitId = entity.UnitId,
            Identifier = entity.Identifier,
            IsOnline = isOnline,
            OperationalStatus = entity.OperationalStatus,
            CurrentMode = entity.CurrentMode,
            LastSeenAt = entity.LastSeenAt,
            GeneratedAt = now,
            Session = new CageOutLiveSessionPayloadResponseDto
            {
                IsActive = false,
                Items = [],
            },
        };
    }
}