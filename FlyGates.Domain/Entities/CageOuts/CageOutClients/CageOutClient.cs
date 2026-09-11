using FlyGates.Application.Entities.Shared;

namespace FlyGates.Application.Entities.CageOuts.CageOutClients;

public class CageOutClient : BaseModel
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Chave S3 da imagem de fundo da tela Idle do CageOuts; null usa o layout padrão (logo).</summary>
    public string? BackgroundImageKey { get; set; }
}
