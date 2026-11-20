using System.ComponentModel.DataAnnotations;

namespace FlyGates.Application.Entities.Halloween;

/// <summary>A private-link Halloween room and its per-game player scores.</summary>
public sealed record HalloweenSessionResponse(
    Guid Id,
    long Revision,
    Dictionary<string, Dictionary<string, int>> Scores,
    HashSet<Guid> AppliedOperations)
{
    public int RulesVersion { get; init; } = 1;
    public HashSet<string> CompletedGames { get; init; } = [];
    public string? InstagramPostUrl { get; init; }
    public HalloweenTimerMatch? Timer { get; init; }
    public List<HalloweenDrinkRound> DrinkRounds { get; init; } = [];
    public HalloweenSurvivalState Survival { get; init; } = new();
}

/// <summary>Five shared rounds with a common scheduled start on all phones.</summary>
public sealed record HalloweenTimerMatch(Guid Id, int RoundIndex, List<HalloweenTimerRound> Rounds);

/// <summary>Readiness, common start, attempts and points for one timer round.</summary>
public sealed record HalloweenTimerRound(Guid Id, int TargetSeconds)
{
    public Dictionary<string, DateTimeOffset> ReadyPlayers { get; init; } = [];
    public DateTimeOffset? StartsAt { get; init; }
    public Dictionary<string, int?> ElapsedMilliseconds { get; init; } = [];
    public string[] StoppedPlayers { get; init; } = [];
    public Dictionary<string, int> Points { get; init; } = [];
    public bool Awarded { get; init; }
}

/// <summary>A drink round; ballot choices are hidden in responses until revelation.</summary>
public sealed record HalloweenDrinkRound(Guid Id)
{
    public Dictionary<string, string> Votes { get; init; } = [];
    public string[] VotedPlayers { get; init; } = [];
    public string? Holder { get; init; }
    public Dictionary<string, int> Points { get; init; } = [];
}

/// <summary>Shared ten-minute horror-game attempts and completed players.</summary>
public sealed record HalloweenSurvivalState
{
    public string? Player { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public HashSet<string> CompletedPlayers { get; init; } = [];
}

/// <summary>One idempotent game command; fields depend on the action.</summary>
public sealed record HalloweenActionRequest
{
    public required Guid OperationId { get; init; }
    [Required, MaxLength(40)]
    public required string Action { get; init; }
    public string? Game { get; init; }
    public string? Player { get; init; }
    public string? Target { get; init; }
    public Guid? RoundId { get; init; }
    public bool? Completed { get; init; }
    [Range(0, 90000)]
    public int? ElapsedMs { get; init; }
    [MaxLength(2048)]
    public string? Url { get; init; }
    [Range(0, 1_000_000)]
    public int? Votes { get; init; }
}

/// <summary>UTC clock sample used to estimate phone clock offset and network uncertainty.</summary>
public sealed record HalloweenClockResponse(DateTimeOffset ServerTime);

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
