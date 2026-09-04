using FlyGates.Application.Entities.CageOuts;
using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutRejects;

public class CageOutReject : BaseModel
{
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public DateTime Schedule { get; set; }
    public required string CheckoutId { get; set; }
    public decimal ExpectedWeight { get; set; }
    public decimal RealWeight { get; set; }
    public required string ProductImage { get; set; }
    public required string ProductVideo { get; set; }
    public CageOutRejectReason Reason { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
