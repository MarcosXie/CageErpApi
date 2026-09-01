using FlyGates.Application.Dao.Shared;

namespace FlyGates.Application.Dao;

public class TotvsMockProdutoDao : BaseDao
{
    public string Nome { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public decimal PesoLiquido { get; set; }
    public decimal PesoBruto { get; set; }
    public bool IsActive { get; set; } = true;
}