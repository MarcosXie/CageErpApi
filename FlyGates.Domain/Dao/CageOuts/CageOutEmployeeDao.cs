using FlyGates.Application.Dao.Shared;

namespace FlyGates.Application.Dao;

public class CageOutEmployeeDao : BaseDao
{
    public string Name { get; set; } = string.Empty;
    public string BadgeCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FingerprintData { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public List<string> AllowedProcedures { get; set; } = [];
}
