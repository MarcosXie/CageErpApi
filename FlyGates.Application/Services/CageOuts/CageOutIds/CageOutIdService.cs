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
    Task BindAsync(Guid id);
    Task UnbindAsync(Guid id);
}

public class CageOutIdService(
    ICageOutIdRepository repository,
    ICageOutUnitRepository unitRepository,
    ICageClusterStatusNotifier clusterStatusNotifier,
    IMapper mapper) : ICageOutIdService
{
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

    public async Task HeartbeatAsync(string identifier, CageOutIdHeartbeatDto? heartbeatDto = null)
    {
        var entity = await repository.FirstOrDefaultAsync(x => x.Identifier == identifier)
            ?? throw new NotFoundException("Cage ID");
        // Mesma convenção de CreatedAt/UpdatedAt (hora local do servidor), não UTC —
        // evita desalinhamento de 3h ao comparar com "agora" no front (América/São Paulo).
        entity.LastSeenAt = DateTime.Now;

        if (heartbeatDto?.OperationalStatus is not null)
        {
            entity.OperationalStatus = heartbeatDto.OperationalStatus.Value;
            entity.CurrentMode = string.IsNullOrWhiteSpace(heartbeatDto.CurrentMode)
                ? null
                : heartbeatDto.CurrentMode.Trim();
            entity.StatusUpdatedAt = DateTime.Now;
        }

        await repository.UpdateAsync(entity);

        if (entity.CageClusterId.HasValue)
        {
            await clusterStatusNotifier.NotifyClusterStatusChangedAsync(entity.CageClusterId.Value);
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
}