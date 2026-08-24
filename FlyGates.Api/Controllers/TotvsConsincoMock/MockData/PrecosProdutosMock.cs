namespace FlyGates.Controllers.TotvsConsincoMock.MockData;

public static class PrecosProdutosMock
{
    public static List<object> Get() =>
    [
        new
        {
            NumeroEmpresa = 1,
            DescCompleta = "CAIXA LEITOR DE CODIGO DE BARRAS",
            NumeroSegmento = 1,
            PrecoVenda = 89.90m,
            Embalagem = 1,
            PrecoVendaCaixa = 0m,
            QuantidadeCaixa = 0,
            DataInicioPromocao = (string?)null,
            DataFimPromocao = (string?)null,
            PrecoPromocao = 0m,
            PromocaoAPartir = new List<object>(),
            DataAtualizacao = "2026-06-20T09:00:00Z",
            StatusVenda = "A",
            ClassificacaoComercial = "EQUIPAMENTOS",
            CodigoAcessoPrincipal = "06632956",
            IdProduto = 2001,
            IdFamilia = 100,
            IdProdutoBase = 2001,
            DataUltimaAtualizacao = "2026-06-20T09:00:00Z"
        },
        new
        {
            NumeroEmpresa = 1,
            DescCompleta = "PROTETOR SOLAR NIVEA SUN FPS50 200ML",
            NumeroSegmento = 1,
            PrecoVenda = 65.90m,
            Embalagem = 1,
            PrecoVendaCaixa = 395.40m,
            QuantidadeCaixa = 6,
            DataInicioPromocao = "2026-06-15T00:00:00Z",
            DataFimPromocao = "2026-07-15T23:59:59Z",
            PrecoPromocao = 54.90m,
            PromocaoAPartir = new List<object>(),
            DataAtualizacao = "2026-06-20T09:10:00Z",
            StatusVenda = "A",
            ClassificacaoComercial = "PERFUMARIA",
            CodigoAcessoPrincipal = "4005808555345",
            IdProduto = 2002,
            IdFamilia = 110,
            IdProdutoBase = 2002,
            DataUltimaAtualizacao = "2026-06-20T09:10:00Z"
        },
        new
        {
            NumeroEmpresa = 1,
            DescCompleta = "CAMERA DIGITAL COMPACTA 20MP",
            NumeroSegmento = 1,
            PrecoVenda = 1299.00m,
            Embalagem = 1,
            PrecoVendaCaixa = 0m,
            QuantidadeCaixa = 0,
            DataInicioPromocao = (string?)null,
            DataFimPromocao = (string?)null,
            PrecoPromocao = 0m,
            PromocaoAPartir = new List<object>(),
            DataAtualizacao = "2026-06-20T09:20:00Z",
            StatusVenda = "A",
            ClassificacaoComercial = "ELETRONICOS",
            CodigoAcessoPrincipal = "6452024011229",
            IdProduto = 2003,
            IdFamilia = 120,
            IdProdutoBase = 2003,
            DataUltimaAtualizacao = "2026-06-20T09:20:00Z"
        }
    ];
}
