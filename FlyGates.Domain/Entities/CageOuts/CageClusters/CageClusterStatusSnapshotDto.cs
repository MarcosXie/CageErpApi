namespace FlyGates.Application.Entities.CageOuts.CageClusters;

public class CageClusterStatusSnapshotDto
{
    public required Guid ClusterId { get; set; }
    public required Guid UnitId { get; set; }
    public required string ClusterName { get; set; }
    public required string ClusterCode { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<CageClusterStatusItemDto> Cages { get; set; } = [];
}