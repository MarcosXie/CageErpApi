using FlyGates.Domain.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutIdSequenceConfiguration : IEntityTypeConfiguration<CageOutIdSequenceDao>
{
    public void Configure(EntityTypeBuilder<CageOutIdSequenceDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_id_sequence_pkey");
        entity.ToTable("cage_out_id_sequence");

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.NextSequenceNumber)
            .IsRequired();
    }
}
