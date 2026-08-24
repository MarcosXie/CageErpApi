using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.TotvsConsincoMock;

public record ProdutoQueryParams(
    [FromQuery(Name = "modelo.idProduto")] int? IdProduto = null,
    [FromQuery(Name = "modelo.codigoAcesso")] string? CodigoAcesso = null,
    [FromQuery(Name = "modelo.descricao")] string? Descricao = null,
    [FromQuery(Name = "modelo.descricaoProduto")] string? DescricaoProduto = null,
    [FromQuery(Name = "modelo.CNPJEmpresa")] string? CnpjEmpresa = null,
    [FromQuery(Name = "modelo.nroEmpresa")] int? NroEmpresa = null,
    [FromQuery(Name = "modelo.numeroSegmento")] int? NumeroSegmento = null,
    [FromQuery(Name = "modelo.statusVenda")] string? StatusVenda = null,
    [FromQuery(Name = "modelo.tipoTributacao")] string? TipoTributacao = null,
    [FromQuery(Name = "modelo.uFClienteFornecedor")] string? UfClienteFornecedor = null,
    [FromQuery(Name = "modelo._pageNo")] int? PageNo = null,
    [FromQuery(Name = "modelo._pageSize")] int? PageSize = null
);
