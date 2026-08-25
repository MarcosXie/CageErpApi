namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public class CageOutTransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid ClientTransactionId { get; set; }
    public required string CheckoutId { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CageOutTransactionItemResponseDto> Items { get; set; } = [];
}