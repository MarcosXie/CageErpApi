using FlyGates.Application.Dao;
using FlyGates.Repository.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyGates.Controllers.TotvsConsincoMock;

/// <summary>
/// Mock do TOTVS Consinco SMProdutosAPI (v4) — usado para testes locais do
/// fluxo de self-checkout (busca de produto, preço, cadastro e tributação).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("SMProdutosAPI/api/v4/produtos")]
public class SMProdutosController(FlyGatesDbContext context) : ControllerBase
{
    private readonly FlyGatesDbContext _context = context;
    private static readonly IReadOnlyDictionary<string, ProdutoLegacyPerfil> LegacyProfiles =
        new Dictionary<string, ProdutoLegacyPerfil>(StringComparer.Ordinal)
        {
            ["06632956"] = new ProdutoLegacyPerfil(
                DescricaoFamilia: "EQUIPAMENTOS",
                FornecedorPrinc: "DISTRIBUIDORA INTERNA LTDA",
                IdFornecedorPrinc: 600,
                DescricaoMarca: "GENERICA",
                IdMarca: 100,
                PesoLiquido: 0.225m,
                PesoBruto: 0.240m,
                ClassificacaoComercial: "EQUIPAMENTOS",
                PrecoPromocao: 0m,
                DataInicioPromocao: null,
                DataFimPromocao: null
            ),
            ["4005808555345"] = new ProdutoLegacyPerfil(
                DescricaoFamilia: "PROTETOR SOLAR",
                FornecedorPrinc: "BEIERSDORF INDUSTRIA E COMERCIO LTDA",
                IdFornecedorPrinc: 601,
                DescricaoMarca: "NIVEA",
                IdMarca: 110,
                PesoLiquido: 0.220m,
                PesoBruto: 0.245m,
                ClassificacaoComercial: "PERFUMARIA",
                PrecoPromocao: 54.90m,
                DataInicioPromocao: "2026-06-15T00:00:00Z",
                DataFimPromocao: "2026-07-15T23:59:59Z"
            ),
            ["6452024011229"] = new ProdutoLegacyPerfil(
                DescricaoFamilia: "ELETRONICOS",
                FornecedorPrinc: "ELETRONICOS BRASIL DISTRIBUIDORA LTDA",
                IdFornecedorPrinc: 602,
                DescricaoMarca: "GENERIC CAM",
                IdMarca: 120,
                PesoLiquido: 0.520m,
                PesoBruto: 0.610m,
                ClassificacaoComercial: "ELETRONICOS",
                PrecoPromocao: 0m,
                DataInicioPromocao: null,
                DataFimPromocao: null
            )
        };

