using FlyGates.Application.Entities.CageOuts;

namespace FlyGates.Application.Entities.CageOuts.CageOutRejects;

public class CageOutRejectDto
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
}
