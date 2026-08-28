using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutUnits;

public class CageOutUnit : BaseModel
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Guid ClientId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
