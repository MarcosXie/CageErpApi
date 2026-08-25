using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;

namespace FlyGates.Application.Services.CageOuts.CageOutTransactions;

public class CageOutTransactionService(ICageOutTransactionRepository repository, IMapper mapper) : ICageOutTransactionService
{
    public async Task<CageOutTransactionResponseDto> CreateAsync(CageOutTransactionDto transactionDto)
    {
        Validate(transactionDto);

        var existing = await repository.GetByClientTransactionIdAsync(transactionDto.ClientTransactionId);
        if (existing is not null)
        {
            return mapper.Map<CageOutTransactionResponseDto>(existing);
        }

        var transaction = new CageOutTransaction
        {
            ClientTransactionId = transactionDto.ClientTransactionId,
            CheckoutId = transactionDto.CheckoutId.Trim(),
            CompletedAt = transactionDto.CompletedAt.ToUniversalTime(),
            Items = transactionDto.Items.Select(item => new CageOutTransactionItem
            {
                ProductCode = item.ProductCode.Trim(),
                ProductName = item.ProductName.Trim(),
                Quantity = item.Quantity,
                UnitPrice = decimal.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero),
                Subtotal = decimal.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero),
            }).ToList(),
        };

        transaction.ItemCount = transaction.Items.Sum(item => item.Quantity);
        transaction.TotalAmount = transaction.Items.Sum(item => item.Subtotal);

        var created = await repository.CreateWithItemsAsync(transaction);
        return mapper.Map<CageOutTransactionResponseDto>(created);
    }

    public async Task<List<CageOutTransactionResponseDto>> GetAllAsync()
    {
        var transactions = await repository.GetAllWithItemsAsync();
        return mapper.Map<List<CageOutTransactionResponseDto>>(transactions);
    }

    public async Task<CageOutTransactionResponseDto?> GetByIdAsync(Guid id)
    {
        var transaction = await repository.GetByIdWithItemsAsync(id);
        return transaction is null ? null : mapper.Map<CageOutTransactionResponseDto>(transaction);
    }

    private static void Validate(CageOutTransactionDto transaction)
    {
        if (transaction.ClientTransactionId == Guid.Empty)
        {
            throw new ArgumentException("ClientTransactionId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(transaction.CheckoutId))
        {
            throw new ArgumentException("CheckoutId é obrigatório.");
        }

        if (transaction.Items.Count == 0)
        {
            throw new ArgumentException("A venda deve ter pelo menos um item.");
        }

        if (transaction.Items.Any(item =>
                string.IsNullOrWhiteSpace(item.ProductCode) ||
                string.IsNullOrWhiteSpace(item.ProductName) ||
                item.Quantity <= 0 ||
                item.UnitPrice < 0))
        {
            throw new ArgumentException("Os itens da venda são inválidos.");
        }
    }
}