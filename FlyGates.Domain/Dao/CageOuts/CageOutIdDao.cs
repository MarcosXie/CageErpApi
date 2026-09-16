using FlyGates.Application.Dao.Shared;
using FlyGates.Application.Entities.CageOuts.CageOutIds;

namespace FlyGates.Domain.Dao;

public class CageOutIdDao : BaseDao
{
    public required Guid UnitId { get; set; }
    public Guid? CageClusterId { get; set; }
    public required string Identifier { get; set; }
    public bool IsActive { get; set; } = true;
    public CageOutOperationalStatus OperationalStatus { get; set; } = CageOutOperationalStatus.Unknown;
    public string? CurrentMode { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? BoundAt { get; set; }
}