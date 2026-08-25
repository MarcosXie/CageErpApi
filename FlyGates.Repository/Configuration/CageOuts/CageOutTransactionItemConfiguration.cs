using FlyGates.Application.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutTransactionItemConfiguration : IEntityTypeConfiguration<CageOutTransactionItemDao>
{
    public void Configure(EntityTypeBuilder<CageOutTransactionItemDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("transaction_items_pkey");
        entity.ToTable("transaction_items");

        entity.Property(x => x.ProductCode).HasMaxLength(100).IsRequired();
        entity.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Quantity).IsRequired();
        entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(x => x.Subtotal).HasColumnType("decimal(18,2)").IsRequired();

        entity.HasIndex(x => x.TransactionId)
            .HasDatabaseName("ix_transaction_items_transaction_id");
    }
}