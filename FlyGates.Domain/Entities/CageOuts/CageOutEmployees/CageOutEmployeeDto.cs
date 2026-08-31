namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public class CageOutEmployeeDto
{
    public required string Name { get; set; }
    public required string BadgeCode { get; set; }
    public required string Password { get; set; }
    public required string FingerprintData { get; set; }
    public required Guid UnitId { get; set; }
    public List<string> AllowedProcedures { get; set; } = [];
}
