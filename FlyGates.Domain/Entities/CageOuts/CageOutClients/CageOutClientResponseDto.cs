namespace FlyGates.Application.Entities.CageOuts.CageOutClients;

public class CageOutClientResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
