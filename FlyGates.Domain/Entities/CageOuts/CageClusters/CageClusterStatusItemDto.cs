using FlyGates.Application.Entities.CageOuts.CageOutIds;

namespace FlyGates.Application.Entities.CageOuts.CageClusters;

public class CageClusterStatusItemDto
{
    public required Guid CageOutId { get; set; }
    public required string Identifier { get; set; }
    public required bool IsActive { get; set; }
    public required bool IsBound { get; set; }
    public required bool IsOnline { get; set; }
    public required bool IsAvailable { get; set; }
    public required CageOutOperationalStatus OperationalStatus { get; set; }
    public string? CurrentMode { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
}