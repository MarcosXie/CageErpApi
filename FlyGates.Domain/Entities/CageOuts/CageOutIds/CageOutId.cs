using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutIds;

public class CageOutId : BaseModel
{
    public required Guid UnitId { get; set; }
    public Guid? CageClusterId { get; set; }
    public int? ClusterBoxNumber { get; set; }
    public required string Identifier { get; set; }
    public bool IsActive { get; set; } = true;
    public CageOutOperationalStatus OperationalStatus { get; set; } = CageOutOperationalStatus.Unknown;
    public string? CurrentMode { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }

    /// <summary>Timestamp (hora local do servidor) do ultimo heartbeat recebido do terminal.</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Quando != null, este Cage ID está vinculado permanentemente a um terminal.</summary>
    public DateTime? BoundAt { get; set; }
}