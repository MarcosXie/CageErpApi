using FlyGates.Application.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration;

public class TotvsMockProdutoConfiguration : IEntityTypeConfiguration<TotvsMockProdutoDao>
{
    public void Configure(EntityTypeBuilder<TotvsMockProdutoDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("totvs_mock_produto_pkey");

        entity.ToTable("totvs_mock_produto");

        entity.Property(x => x.Nome)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.CodigoBarras)
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.Preco)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        entity.Property(x => x.PesoLiquido)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        entity.Property(x => x.PesoBruto)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        entity.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        entity.HasIndex(x => x.CodigoBarras)
            .IsUnique()
            .HasDatabaseName("ix_totvs_mock_produto_codigo_barras");
    }
}