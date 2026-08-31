using FlyGates.Application.Dao.Shared;

namespace FlyGates.Domain.Dao;

public class CageOutUnitDao : BaseDao
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Guid ClientId { get; set; }
    public bool IsActive { get; set; } = true;
}
