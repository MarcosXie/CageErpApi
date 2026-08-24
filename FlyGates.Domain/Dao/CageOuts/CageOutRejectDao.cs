using FlyGates.Application.Dao.Shared;
using FlyGates.Application.Entities.CageOuts;

namespace FlyGates.Application.Dao;

public class CageOutRejectDao : BaseDao
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime Schedule { get; set; }
    public string CheckoutId { get; set; } = string.Empty;
    public decimal ExpectedWeight { get; set; }
    public decimal RealWeight { get; set; }
    public string ProductImage { get; set; } = string.Empty;
    public string ProductVideo { get; set; } = string.Empty;
    public CageOutRejectReason Reason { get; set; }
}
