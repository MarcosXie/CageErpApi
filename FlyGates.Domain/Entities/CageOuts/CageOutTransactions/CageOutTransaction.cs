using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public class CageOutTransaction : BaseModel
{
    public Guid ClientTransactionId { get; set; }
    public required string CheckoutId { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<CageOutTransactionItem> Items { get; set; } = [];
}