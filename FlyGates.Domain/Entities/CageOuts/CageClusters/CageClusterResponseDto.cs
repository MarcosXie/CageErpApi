namespace FlyGates.Application.Entities.CageOuts.CageClusters;

public class CageClusterResponseDto
{
    public required Guid Id { get; set; }
    public required Guid UnitId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> CageOutIds { get; set; } = [];
    public List<CageClusterMemberDto> Members { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}