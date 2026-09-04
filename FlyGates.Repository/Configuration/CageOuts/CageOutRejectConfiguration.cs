using FlyGates.Application.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutRejectConfiguration : IEntityTypeConfiguration<CageOutRejectDao>
{
    public void Configure(EntityTypeBuilder<CageOutRejectDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_reject_pkey");

        entity.ToTable("cage_out_reject");

        entity.Property(x => x.ProductCode)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Schedule)
            .IsRequired();

        entity.Property(x => x.CheckoutId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.ExpectedWeight)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        entity.Property(x => x.RealWeight)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        entity.Property(x => x.ProductImage)
            .HasColumnType("longtext")
            .IsRequired();

        entity.Property(x => x.ProductVideo)
            .HasColumnType("longtext")
            .IsRequired();

        entity.Property(x => x.Reason)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.IsResolved)
            .IsRequired();

        entity.Property(x => x.ResolvedAt);
    }
}
