namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public interface ICageOutTransactionService
{
    Task<CageOutTransactionResponseDto> CreateAsync(CageOutTransactionDto transactionDto);
    Task<List<CageOutTransactionResponseDto>> GetAllAsync();
    Task<CageOutTransactionResponseDto?> GetByIdAsync(Guid id);
}