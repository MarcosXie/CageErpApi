namespace FlyGates.Controllers.TotvsConsincoMock.MockData;

public static class CodigosAcessoMock
{
    public static List<object> Get() =>
    [
        new
        {
            CodigosAcessos = new List<object>
            {
                new { CodigoAcesso = "06632956", TipoCodigo = "I", QtdEmbalagem = 1, Status = "A", DataExclusao = (string?)null }
            },
            CodigoAcessoPrincipal = "06632956",
            IdProduto = 2001,
            IdFamilia = 100,
            IdProdutoBase = 2001,
            DataUltimaAtualizacao = "2026-06-20T09:00:00Z"
        },
        new
        {
            CodigosAcessos = new List<object>
            {
                new { CodigoAcesso = "4005808555345", TipoCodigo = "E", QtdEmbalagem = 1, Status = "A", DataExclusao = (string?)null }
            },
            CodigoAcessoPrincipal = "4005808555345",
            IdProduto = 2002,
            IdFamilia = 110,
            IdProdutoBase = 2002,
            DataUltimaAtualizacao = "2026-06-20T09:10:00Z"
        },
        new
        {
            CodigosAcessos = new List<object>
            {
                new { CodigoAcesso = "6452024011229", TipoCodigo = "E", QtdEmbalagem = 1, Status = "A", DataExclusao = (string?)null }
            },
            CodigoAcessoPrincipal = "6452024011229",
            IdProduto = 2003,
            IdFamilia = 120,
            IdProdutoBase = 2003,
            DataUltimaAtualizacao = "2026-06-20T09:20:00Z"
        }
    ];
}
