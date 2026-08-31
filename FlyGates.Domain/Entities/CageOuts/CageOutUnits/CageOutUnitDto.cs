namespace FlyGates.Application.Entities.CageOuts.CageOutUnits;

public class CageOutUnitDto
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Guid ClientId { get; set; }
    public bool IsActive { get; set; } = true;
}
