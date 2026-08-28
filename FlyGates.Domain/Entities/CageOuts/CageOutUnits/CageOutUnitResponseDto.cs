namespace FlyGates.Application.Entities.CageOuts.CageOutUnits;

public class CageOutUnitResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Guid ClientId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
