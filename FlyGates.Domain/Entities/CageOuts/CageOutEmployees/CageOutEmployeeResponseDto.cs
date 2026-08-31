namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public class CageOutEmployeeResponseDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string BadgeCode { get; set; }
    public required string FingerprintData { get; set; }
    public required Guid UnitId { get; set; }
    public List<string> AllowedProcedures { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
