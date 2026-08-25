using FlyGates.Application.Dao;
using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutTransactions;

public interface ICageOutTransactionRepository : IBaseRepository<CageOutTransactionDao, CageOutTransaction>
{
    Task<CageOutTransaction?> GetByClientTransactionIdAsync(Guid clientTransactionId);
    Task<List<CageOutTransaction>> GetAllWithItemsAsync();
    Task<CageOutTransaction?> GetByIdWithItemsAsync(Guid id);
    Task<CageOutTransaction> CreateWithItemsAsync(CageOutTransaction transaction);
}