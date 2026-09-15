using FlyGates.Application.Dao.Shared;

namespace FlyGates.Domain.Dao;

public class CageClusterDao : BaseDao
{
    public required Guid UnitId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
}