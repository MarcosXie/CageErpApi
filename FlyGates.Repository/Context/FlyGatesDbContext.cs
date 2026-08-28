using FlyGates.Application.Dao;
using FlyGates.Domain.Dao;
using FlyGates.Repository.Configuration;
using FlyGates.Repository.Configuration.CageOuts;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Repository.Context;

// Alteração aqui: Receba DbContextOptions e repasse para o base
public class FlyGatesDbContext(DbContextOptions<FlyGatesDbContext> options) : DbContext(options)
{
    public virtual DbSet<TotvsMockProdutoDao> TotvsMockProdutos { get; set; }
    public virtual DbSet<CageOutClientDao> CageOutClients { get; set; }
    public virtual DbSet<CageOutUnitDao> CageOutUnits { get; set; }
    public virtual DbSet<CageOutEmployeeDao> CageOutEmployees { get; set; }
    public virtual DbSet<CageOutRejectDao> CageOutRejects { get; set; }
    public virtual DbSet<CageOutTransactionDao> CageOutTransactions { get; set; }
    public virtual DbSet<CageOutTransactionItemDao> CageOutTransactionItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TotvsMockProdutoConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutClientConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutUnitConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutEmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutRejectConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CageOutTransactionItemConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}