using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlyGates.Repository.Context;

public class FlyGatesDbContextFactory : IDesignTimeDbContextFactory<FlyGatesDbContext>
{
    public FlyGatesDbContext CreateDbContext(string[] args)
    {
        // Calcula o caminho base subindo um nível e entrando na pasta FlyGates.Api
        var currentDirectory = Directory.GetCurrentDirectory();
        var parentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        if (string.IsNullOrEmpty(parentDirectory))
        {
            throw new InvalidOperationException("Não foi possível determinar o diretório pai do diretório atual.");
        }

        var basePath = Path.Combine(parentDirectory, "FlyGates.Api");
        var appsettingsPath = Path.Combine(basePath, "appsettings.json");
        
        if (!File.Exists(appsettingsPath))
        {
            throw new FileNotFoundException($"O arquivo appsettings.json não foi encontrado no caminho: {appsettingsPath}");
        }
        
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<FlyGatesDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 2. Configura o MySQL sem AutoDetect para permitir gerar migrations offline
        builder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 37)));

        // 3. Retorna o Contexto pronto
        return new FlyGatesDbContext(builder.Options);
    }
}