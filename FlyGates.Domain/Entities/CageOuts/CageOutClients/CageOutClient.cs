using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutClients;

public class CageOutClient : BaseModel
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
}
