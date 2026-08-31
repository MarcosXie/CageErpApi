using FlyGates.Domain.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutClientConfiguration : IEntityTypeConfiguration<CageOutClientDao>
{
    public void Configure(EntityTypeBuilder<CageOutClientDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_client_pkey");

        entity.ToTable("cage_out_client");

        entity.Property(x => x.Name)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(120)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("idx_cage_out_client_email_unique");
    }
}
