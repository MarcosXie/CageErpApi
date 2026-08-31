using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutUnits;

public class CageOutUnit : BaseModel
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Guid ClientId { get; set; }
    public bool IsActive { get; set; } = true;
}
