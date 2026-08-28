using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Repository.Context;
using FlyGates.Repository.Mapper;
using FlyGates.Repository.Repositories.CageOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlyGates.Repository.Extensions;

public static class DependencyInjection
{
    public static void AddRepository(this IServiceCollection services, IConfiguration configuration)
    {
        //Mappers
        services.SetProfileBuilder(_ => new DaoMapper());
        services.SetProfileBuilder(_ => new DomainToDto());
        services.SetProfileBuilder(_ => new DtoToDomain());
        services.CreateMappers();
	       
        // Context
        services.AddDbContext<FlyGatesDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
        
            options.UseMySql(
                connectionString, 
                new MySqlServerVersion(new Version(8, 0, 37))
            )
            .LogTo(Console.WriteLine, LogLevel.Warning);
        });
        
        // Repositories
        services.AddScoped<ICageOutClientRepository, CageOutClientRepository>();
        services.AddScoped<ICageOutUnitRepository, CageOutUnitRepository>();
        services.AddScoped<ICageOutEmployeeRepository, CageOutEmployeeRepository>();
        services.AddScoped<ICageOutRejectRepository, CageOutRejectRepository>();
        services.AddScoped<ICageOutTransactionRepository, CageOutTransactionRepository>();
    }
}