using FlyGates.Application.Extensions;
using FlyGates.Application.Services.CageOuts.CageClusters;
using FlyGates.Application.Services.CageOuts.CageOutIds;
using FlyGates.Api.Hubs;
using FlyGates.Infraestructure.Extensions;
using FlyGates.Middlewares;
using FlyGates.Repository.Extensions;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IServiceCollection services = builder.Services;
ConfigurationManager config = builder.Configuration;
config.AddEnvironmentVariables();

services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:5174",
                "https://cageouts.com.br",
                "https://www.cageouts.com.br"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition")
            .AllowCredentials();
    });
});

services.AddControllers();
services.AddSignalR();
services.AddHealthChecks();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CageErpApi", Version = "v1" });
});

services.AddHttpContextAccessor();
services.AddApplication();
services.AddSingleton<ICageClusterStatusNotifier, CageClusterStatusNotifier>();
services.AddSingleton<ICageOutLiveSessionNotifier, CageOutLiveSessionNotifier>();
services.AddRepository(config);
services.AddInfrastructure(config);
services.AddHttpClient();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CageErpApi v1");
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("CorsPolicy");
app.UseAuthorization();

app.MapControllers();
app.MapHub<CageClusterMonitorHub>("/hubs/cagecluster");
app.MapHub<CageOutLiveSessionHub>("/hubs/cageouts/live-session");
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();