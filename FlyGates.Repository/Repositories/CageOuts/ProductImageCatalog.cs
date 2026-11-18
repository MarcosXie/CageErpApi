using System.Data;
using FlyGates.Application.Entities.Storage;
using FlyGates.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Repositories.CageOuts;

public sealed class ProductImageCatalog(FlyGatesDbContext db) : IProductImageCatalog
{
    public async Task RecordAsync(string sku, string fileName, bool deleted, long length, DateTimeOffset modified, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize revisions through commit; otherwise a reader can skip an earlier uncommitted revision.
        await db.Set<ReferenceRevision>().Where(x => x.Id == 1)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Value, x => x.Value + 1), ct);
        var revision = await db.Set<ReferenceRevision>().Where(x => x.Id == 1).Select(x => x.Value).SingleAsync(ct);
        var refs = db.Set<CatalogReference>();
        var row = await refs.FindAsync([sku, fileName], ct);
        if (row is null)
            refs.Add(new CatalogReference { Sku = sku, FileName = fileName, Deleted = deleted,
                Length = length, ModifiedUtc = modified.UtcDateTime, Revision = revision });
        else if (deleted && !row.Deleted)
        {
            row.Deleted = true;
            row.Length = 0;
            row.ModifiedUtc = modified.UtcDateTime;
            row.Revision = revision;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ReplaceAssortmentAsync(int store, IReadOnlyCollection<string> skus, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var generation = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO `ProductImageAssortments` (`Store`, `Generation`) VALUES ({store}, {generation})
            ON DUPLICATE KEY UPDATE `Generation` = {generation}
            """, ct);
        await db.Set<AssortmentSku>().Where(x => x.Store == store).ExecuteDeleteAsync(ct);
        foreach (var batch in skus.Chunk(1000))
        {
            db.Set<AssortmentSku>().AddRange(batch.Select(sku => new AssortmentSku { Store = store, Sku = sku }));
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        await tx.CommitAsync(ct);
    }

    public async Task<ProductImageCatalogPage?> ReadAsync(int store, long after, int limit, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var generation = await db.Set<ReferenceAssortment>().Where(x => x.Store == store)
            .Select(x => (Guid?)x.Generation).SingleOrDefaultAsync(ct);
        if (generation is null) return null;

        var watermark = await db.Set<ReferenceRevision>().Where(x => x.Id == 1).Select(x => x.Value).SingleAsync(ct);
        var rows = await (from item in db.Set<CatalogReference>().AsNoTracking()
                          join sku in db.Set<AssortmentSku>().Where(x => x.Store == store) on item.Sku equals sku.Sku
                          where item.Revision > after
                          orderby item.Revision
                          select item).Take(limit + 1).ToListAsync(ct);
        await tx.CommitAsync(ct);
        var hasMore = rows.Count > limit;
        return new ProductImageCatalogPage(generation.Value,
            rows.Take(limit).Select(x => new ProductImageCatalogEntry(x.Revision, x.Sku, x.FileName, x.Deleted,
                x.Length, new DateTimeOffset(DateTime.SpecifyKind(x.ModifiedUtc, DateTimeKind.Utc)))).ToArray(),
            hasMore, hasMore ? rows[limit - 1].Revision : Math.Max(after, watermark));
    }

    internal static void Configure(ModelBuilder model)
    {
        model.Entity<ReferenceRevision>(e =>
        {
            e.ToTable("ProductImageRevisions").HasKey(x => x.Id);
            e.HasData(new ReferenceRevision { Id = 1 });
        });
        model.Entity<CatalogReference>(e =>
        {
            e.ToTable("ProductImageCatalog").HasKey(x => new { x.Sku, x.FileName });
            e.Property(x => x.Sku).HasMaxLength(128).UseCollation("ascii_general_ci").HasCharSet("ascii");
            e.Property(x => x.FileName).HasMaxLength(180).UseCollation("ascii_general_ci").HasCharSet("ascii");
            e.HasIndex(x => x.Revision).IsUnique();
        });
        model.Entity<ReferenceAssortment>(e => e.ToTable("ProductImageAssortments").HasKey(x => x.Store));
        model.Entity<AssortmentSku>(e =>
        {
            e.ToTable("ProductImageAssortmentSkus").HasKey(x => new { x.Store, x.Sku });
            e.Property(x => x.Sku).HasMaxLength(128).UseCollation("ascii_general_ci").HasCharSet("ascii");
        });
    }
}

internal sealed class ReferenceRevision { public int Id { get; set; } public long Value { get; set; } }
internal sealed class ReferenceAssortment { public int Store { get; set; } public Guid Generation { get; set; } }
internal sealed class AssortmentSku { public int Store { get; set; } public required string Sku { get; set; } }
internal sealed class CatalogReference
{
    public required string Sku { get; set; }
    public required string FileName { get; set; }
    public bool Deleted { get; set; }
    public long Length { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public long Revision { get; set; }
}
