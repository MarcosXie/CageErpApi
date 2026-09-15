using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageClusters;

public class CageCluster : BaseModel
{
    public required Guid UnitId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
}