namespace FlyGates.Infraestructure.Configuration;

/// <summary>Fonte única de verdade para os dias de retenção de mídia; exposta via /api/MediaSettings.</summary>
public class MediaRetentionOptions
{
    public const string SectionName = "MediaRetention";

    public int VideoRetentionDays { get; set; } = 7;
}
