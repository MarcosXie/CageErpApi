using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Domain.Dao;
using FlyGates.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutIdRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutIdDao, CageOutId>(context, mapper), ICageOutIdRepository
{
    private const int DefaultSequenceId = 1;

    public async Task<Guid> CreateWithGeneratedIdentifierAsync(CageOutId item, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        var sequence = await Context.CageOutIdSequences
            .FromSqlRaw("SELECT * FROM cage_out_id_sequence WHERE Id = {0} FOR UPDATE", DefaultSequenceId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("A sequência global de Cage IDs não foi inicializada.");

        if (sequence.NextSequenceNumber <= 0)
            throw new InvalidOperationException("A sequência global de Cage IDs está inválida.");

        item.Identifier = $"CageId_{sequence.NextSequenceNumber:D3}";
        sequence.NextSequenceNumber++;

        var entityDao = Mapper.Map<CageOutIdDao>(item);
        entityDao.CreatedAt = DateTime.Now;

        Database.Add(entityDao);

        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return entityDao.Id;
    }
}