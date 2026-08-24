using System.Linq.Expressions;
using AutoMapper;
using FlyGates.Application.Dao.Shared;
using FlyGates.Application.Entities.Shared;
using FlyGates.Application.Exceptions;
using FlyGates.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Repositories;

public class BaseRepository<TDao, TEntity> : IBaseRepository<TDao, TEntity>
	where TDao : BaseDao
	where TEntity : BaseModel
{
	protected readonly FlyGatesDbContext Context;
	protected readonly DbSet<TDao> Database;
	protected readonly IMapper Mapper;

	protected BaseRepository(FlyGatesDbContext context, IMapper mapper)
	{
		Context = context;
		Database = context.Set<TDao>();
		Mapper = mapper;
	}

	public virtual async Task<Guid> CreateAsync(TEntity item, CancellationToken cancellationToken = default)
	{
		TDao entityDao = Mapper.Map<TDao>(item);

		entityDao.CreatedAt = DateTime.Now;

		Database.Add(entityDao);

		await Context.SaveChangesAsync(cancellationToken);

		return entityDao.Id;
	}

	public async Task CreateAsync(IEnumerable<TEntity> items, CancellationToken cancellationToken = default)
	{
		List<TDao> entityDao = Mapper.Map<List<TDao>>(items);

		foreach (TDao e in entityDao)
			e.CreatedAt = DateTime.Now;

		Database.AddRange(entityDao);

		await Context.SaveChangesAsync(cancellationToken);
	}

	public virtual async Task<List<TEntity>> GetAsync(CancellationToken cancellationToken = default) =>
		Mapper.Map<List<TEntity>>(await Database.AsNoTracking().ToListAsync(cancellationToken));

	public async Task<List<TEntity>> GetAsync(Expression<Func<TDao, bool>> expression)
	{
		var items = await Database
			.AsNoTracking()
			.Where(expression)
			.ToListAsync();

		return Mapper.Map<List<TEntity>>(items);
	}

	public virtual async Task<TEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		TDao? entity = await Database
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken: cancellationToken);

		return entity is null
			? throw new Exception($"{nameof(TDao)} com id = {id}")
		: Mapper.Map<TEntity>(entity);
	}

	public async Task<bool> AnyAsync(Guid id, CancellationToken cancellationToken = default) =>
	   await Database
			.AsNoTracking()
			.AnyAsync(x => x.Id!.Equals(id), cancellationToken: cancellationToken);

	public async Task<int> CountAsync(Expression<Func<TDao, bool>> expression) => 
		await Database
			.AsNoTracking()
			.CountAsync(expression);


	public async Task<bool> AnyAsync(Expression<Func<TDao, bool>> expression) =>
	   await Database
			.AsNoTracking()
			.AnyAsync(expression);

	public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TDao, bool>> expression)
	{
		TDao? entity = await Database
			.AsNoTracking()
			.FirstOrDefaultAsync(expression);

		return entity is null
			? null
		: Mapper.Map<TEntity>(entity);
	}

	public virtual async Task<TEntity?> GetByIdOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
	{
		TDao? entity = await Database.AsNoTracking().FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken: cancellationToken);

		return entity is null ? default : Mapper.Map<TEntity>(entity);
	}

	public async Task UpdateAsync(TEntity item, CancellationToken cancellationToken = default)
	{
		TDao existingEntity = await Database.FirstOrDefaultAsync(x => x.Id!.Equals(item.Id), cancellationToken: cancellationToken)
		   ?? throw new NotFoundException($"{nameof(TDao)} com id = {item.Id}");
		existingEntity.UpdatedAt = DateTime.Now;

		Mapper.Map(item, existingEntity);

		await Context.SaveChangesAsync(cancellationToken);
	}

	public async Task UpdateAsync(IEnumerable<TEntity> items, CancellationToken cancellationToken = default)
	{
		List<TEntity> itemsList = items.ToList();

		List<Guid> itemsIds = itemsList.Select(x => x.Id).ToList();

		List<TDao> entityDaos = Database.Where(x => itemsIds.Contains(x.Id)).ToList();

		foreach (TEntity? item in itemsList)
		{
			item.UpdatedAt = DateTime.Now;
			Mapper.Map(item, entityDaos.FirstOrDefault(x => x.Id!.Equals(item.Id)));
		}
		await Context.SaveChangesAsync(cancellationToken);
	}

	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		TDao? item = await Database.FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken: cancellationToken);

		if (item is null) return;

		Database.Remove(item);

		await Context.SaveChangesAsync(cancellationToken);
	}

	public async Task<List<TEntity>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default) =>
		Mapper.Map<List<TEntity>>(await Database.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken));

	public async Task DeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default)
	{
		var entities = await Database.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

		Database.RemoveRange(entities);

		await Context.SaveChangesAsync(cancellationToken);
	}
}
