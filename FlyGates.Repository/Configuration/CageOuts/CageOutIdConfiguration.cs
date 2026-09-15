using FlyGates.Domain.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutIdConfiguration : IEntityTypeConfiguration<CageOutIdDao>
{
    public void Configure(EntityTypeBuilder<CageOutIdDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_id_pkey");
        entity.ToTable("cage_out_id");

        entity.Property(x => x.UnitId).IsRequired();
        entity.Property(x => x.CageClusterId);
        entity.Property(x => x.Identifier).HasMaxLength(80).IsRequired();
        entity.Property(x => x.IsActive).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired();

        entity.Property(x => x.BoundAt);

        entity.HasIndex(x => x.Identifier)
            .IsUnique()
            .HasDatabaseName("idx_cage_out_id_identifier_unique");
        entity.HasIndex(x => x.UnitId)
            .HasDatabaseName("idx_cage_out_id_unit_id");
        entity.HasIndex(x => x.CageClusterId)
            .HasDatabaseName("idx_cage_out_id_cage_cluster_id");

        entity.HasOne<CageOutUnitDao>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cage_out_id_unit");

        entity.HasOne<CageClusterDao>()
            .WithMany()
            .HasForeignKey(x => x.CageClusterId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_cage_out_id_cage_cluster");
    }
}