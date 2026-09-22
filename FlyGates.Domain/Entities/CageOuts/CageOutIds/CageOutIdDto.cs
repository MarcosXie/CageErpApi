namespace FlyGates.Application.Entities.CageOuts.CageOutIds;

public class CageOutIdDto
{
    public required Guid UnitId { get; set; }
    public bool IsActive { get; set; } = true;
}