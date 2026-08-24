using FlyGates.Application.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlyGates.Repository.Configuration.CageOuts;

public class CageOutEmployeeConfiguration : IEntityTypeConfiguration<CageOutEmployeeDao>
{
    public void Configure(EntityTypeBuilder<CageOutEmployeeDao> entity)
    {
        entity.HasKey(x => x.Id).HasName("cage_out_employee_pkey");

        entity.ToTable("cage_out_employee");

        entity.Property(x => x.Name)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(x => x.BadgeCode)
            .HasMaxLength(80)
            .IsRequired();

        entity.Property(x => x.Password)
            .HasMaxLength(512)
            .IsRequired();

        entity.Property(x => x.FingerprintData)
            .HasColumnType("longtext")
            .IsRequired();

        // Stored as JSON text; longtext avoids Pomelo intercepting writes before the converter runs.
        entity.Property(x => x.AllowedProcedures)
            .HasColumnType("longtext")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>()
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            ));

        entity.HasIndex(x => x.BadgeCode)
            .HasDatabaseName("ix_cage_out_employee_badge_code");
    }
}
