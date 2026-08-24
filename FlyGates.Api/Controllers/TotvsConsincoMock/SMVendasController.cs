using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlyGates.Controllers.TotvsConsincoMock;

/// <summary>
/// Mock do TOTVS Consinco SMVendasAPI (v1) — registra a venda no ERP após
/// o pagamento TEF no fluxo de self-checkout.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("SMVendasAPI/api/v1/vendas")]
public class SMVendasController : ControllerBase
{
    [HttpGet]
    public IActionResult GetVendas()
    {
        return Ok(new[]
        {
            new
            {
                IdVenda = 9001,
                NumeroEmpresa = 1,
                NumeroPDV = 50,
                NumeroCupom = 123456,
                DataVenda = "2026-05-06T10:30:00Z",
                ValorTotal = 41.38m,
                StatusVenda = "F"
            }
        });
    }

    [HttpPost]
    public IActionResult PostVendas()
    {
        return Ok(new
        {
            IdVenda = Random.Shared.Next(10000, 99999),
            Mensagem = "Venda registrada com sucesso (mock)"
        });
    }
}
