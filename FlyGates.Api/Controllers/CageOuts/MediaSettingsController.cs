using FlyGates.Infraestructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlyGates.Controllers.CageOuts;

/// <summary>Fonte única de verdade para os PDVs consultarem os dias de retenção de mídia configurados na API.</summary>
[ApiController]
[Route("api/[controller]")]
public class MediaSettingsController(IOptionsMonitor<MediaRetentionOptions> retentionOptions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MediaSettingsResponseDto), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new MediaSettingsResponseDto(retentionOptions.CurrentValue.VideoRetentionDays));
    }
}

public sealed record MediaSettingsResponseDto(int VideoRetentionDays);
