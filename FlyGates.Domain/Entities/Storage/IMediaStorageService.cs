namespace FlyGates.Application.Entities.Storage;

/// <summary>Abstração sobre o armazenamento de mídia (S3) usada pela camada de aplicação.</summary>
public interface IMediaStorageService
{
    /// <summary>Gera uma URL temporária de leitura para a chave informada; retorna null se a chave for vazia.</summary>
    string? GeneratePresignedUrl(string? objectKey);

    /// <summary>Gera uma URL temporária de download que força o navegador a salvar com o nome de arquivo informado.</summary>
    string? GeneratePresignedDownloadUrl(string objectKey, string fileName, TimeSpan expiration);

    /// <summary>Lê o objeto como texto UTF-8; retorna null se a chave não existir no bucket.</summary>
    Task<string?> ReadObjectTextAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Remove o objeto do bucket; não falha se a chave for vazia ou o objeto não existir.</summary>
    Task DeleteObjectAsync(string? objectKey, CancellationToken cancellationToken = default);

    /// <summary>Envia o conteúdo para o bucket sob a chave informada e retorna a própria chave.</summary>
    Task<string> UploadObjectAsync(Stream content, string objectKey, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Sincroniza a regra de expiração automática (lifecycle) dos vídeos com o valor configurado.</summary>
    Task SyncVideoLifecycleRuleAsync(int retentionDays, CancellationToken cancellationToken = default);
}
