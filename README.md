# CageErpApi

API recortada a partir de `FlyGates-API`, mantendo somente:

- Mock TOTVS Consinco (`SMProdutosAPI` e `SMVendasAPI`)
- CageOuts (`CageOutEmployee` e `CageOutReject`)
- Camadas `Api`, `Application`, `Domain`, `Infrastructure` e `Repository`
- Banco MySQL com Entity Framework Core e migration inicial própria

## Estrutura

```text
FlyGates.Api          Controllers, Swagger, CORS e middleware de erro
FlyGates.Application  Servicos CageOuts e utilitarios
FlyGates.Domain       Entidades, DTOs, DAOs, interfaces e excecoes
FlyGates.Infrastructure Camada base para integracoes futuras
FlyGates.Repository   DbContext, configuracoes EF, repositorios e migrations
```

## Banco local

O `appsettings.json` aponta para um MySQL local:

```text
server=localhost;port=3306;database=cageouts_db;user=root;password=CHANGE_ME
```

Ajuste a senha em `FlyGates.Api/appsettings.Development.json` ou use variavel de ambiente `ConnectionStrings__DefaultConnection`.

O deploy de VPS usa o MySQL compartilhado da rede Docker `flygates_net` e a porta `8093`.

Arquivos de deploy:

```text
Dockerfile
deploy/docker-compose.yml
deploy/nginx.conf
deploy/.env.example
.github/workflows/deploy-vps.yml
```

Para subir o compose de produção na VPS:

```powershell
docker compose -f .\deploy\docker-compose.yml up -d
```

Para aplicar as migrations:

```powershell
dotnet ef database update --project .\FlyGates.Repository\FlyGates.Repository.csproj --startup-project .\FlyGates.Api\FlyGates.Api.csproj --context FlyGatesDbContext
```

## Executar

```powershell
dotnet build .\CageErpApi.sln
dotnet run --project .\FlyGates.Api\FlyGates.Api.csproj
```

Swagger fica em `/swagger` e healthcheck em `/health`.
