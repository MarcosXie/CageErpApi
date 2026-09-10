using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutIds;

public class CageOutId : BaseModel
{
    public required Guid UnitId { get; set; }
    public required string Identifier { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Timestamp (UTC) do último heartbeat recebido do terminal.</summary>
    public DateTime? LastSeenAt { get; set; }
}