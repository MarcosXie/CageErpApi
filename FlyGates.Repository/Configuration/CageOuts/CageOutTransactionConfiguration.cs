using FlyGates.Application.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutTransactionConfiguration : IEntityTypeConfiguration<CageOutTransactionDao>
{
    public void Configure(EntityTypeBuilder<CageOutTransactionDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("transactions_pkey");
        entity.ToTable("transactions");

        entity.Property(x => x.ClientTransactionId).IsRequired();
        entity.Property(x => x.CheckoutId).HasMaxLength(100).IsRequired();
        entity.Property(x => x.CompletedAt).IsRequired();
        entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(x => x.ItemCount).IsRequired();

        entity.HasIndex(x => x.ClientTransactionId)
            .IsUnique()
            .HasDatabaseName("ux_transactions_client_transaction_id");
        entity.HasIndex(x => x.CompletedAt)
            .HasDatabaseName("ix_transactions_completed_at");

        entity.HasMany(x => x.Items)
            .WithOne(x => x.Transaction)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}