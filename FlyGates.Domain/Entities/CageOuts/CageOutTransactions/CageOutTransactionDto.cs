namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public class CageOutTransactionDto
{
    public Guid ClientTransactionId { get; set; }
    public required string CheckoutId { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<CageOutTransactionItemDto> Items { get; set; } = [];
}