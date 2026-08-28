using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Application.Services.CageOuts.CageOutClients;
using FlyGates.Application.Services.CageOuts.CageOutEmployees;
using FlyGates.Application.Services.CageOuts.CageOutRejects;
using FlyGates.Application.Services.CageOuts.CageOutTransactions;
using FlyGates.Application.Services.CageOuts.CageOutUnits;
using Microsoft.Extensions.DependencyInjection;

namespace FlyGates.Application.Extensions;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICageOutClientService, CageOutClientService>();
        services.AddScoped<ICageOutUnitService, CageOutUnitService>();
        services.AddScoped<ICageOutEmployeeService, CageOutEmployeeService>();
        services.AddScoped<ICageOutRejectService, CageOutRejectService>();
        services.AddScoped<ICageOutTransactionService, CageOutTransactionService>();
    }
}

