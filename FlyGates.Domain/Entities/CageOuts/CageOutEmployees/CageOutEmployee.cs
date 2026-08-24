using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public class CageOutEmployee : BaseModel
{
    public required string Name { get; set; }
    public required string BadgeCode { get; set; }
    public required string Password { get; set; }
    public required string FingerprintData { get; set; }
    public List<string> AllowedProcedures { get; set; } = [];
}
