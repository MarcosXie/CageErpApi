using AutoMapper;
using FlyGates.Application.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutTransactionRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutTransactionDao, CageOutTransaction>(context, mapper), ICageOutTransactionRepository
{
    public async Task<CageOutTransaction?> GetByClientTransactionIdAsync(Guid clientTransactionId)
    {
        var transaction = await Context.CageOutTransactions
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.ClientTransactionId == clientTransactionId);

        return transaction is null ? null : Mapper.Map<CageOutTransaction>(transaction);
    }

    public async Task<List<CageOutTransaction>> GetAllWithItemsAsync()
    {
        var transactions = await Context.CageOutTransactions
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync();

        return Mapper.Map<List<CageOutTransaction>>(transactions);
    }

    public async Task<CageOutTransaction?> GetByIdWithItemsAsync(Guid id)
    {
        var transaction = await Context.CageOutTransactions
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        return transaction is null ? null : Mapper.Map<CageOutTransaction>(transaction);
    }

    public async Task<CageOutTransaction> CreateWithItemsAsync(CageOutTransaction transaction)
    {
        var transactionDao = Mapper.Map<CageOutTransactionDao>(transaction);
        var now = DateTime.UtcNow;
        transactionDao.CreatedAt = now;
        transactionDao.UpdatedAt = now;

        foreach (var item in transactionDao.Items)
        {
            item.CreatedAt = now;
            item.UpdatedAt = now;
        }

        Context.CageOutTransactions.Add(transactionDao);
        await Context.SaveChangesAsync();

        return Mapper.Map<CageOutTransaction>(transactionDao);
    }
}