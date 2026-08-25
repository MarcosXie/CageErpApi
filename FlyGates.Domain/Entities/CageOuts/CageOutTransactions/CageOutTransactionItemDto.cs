namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public class CageOutTransactionItemDto
{
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}