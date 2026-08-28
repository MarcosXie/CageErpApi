using FlyGates.Application.Dao.Shared;

namespace FlyGates.Domain.Dao;

public class CageOutClientDao : BaseDao
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
