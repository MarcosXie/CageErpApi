using FlyGates.Application.Dao;
using FlyGates.Repository.Configuration;
using FlyGates.Repository.Configuration.CageOuts;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Context;

// Alteração aqui: Receba DbContextOptions e repasse para o base
public class FlyGatesDbContext(DbContextOptions<FlyGatesDbContext> options) : DbContext(options)
{
    public virtual DbSet<TotvsMockProdutoDao> TotvsMockProdutos { get; set; }
    public virtual DbSet<CageOutEmployeeDao> CageOutEmployees { get; set; }
    public virtual DbSet<CageOutRejectDao> CageOutRejects { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TotvsMockProdutoConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutEmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutRejectConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}