    [HttpGet]
    public async Task<IActionResult> GetProdutos()
    {
        var produtosDb = await _context.TotvsMockProdutos
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync();

        var produtos = produtosDb
            .Select(x =>
            {
                var perfil = BuildLegacyPerfil(x);
                return new ProdutoResponse(
                    x.Id,
                    x.Nome,
                    x.CodigoBarras,
                    x.Preco,
                    x.CreatedAt,
                    x.UpdatedAt,
                    perfil.PesoLiquido,
                    perfil.PesoBruto,
                    x.IsActive
                );
            })
            .ToList();

        return Ok(produtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProdutoById(Guid id)
    {
        var entity = await _context.TotvsMockProdutos
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (entity is null)
            return NotFound(new { Mensagem = "Produto nao encontrado." });

        var perfil = BuildLegacyPerfil(entity);
        var produto = new ProdutoResponse(
            entity.Id,
            entity.Nome,
            entity.CodigoBarras,
            entity.Preco,
            entity.CreatedAt,
            entity.UpdatedAt,
            perfil.PesoLiquido,
            perfil.PesoBruto,
            entity.IsActive
        );

        return Ok(produto);
    }

    [HttpGet("codigo-barras/{codigoBarras}")]
    public async Task<IActionResult> GetProdutoByCodigoBarras(string codigoBarras)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return BadRequest(new { Mensagem = "Codigo de barras e obrigatorio." });

        var codigoNormalizado = codigoBarras.Trim();
        var entity = await _context.TotvsMockProdutos
            .AsNoTracking()
            .Where(x => x.CodigoBarras == codigoNormalizado)
            .FirstOrDefaultAsync();

        if (entity is null)
            return NotFound(new { Mensagem = "Produto nao encontrado." });

        var perfil = BuildLegacyPerfil(entity);
        var produto = new ProdutoResponse(
            entity.Id,
            entity.Nome,
            entity.CodigoBarras,
            entity.Preco,
            entity.CreatedAt,
            entity.UpdatedAt,
            perfil.PesoLiquido,
            perfil.PesoBruto,
            entity.IsActive
        );

        return Ok(produto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduto([FromBody] CreateProdutoRequest request)
    {
        var validationError = ValidateRequest(request.Nome, request.CodigoBarras, request.Preco, request.PesoLiquido, request.PesoBruto);
        if (validationError is not null)
            return BadRequest(new { Mensagem = validationError });

        var codigoBarras = request.CodigoBarras.Trim();
        var exists = await _context.TotvsMockProdutos.AnyAsync(x => x.CodigoBarras == codigoBarras);
        if (exists)
            return Conflict(new { Mensagem = "Ja existe produto com este codigo de barras." });

        var now = DateTime.UtcNow;
        var entity = new TotvsMockProdutoDao
        {
            Nome = request.Nome.Trim(),
            CodigoBarras = codigoBarras,
            Preco = request.Preco,
            PesoLiquido = request.PesoLiquido,
            PesoBruto = request.PesoBruto,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.TotvsMockProdutos.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProdutoById),
            new { id = entity.Id },
            new ProdutoResponse(
                entity.Id,
                entity.Nome,
                entity.CodigoBarras,
                entity.Preco,
                entity.CreatedAt,
                entity.UpdatedAt,
                BuildLegacyPerfil(entity).PesoLiquido,
                BuildLegacyPerfil(entity).PesoBruto,
                entity.IsActive
            ));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduto(Guid id, [FromBody] UpdateProdutoRequest request)
    {
        var validationError = ValidateRequest(request.Nome, request.CodigoBarras, request.Preco, request.PesoLiquido, request.PesoBruto);
        if (validationError is not null)
            return BadRequest(new { Mensagem = validationError });

        var entity = await _context.TotvsMockProdutos.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return NotFound(new { Mensagem = "Produto nao encontrado." });

        var codigoBarras = request.CodigoBarras.Trim();
        var exists = await _context.TotvsMockProdutos.AnyAsync(x => x.CodigoBarras == codigoBarras && x.Id != id);
        if (exists)
            return Conflict(new { Mensagem = "Ja existe produto com este codigo de barras." });

        entity.Nome = request.Nome.Trim();
        entity.CodigoBarras = codigoBarras;
        entity.Preco = request.Preco;
        entity.PesoLiquido = request.PesoLiquido;
        entity.PesoBruto = request.PesoBruto;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new ProdutoResponse(
            entity.Id,
            entity.Nome,
            entity.CodigoBarras,
            entity.Preco,
            entity.CreatedAt,
            entity.UpdatedAt,
            BuildLegacyPerfil(entity).PesoLiquido,
            BuildLegacyPerfil(entity).PesoBruto,
            entity.IsActive
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduto(Guid id)
    {
        var entity = await _context.TotvsMockProdutos.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return NotFound(new { Mensagem = "Produto nao encontrado." });

        _context.TotvsMockProdutos.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("precos-produtos")]
    public async Task<IActionResult> GetPrecosProdutos([FromQuery] ProdutoQueryParams p)
    {
        const int numeroEmpresaPadrao = 1;
        const int numeroSegmentoPadrao = 1;

        var produtos = await QueryProdutosBase(p)
            .OrderBy(x => x.Nome)
            .ToListAsync();

        var data = produtos
            .Select(produto =>
            {
                var perfil = BuildLegacyPerfil(produto);
                var idProduto = BuildIdProduto(produto.CodigoBarras);
                return new PrecoProdutoResponse(
                    numeroEmpresaPadrao,
                    produto.Nome,
                    numeroSegmentoPadrao,
                    produto.Preco,
                    1,
                    0m,
                    0,
                    perfil.DataInicioPromocao,
                    perfil.DataFimPromocao,
                    perfil.PrecoPromocao,
                    [],
                    produto.UpdatedAt,
                    "A",
                    perfil.ClassificacaoComercial,
                    produto.CodigoBarras,
                    idProduto,
                    idProduto,
                    idProduto,
                    produto.UpdatedAt
                );
            })
            .Where(x => !p.NroEmpresa.HasValue || x.NumeroEmpresa == p.NroEmpresa.Value)
            .Where(x => !p.NumeroSegmento.HasValue || x.NumeroSegmento == p.NumeroSegmento.Value)
            .Where(x => string.IsNullOrWhiteSpace(p.StatusVenda) || x.StatusVenda.Equals(p.StatusVenda, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(data);
    }

    [HttpGet("codigos-acesso-prod")]
    public async Task<IActionResult> GetCodigosAcessoProd([FromQuery] ProdutoQueryParams p)
    {
        var produtos = await QueryProdutosBase(p)
            .OrderBy(x => x.Nome)
            .ToListAsync();

        var data = produtos
            .Select(produto =>
            {
                var idProduto = BuildIdProduto(produto.CodigoBarras);
                return new CodigoAcessoProdutoResponse(
                    [new CodigoAcessoItemResponse(produto.CodigoBarras, "E", 1, "A", null)],
                    produto.CodigoBarras,
                    idProduto,
                    idProduto,
                    idProduto,
                    produto.UpdatedAt
                );
            })
            .ToList();

        return Ok(data);
    }

    [HttpGet("dados-cadastrais")]
    public async Task<IActionResult> GetDadosCadastrais([FromQuery] ProdutoQueryParams p)
    {
        var produtos = await QueryProdutosBase(p)
            .OrderBy(x => x.Nome)
            .ToListAsync();

        var data = produtos
            .Select(produto =>
            {
                var perfil = BuildLegacyPerfil(produto);
                var idProduto = BuildIdProduto(produto.CodigoBarras);
                return new DadosCadastraisProdutoResponse(
                    produto.CreatedAt,
                    produto.Nome,
                    produto.Nome,
                    produto.Nome,
                    string.Empty,
                    $"Produto cadastrado via mock: {produto.Nome}",
                    string.Empty,
                    perfil.DescricaoFamilia,
                    0,
                    0,
                    0,
                    "S",
                    "N",
                    "N",
                    string.Empty,
                    string.Empty,
                    "N",
                    1,
                    perfil.IdFornecedorPrinc,
                    perfil.FornecedorPrinc,
                    perfil.IdMarca,
                    perfil.DescricaoMarca,
                    "A",
                    [],
                    new { },
                    produto.Nome,
                    produto.Nome,
                    perfil.PesoLiquido,
                    perfil.PesoBruto,
                    "KG",
                    produto.CodigoBarras,
                    idProduto,
                    idProduto,
                    idProduto,
                    produto.UpdatedAt
                );
            })
            .ToList();

        return Ok(data);
    }

    [HttpGet("tributacao-produtos")]
    public async Task<IActionResult> GetTributacaoProdutos([FromQuery] ProdutoQueryParams p)
    {
        const int numeroEmpresaPadrao = 1;

        var produtos = await QueryProdutosBase(p)
            .OrderBy(x => x.Nome)
            .ToListAsync();

        var data = produtos
            .Select(produto =>
            {
                var idProduto = BuildIdProduto(produto.CodigoBarras);
                return new TributacaoProdutoResponse(
                    numeroEmpresaPadrao,
                    "SP",
                    p.UfClienteFornecedor ?? "SP",
                    "NORMAL",
                    1,
                    200,
                    "SN",
                    "000",
                    100.0m,
                    0.0m,
                    18.0m,
                    1.65m,
                    7.60m,
                    0.0m,
                    0.0m,
                    0,
                    0.0m,
                    0.0m,
                    0.0m,
                    0.0m,
                    0.0m,
                    "N",
                    "N",
                    "N",
                    produto.CodigoBarras,
                    idProduto,
                    idProduto,
                    idProduto,
                    produto.UpdatedAt
                );
            })
            .Where(x => !p.NroEmpresa.HasValue || x.NumeroEmpresa == p.NroEmpresa.Value)
            .Where(x => string.IsNullOrWhiteSpace(p.TipoTributacao) || x.TipoTributacao.Equals(p.TipoTributacao, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(data);
    }

    private static string? ValidateRequest(string? nome, string? codigoBarras, decimal preco, decimal pesoLiquido, decimal pesoBruto)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "Nome e obrigatorio.";
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return "Codigo de barras e obrigatorio.";
        if (preco <= 0)
            return "Preco deve ser maior que zero.";
        if (pesoLiquido <= 0)
            return "Peso liquido deve ser maior que zero.";
        if (pesoBruto <= 0)
            return "Peso bruto deve ser maior que zero.";

        return null;
    }

    private IQueryable<TotvsMockProdutoDao> QueryProdutosBase(ProdutoQueryParams p)
    {
        var query = _context.TotvsMockProdutos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(p.CodigoAcesso))
        {
            var codigoBarras = p.CodigoAcesso.Trim();
            query = query.Where(x => x.CodigoBarras == codigoBarras);
        }

        if (p.IdProduto.HasValue)
        {
            var idProduto = p.IdProduto.Value;
            query = query.Where(x => BuildIdProduto(x.CodigoBarras) == idProduto);
        }

        if (!string.IsNullOrWhiteSpace(p.Descricao))
        {
            var descricao = p.Descricao.Trim();
            query = query.Where(x => x.Nome.Contains(descricao));
        }

        if (!string.IsNullOrWhiteSpace(p.DescricaoProduto))
        {
            var descricaoProduto = p.DescricaoProduto.Trim();
            query = query.Where(x => x.Nome.Contains(descricaoProduto));
        }

        return query;
    }

    private static int BuildIdProduto(string codigoBarras)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in codigoBarras)
                hash = (hash * 31) + c;

            return hash == int.MinValue ? 1 : Math.Abs(hash);
        }
    }

    private static ProdutoLegacyPerfil BuildLegacyPerfil(TotvsMockProdutoDao produto)
    {
        if (LegacyProfiles.TryGetValue(produto.CodigoBarras, out var perfil))
            return perfil with { PesoLiquido = produto.PesoLiquido, PesoBruto = produto.PesoBruto };

        return new ProdutoLegacyPerfil(
            DescricaoFamilia: "GERAL",
            FornecedorPrinc: string.Empty,
            IdFornecedorPrinc: 0,
            DescricaoMarca: string.Empty,
            IdMarca: 0,
            PesoLiquido: produto.PesoLiquido,
            PesoBruto: produto.PesoBruto,
            ClassificacaoComercial: "GERAL",
            PrecoPromocao: 0m,
            DataInicioPromocao: null,
            DataFimPromocao: null
        );
    }

    public record CreateProdutoRequest(string Nome, string CodigoBarras, decimal Preco, decimal PesoLiquido, decimal PesoBruto, bool IsActive = true);
    public record UpdateProdutoRequest(string Nome, string CodigoBarras, decimal Preco, decimal PesoLiquido, decimal PesoBruto, bool IsActive = true);
    public record ProdutoResponse(
        Guid Id,
        string Nome,
        string CodigoBarras,
        decimal Preco,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        decimal PesoLiquido,
        decimal PesoBruto,
        bool IsActive
    );

    public record PrecoProdutoResponse(
        int NumeroEmpresa,
        string DescCompleta,
        int NumeroSegmento,
        decimal PrecoVenda,
        int Embalagem,
        decimal PrecoVendaCaixa,
        int QuantidadeCaixa,
        string? DataInicioPromocao,
        string? DataFimPromocao,
        decimal PrecoPromocao,
        List<object> PromocaoAPartir,
        DateTime DataAtualizacao,
        string StatusVenda,
        string ClassificacaoComercial,
        string CodigoAcessoPrincipal,
        int IdProduto,
        int IdFamilia,
        int IdProdutoBase,
        DateTime DataUltimaAtualizacao
    );

    public record CodigoAcessoProdutoResponse(
        List<CodigoAcessoItemResponse> CodigosAcessos,
        string CodigoAcessoPrincipal,
        int IdProduto,
        int IdFamilia,
        int IdProdutoBase,
        DateTime DataUltimaAtualizacao
    );

    public record CodigoAcessoItemResponse(
        string CodigoAcesso,
        string TipoCodigo,
        int QtdEmbalagem,
        string Status,
        string? DataExclusao
    );

    public record DadosCadastraisProdutoResponse(
        DateTime DtaInclusao,
        string DescricaoCompleta,
        string DescricaoGenerica,
        string DescricaoReduzida,
        string DescricaoComposicao,
        string DetalhesProduto,
        string ReferenciaFabricante,
        string DescricaoFamilia,
        int ValidadeDias,
        int ValidadeMes,
        int NroDiasValidadeDeRecebto,
        string ProcessoFabricacao,
        string PermiteDecimal,
        string Pesavel,
        string UrlEcommerce,
        string UrlAlternativa,
        string IntegraEcommerce,
        int MultiploVendaEcommerce,
        int IdFornecedorPrinc,
        string FornecedorPrinc,
        int IdMarca,
        string DescricaoMarca,
        string StatusVenda,
        List<object> PrincipiosAtivos,
        object RetornoCustomizados,
        string NomeEcommerce,
        string DescEcommerce,
        decimal PesoLiquido,
        decimal PesoBruto,
        string UnidadeMedidaPeso,
        string CodigoAcessoPrincipal,
        int IdProduto,
        int IdFamilia,
        int IdProdutoBase,
        DateTime DataUltimaAtualizacao
    );

    private record ProdutoLegacyPerfil(
        string DescricaoFamilia,
        string FornecedorPrinc,
        int IdFornecedorPrinc,
        string DescricaoMarca,
        int IdMarca,
        decimal PesoLiquido,
        decimal PesoBruto,
        string ClassificacaoComercial,
        decimal PrecoPromocao,
        string? DataInicioPromocao,
        string? DataFimPromocao
    );

    public record TributacaoProdutoResponse(
        int NumeroEmpresa,
        string UFEmpresa,
        string UFDestino,
        string RegimeTributacao,
        int NumeroDivisao,
        int NumeroTributacao,
        string TipoTributacao,
        string CstIcms,
        decimal PercentualICMS,
        decimal PercentualIsentoICMS,
        decimal AliquotaICMS,
        decimal AliquotaPIS,
        decimal AliquotaCofins,
        decimal MVA,
        decimal AliquotaICMSST,
        int ReducaoBaseICMSST,
        decimal ValorPautaICMSST,
        decimal AliquotaIPI,
        decimal AliquotaIPISaida,
        decimal ValorPautaIPI,
        decimal AliquotaFCPICMS,
        string UsaPMCBaseST,
        string STConformeEntrada,
        string SomaIPInoST,
        string CodigoAcessoPrincipal,
        int IdProduto,
        int IdFamilia,
        int IdProdutoBase,
        DateTime DataUltimaAtualizacao
    );
}
