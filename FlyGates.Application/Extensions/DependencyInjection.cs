using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Services.CageOuts.CageOutEmployees;
using FlyGates.Application.Services.CageOuts.CageOutRejects;
using Microsoft.Extensions.DependencyInjection;

namespace FlyGates.Application.Extensions;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICageOutEmployeeService, CageOutEmployeeService>();
        services.AddScoped<ICageOutRejectService, CageOutRejectService>();
    }
}

