using FlyGates.Application.Dao.Shared;

namespace FlyGates.Domain.Dao;

public class CageOutIdDao : BaseDao
{
    public required Guid UnitId { get; set; }
    public required string Identifier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAt { get; set; }
}