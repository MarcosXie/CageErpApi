namespace FlyGates.Application.Entities.CageOuts.CageOutIds;

public sealed class CageOutLiveSessionHeartbeatDto
{
    public bool IsActive { get; set; }
    public string? CheckoutId { get; set; }
    public decimal CurrentTotalAmount { get; set; }
    public int ScannedCount { get; set; }
    public int ApprovedCount { get; set; }
    public DateTimeOffset? SessionStartedAt { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public string? PhotoSnapshotBase64 { get; set; }
    public string? VideoSnapshotBase64 { get; set; }
    public List<CageOutLiveSessionItemHeartbeatDto> Items { get; set; } = [];
}

public sealed class CageOutLiveSessionItemHeartbeatDto
{
    public Guid ItemId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MatchStatus { get; set; } = "Pending";
    public decimal? ExpectedWeightKg { get; set; }
    public decimal? RealWeightKg { get; set; }
    public decimal? UnitPrice { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
}

public sealed class CageOutLiveSessionResponseDto
{
    public required Guid CageOutId { get; set; }
    public required Guid UnitId { get; set; }
    public required string Identifier { get; set; }
    public required bool IsOnline { get; set; }
    public required CageOutOperationalStatus OperationalStatus { get; set; }
    public string? CurrentMode { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public CageOutLiveSessionPayloadResponseDto Session { get; set; } = new();
}

public sealed class CageOutLiveSessionPayloadResponseDto
{
    public bool IsActive { get; set; }
    public string? CheckoutId { get; set; }
    public decimal CurrentTotalAmount { get; set; }
    public int ScannedCount { get; set; }
    public int ApprovedCount { get; set; }
    public DateTimeOffset? SessionStartedAt { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public string? PhotoSnapshotBase64 { get; set; }
    public string? VideoSnapshotBase64 { get; set; }
    public List<CageOutLiveSessionItemResponseDto> Items { get; set; } = [];
}

public sealed class CageOutLiveSessionItemResponseDto
{
    public Guid ItemId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MatchStatus { get; set; } = "Pending";
    public decimal? ExpectedWeightKg { get; set; }
    public decimal? RealWeightKg { get; set; }
    public decimal? UnitPrice { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
}
