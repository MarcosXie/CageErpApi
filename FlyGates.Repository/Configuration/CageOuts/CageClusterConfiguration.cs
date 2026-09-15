using FlyGates.Domain.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageClusterConfiguration : IEntityTypeConfiguration<CageClusterDao>
{
    public void Configure(EntityTypeBuilder<CageClusterDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_cluster_pkey");

        entity.ToTable("cage_cluster");

        entity.Property(x => x.UnitId)
            .IsRequired();

        entity.Property(x => x.Name)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.UnitId, x.Code })
            .IsUnique()
            .HasDatabaseName("idx_cage_cluster_unit_code_unique");

        entity.HasIndex(x => x.UnitId)
            .HasDatabaseName("idx_cage_cluster_unit_id");

        entity.HasOne<CageOutUnitDao>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cage_cluster_unit");
    }
}