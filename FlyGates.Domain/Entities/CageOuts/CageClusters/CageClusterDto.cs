namespace FlyGates.Application.Entities.CageOuts.CageClusters;

public class CageClusterDto
{
    public required Guid UnitId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> CageOutIds { get; set; } = [];
    public List<CageClusterMemberDto> Members { get; set; } = [];
}