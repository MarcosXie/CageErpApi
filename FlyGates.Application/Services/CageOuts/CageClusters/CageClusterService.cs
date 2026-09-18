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
    private sealed record ClusterAssignment(Guid CageOutId, int BoxNumber);

    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(30);

    public async Task<CageClusterResponseDto> CreateAsync(CageClusterDto dto)
    {
        Normalize(dto);
        await ValidateAsync(dto);

        var entity = mapper.Map<CageCluster>(dto);
        var id = await repository.CreateAsync(entity);

        await SyncCageOutAssignmentsAsync(id, dto.UnitId, ToAssignments(dto));
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

        await SyncCageOutAssignmentsAsync(id, dto.UnitId, ToAssignments(dto));
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

        return clusters.Select(cluster => BuildResponse(cluster, cageOutIds)).ToList();
    }

    public async Task<CageClusterStatusSnapshotDto> GetStatusSnapshotAsync(Guid id)
    {
        var cluster = await repository.GetByIdAsync(id);
        var now = DateTime.Now;
        var thresholdTime = now.Subtract(OnlineThreshold);

        var orderedCages = OrderClusterMembers((await cageOutIdRepository.GetAsync())
            .Where(item => item.CageClusterId == id)
            .ToList());

        var cages = orderedCages
            .Select((item, index) =>
            {
                var isOnline = item.LastSeenAt.HasValue && item.LastSeenAt.Value >= thresholdTime;
                var isAvailable = item.IsActive && isOnline && item.OperationalStatus == CageOutOperationalStatus.Available;

                return new CageClusterStatusItemDto
                {
                    CageOutId = item.Id,
                    BoxNumber = item.ClusterBoxNumber ?? (index + 1),
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
        return BuildResponse(cluster, await cageOutIdRepository.GetAsync());
    }

    private CageClusterResponseDto BuildResponse(CageCluster cluster, List<CageOutId> allCageOutIds)
    {
        var response = mapper.Map<CageClusterResponseDto>(cluster);

        var orderedMembers = BuildMemberDtos(
            OrderClusterMembers(allCageOutIds.Where(item => item.CageClusterId == cluster.Id).ToList()));

        response.Members = orderedMembers;
        response.CageOutIds = orderedMembers.Select(member => member.CageOutId).ToList();
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

        var selectedIds = ToAssignments(dto).Select(item => item.CageOutId).ToList();
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

    private async Task SyncCageOutAssignmentsAsync(Guid clusterId, Guid unitId, List<ClusterAssignment> assignments)
    {
        var targetIds = assignments.Select(item => item.CageOutId).ToHashSet();
        var cageOutIds = await cageOutIdRepository.GetAsync();
        var cageOutById = cageOutIds.ToDictionary(item => item.Id, item => item);
        var clustersToReindex = new HashSet<Guid>();

        var currentlyAssigned = cageOutIds
            .Where(item => item.CageClusterId == clusterId)
            .Select(item => item.Id)
            .ToHashSet();

        var idsToUnassign = currentlyAssigned.Where(id => !targetIds.Contains(id)).ToList();

        foreach (var id in idsToUnassign)
        {
            if (!cageOutById.TryGetValue(id, out var item))
                continue;

            item.CageClusterId = null;
            item.ClusterBoxNumber = null;
            await cageOutIdRepository.UpdateAsync(item);
        }

        foreach (var assignment in assignments)
        {
            if (!cageOutById.TryGetValue(assignment.CageOutId, out var item))
                throw new BadRequestException("Um ou mais Cage IDs informados nao existem.");

            if (item.UnitId != unitId)
                throw new BadRequestException("Cage ID fora da unidade selecionada.");

            if (item.CageClusterId.HasValue && item.CageClusterId.Value != clusterId)
                clustersToReindex.Add(item.CageClusterId.Value);

            item.CageClusterId = clusterId;
            item.ClusterBoxNumber = assignment.BoxNumber;
            await cageOutIdRepository.UpdateAsync(item);
        }

        foreach (var movedClusterId in clustersToReindex)
        {
            await ReindexClusterMembersAsync(movedClusterId);
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
            item.ClusterBoxNumber = null;
            await cageOutIdRepository.UpdateAsync(item);
        }
    }

    private async Task ReindexClusterMembersAsync(Guid clusterId)
    {
        var orderedMembers = OrderClusterMembers((await cageOutIdRepository.GetAsync())
            .Where(item => item.CageClusterId == clusterId)
            .ToList());

        for (var index = 0; index < orderedMembers.Count; index++)
        {
            var expectedBoxNumber = index + 1;
            var item = orderedMembers[index];
            if (item.ClusterBoxNumber == expectedBoxNumber)
                continue;

            item.ClusterBoxNumber = expectedBoxNumber;
            await cageOutIdRepository.UpdateAsync(item);
        }
    }

    private static List<CageOutId> OrderClusterMembers(List<CageOutId> members)
    {
        return members
            .OrderBy(item => item.ClusterBoxNumber ?? int.MaxValue)
            .ThenBy(item => item.Identifier, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<CageClusterMemberDto> BuildMemberDtos(List<CageOutId> members)
    {
        return members
            .Select((item, index) => new CageClusterMemberDto
            {
                CageOutId = item.Id,
                BoxNumber = item.ClusterBoxNumber ?? (index + 1),
            })
            .ToList();
    }

    private static void Normalize(CageClusterDto dto)
    {
        dto.Name = dto.Name.Trim();
        dto.Code = dto.Code.Trim();

        var sourceIds = dto.Members.Count > 0
            ? dto.Members.Select(member => member.CageOutId)
            : dto.CageOutIds;

        var uniqueIds = new List<Guid>();
        var seen = new HashSet<Guid>();

        foreach (var id in sourceIds)
        {
            if (seen.Add(id))
                uniqueIds.Add(id);
        }

        dto.CageOutIds = uniqueIds;
        dto.Members = uniqueIds
            .Select((id, index) => new CageClusterMemberDto
            {
                CageOutId = id,
                BoxNumber = index + 1,
            })
            .ToList();
    }

    private static List<ClusterAssignment> ToAssignments(CageClusterDto dto)
    {
        if (dto.Members.Count > 0)
        {
            return dto.Members
                .Select(member => new ClusterAssignment(member.CageOutId, member.BoxNumber))
                .ToList();
        }

        return dto.CageOutIds
            .Select((id, index) => new ClusterAssignment(id, index + 1))
            .ToList();
    }
}