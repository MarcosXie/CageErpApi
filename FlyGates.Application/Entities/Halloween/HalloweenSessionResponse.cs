using System.ComponentModel.DataAnnotations;

namespace FlyGates.Application.Entities.Halloween;

/// <summary>A private-link Halloween room and its per-game player scores.</summary>
public sealed record HalloweenSessionResponse(
    Guid Id,
    long Revision,
    Dictionary<string, Dictionary<string, int>> Scores,
    HashSet<Guid> AppliedOperations);

/// <summary>An idempotent, atomic set of score adjustments.</summary>
public sealed record UpdateHalloweenScoresRequest
{
    public required Guid OperationId { get; init; }

    [Required, MinLength(1), MaxLength(4)]
    public required HalloweenScoreChange[] Changes { get; init; }
}

/// <summary>Adjusts a player's score in one Halloween game.</summary>
public sealed record HalloweenScoreChange
{
    [Required]
    public required string Player { get; init; }

    [Required]
    public required string Game { get; init; }

    [Range(-100, 100)]
    public required int Delta { get; init; }
}
