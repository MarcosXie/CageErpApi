using FlyGates.Application.Dao.Shared;

namespace FlyGates.Application.Dao;

public class CageOutTransactionItemDao : BaseDao
{
    public Guid TransactionId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public CageOutTransactionDao? Transaction { get; set; }
}