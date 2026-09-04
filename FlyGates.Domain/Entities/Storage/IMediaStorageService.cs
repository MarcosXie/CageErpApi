namespace FlyGates.Application.Entities.Storage;

/// <summary>Abstração sobre o armazenamento de mídia (S3) usada pela camada de aplicação.</summary>
public interface IMediaStorageService
{
    /// <summary>Gera uma URL temporária de leitura para a chave informada; retorna null se a chave for vazia.</summary>
    string? GeneratePresignedUrl(string? objectKey);

    /// <summary>Remove o objeto do bucket; não falha se a chave for vazia ou o objeto não existir.</summary>
    Task DeleteObjectAsync(string? objectKey, CancellationToken cancellationToken = default);

    /// <summary>Sincroniza a regra de expiração automática (lifecycle) dos vídeos com o valor configurado.</summary>
    Task SyncVideoLifecycleRuleAsync(int retentionDays, CancellationToken cancellationToken = default);
}
