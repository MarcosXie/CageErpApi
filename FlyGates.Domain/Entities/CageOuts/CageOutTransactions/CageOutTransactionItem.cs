using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public class CageOutTransactionItem : BaseModel
{
    public Guid TransactionId { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}