using FlyGates.Application.Dao.Shared;

namespace FlyGates.Application.Dao;

public class CageOutTransactionDao : BaseDao
{
    public Guid ClientTransactionId { get; set; }
    public string CheckoutId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<CageOutTransactionItemDao> Items { get; set; } = [];
}