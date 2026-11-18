namespace FlyGates.Infraestructure.Configuration;

/// <summary>Configuração de acesso ao bucket S3 usado para vídeos/imagens do CageOuts.</summary>
public class AwsS3Options
{
    public const string SectionName = "AwsS3";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string VideoPrefix { get; set; } = "videos/";
    public string ProductImageReferencePrefix { get; set; } = "product-image-references/";
    public string ProductImageReferenceApiKey { get; set; } = string.Empty;
    public int PresignedUrlExpirationMinutes { get; set; } = 15;
}
