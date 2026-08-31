using FlyGates.Domain.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutUnitConfiguration : IEntityTypeConfiguration<CageOutUnitDao>
{
    public void Configure(EntityTypeBuilder<CageOutUnitDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_unit_pkey");

        entity.ToTable("cage_out_unit");

        entity.Property(x => x.Name)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.ClientId)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(120);

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("idx_cage_out_unit_code_unique");

        entity.HasIndex(x => x.ClientId)
            .HasDatabaseName("idx_cage_out_unit_client_id");
    }
}
