using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageClusters;
using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.CageOuts.CageClusters;

public interface ICageClusterService
{
    Task<CageClusterResponseDto> CreateAsync(CageClusterDto dto);
    Task UpdateAsync(Guid id, CageClusterDto dto);
    Task DeleteAsync(Guid id);
    Task<CageClusterResponseDto> GetByIdAsync(Guid id);
    Task<List<CageClusterResponseDto>> GetAllAsync();
    Task<CageClusterStatusSnapshotDto> GetStatusSnapshotAsync(Guid id);
}

public class CageClusterService(
    ICageClusterRepository repository,
    ICageOutUnitRepository unitRepository,
    ICageOutIdRepository cageOutIdRepository,
    IMapper mapper) : ICageClusterService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(30);

    public async Task<CageClusterResponseDto> CreateAsync(CageClusterDto dto)
    {
        Normalize(dto);
        await ValidateAsync(dto);

        var entity = mapper.Map<CageCluster>(dto);
        var id = await repository.CreateAsync(entity);

        await SyncCageOutAssignmentsAsync(id, dto.UnitId, dto.CageOutIds);
        var created = await repository.GetByIdAsync(id);
        return await BuildResponseAsync(created);
    }

    public async Task UpdateAsync(Guid id, CageClusterDto dto)
    {
        Normalize(dto);
        await ValidateAsync(dto, id);

        var entity = await repository.GetByIdAsync(id);
        mapper.Map(dto, entity);
        await repository.UpdateAsync(entity);

        await SyncCageOutAssignmentsAsync(id, dto.UnitId, dto.CageOutIds);
    }

    public async Task DeleteAsync(Guid id)
    {
        _ = await repository.GetByIdAsync(id);
        await ClearClusterAssignmentsAsync(id);
        await repository.DeleteAsync(id);
    }

    public async Task<CageClusterResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        return await BuildResponseAsync(entity);
    }

    public async Task<List<CageClusterResponseDto>> GetAllAsync()
    {
        var clusters = await repository.GetAsync();
        var cageOutIds = await cageOutIdRepository.GetAsync();

        var cageOutIdsByCluster = cageOutIds
            .Where(item => item.CageClusterId.HasValue)
            .GroupBy(item => item.CageClusterId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Id).ToList());

        return clusters.Select(cluster =>
        {
            var response = mapper.Map<CageClusterResponseDto>(cluster);
            response.CageOutIds = cageOutIdsByCluster.TryGetValue(cluster.Id, out var ids)
                ? ids
                : [];
            return response;
        }).ToList();
    }

    public async Task<CageClusterStatusSnapshotDto> GetStatusSnapshotAsync(Guid id)
    {
        var cluster = await repository.GetByIdAsync(id);
        var now = DateTime.Now;
        var thresholdTime = now.Subtract(OnlineThreshold);

        var cages = (await cageOutIdRepository.GetAsync())
            .Where(item => item.CageClusterId == id)
            .OrderBy(item => item.Identifier)
            .Select(item =>
            {
                var isOnline = item.LastSeenAt.HasValue && item.LastSeenAt.Value >= thresholdTime;
                var isAvailable = item.IsActive && isOnline && item.OperationalStatus == CageOutOperationalStatus.Available;

                return new CageClusterStatusItemDto
                {
                    CageOutId = item.Id,
                    Identifier = item.Identifier,
                    IsActive = item.IsActive,
                    IsBound = item.BoundAt.HasValue,
                    IsOnline = isOnline,
                    IsAvailable = isAvailable,
                    OperationalStatus = item.OperationalStatus,
                    CurrentMode = item.CurrentMode,
                    LastSeenAt = item.LastSeenAt,
                    StatusUpdatedAt = item.StatusUpdatedAt,
                };
            })
            .ToList();

        return new CageClusterStatusSnapshotDto
        {
            ClusterId = cluster.Id,
            UnitId = cluster.UnitId,
            ClusterName = cluster.Name,
            ClusterCode = cluster.Code,
            GeneratedAt = now,
            Cages = cages,
        };
    }

    private async Task<CageClusterResponseDto> BuildResponseAsync(CageCluster cluster)
    {
        var response = mapper.Map<CageClusterResponseDto>(cluster);
        response.CageOutIds = (await cageOutIdRepository.GetAsync())
            .Where(item => item.CageClusterId == cluster.Id)
            .Select(item => item.Id)
            .ToList();
        return response;
    }

    private async Task ValidateAsync(CageClusterDto dto, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("O nome do cluster e obrigatorio.");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new BadRequestException("O codigo do cluster e obrigatorio.");
        if (dto.Name.Length > 160)
            throw new BadRequestException("O nome do cluster deve ter no maximo 160 caracteres.");
        if (dto.Code.Length > 50)
            throw new BadRequestException("O codigo do cluster deve ter no maximo 50 caracteres.");

        _ = await unitRepository.GetByIdAsync(dto.UnitId);

        var normalizedCode = dto.Code.Trim();
        var duplicateCode = (await repository.GetAsync()).Any(item =>
            item.Id != currentId &&
            item.UnitId == dto.UnitId &&
            string.Equals(item.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (duplicateCode)
            throw new BadRequestException("Ja existe um cluster com este codigo nesta unidade.");

        var selectedIds = dto.CageOutIds.Distinct().ToList();
        if (selectedIds.Count == 0)
            return;

        var cageOutIds = await cageOutIdRepository.GetAsync();
        var existingById = cageOutIds.ToDictionary(item => item.Id, item => item);

        var missingIds = selectedIds.Where(id => !existingById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
            throw new BadRequestException("Um ou mais Cage IDs informados nao existem.");

        var outOfUnit = selectedIds.Any(id => existingById[id].UnitId != dto.UnitId);
        if (outOfUnit)
            throw new BadRequestException("Todos os Cage IDs do cluster devem pertencer a mesma unidade.");
    }

    private async Task SyncCageOutAssignmentsAsync(Guid clusterId, Guid unitId, List<Guid> selectedIds)
    {
        var targetIds = selectedIds.Distinct().ToHashSet();
        var cageOutIds = await cageOutIdRepository.GetAsync();

        var currentlyAssigned = cageOutIds
            .Where(item => item.CageClusterId == clusterId)
            .Select(item => item.Id)
            .ToHashSet();

        var idsToUnassign = currentlyAssigned.Where(id => !targetIds.Contains(id)).ToList();
        var idsToAssign = targetIds.Where(id => !currentlyAssigned.Contains(id)).ToList();

        foreach (var id in idsToUnassign)
        {
            var item = await cageOutIdRepository.GetByIdAsync(id);
            item.CageClusterId = null;
            await cageOutIdRepository.UpdateAsync(item);
        }

        foreach (var id in idsToAssign)
        {
            var item = await cageOutIdRepository.GetByIdAsync(id);
            if (item.UnitId != unitId)
                throw new BadRequestException("Cage ID fora da unidade selecionada.");

            item.CageClusterId = clusterId;
            await cageOutIdRepository.UpdateAsync(item);
        }
    }

    private async Task ClearClusterAssignmentsAsync(Guid clusterId)
    {
        var assignedIds = (await cageOutIdRepository.GetAsync())
            .Where(item => item.CageClusterId == clusterId)
            .Select(item => item.Id)
            .ToList();

        foreach (var id in assignedIds)
        {
            var item = await cageOutIdRepository.GetByIdAsync(id);
            item.CageClusterId = null;
            await cageOutIdRepository.UpdateAsync(item);
        }
    }

    private static void Normalize(CageClusterDto dto)
    {
        dto.Name = dto.Name.Trim();
        dto.Code = dto.Code.Trim();
    }
}