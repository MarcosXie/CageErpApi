namespace FlyGates.Application.Entities.CageOuts.CageOutIds;

public class CageOutIdResponseDto
{
    public required Guid Id { get; set; }
    public required Guid UnitId { get; set; }
    public Guid? CageClusterId { get; set; }
    public required string Identifier { get; set; }
    public bool IsActive { get; set; }
    public CageOutOperationalStatus OperationalStatus { get; set; }
    public string? CurrentMode { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? BoundAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}