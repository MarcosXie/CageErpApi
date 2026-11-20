using System.Text.Json;
using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Exceptions;
using FlyGates.Application.Services.Halloween;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlyGates.Application.Tests;

public sealed class HalloweenGameTests : IDisposable
{
    private static readonly string[] Players = ["Andre", "Duda", "Leh", "Maicon"];
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "halloween-game-tests-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new();
    private readonly HalloweenSessionService _service;

    public HalloweenGameTests() => _service = new(_directory, NullLogger<HalloweenSessionService>.Instance, _clock);

    [Fact]
    public async Task TimerSharesOneStartHidesAttemptsAndAwardsEveryRoundExactlyOnce()
    {
        var room = await _service.CreateAsync(default);
        for (var index = 0; index < 5; index++)
        {
            var round = room.Timer!.Rounds[index];
            Assert.InRange(round.TargetSeconds, 3, 30);
            if (index > 0) Assert.True(round.TargetSeconds > room.Timer.Rounds[index - 1].TargetSeconds);
            await Reject(room.Id, Action("timer-start", round.Id), 409);
            foreach (var player in Players)
                room = await Run(room.Id, Action("timer-ready", round.Id, player));
            var started = _clock.GetUtcNow().AddSeconds(5);
            room = await Run(room.Id, Action("timer-start", round.Id));
            Assert.Equal(started, room.Timer!.Rounds[index].StartsAt);
            await Reject(room.Id, Action("timer-stop", round.Id, "Andre") with { ElapsedMs = 0 }, 409);
            _clock.Advance(TimeSpan.FromSeconds(6 + round.TargetSeconds));
            var offsets = new[] { 0, 100, -100, 1000 };
            for (var playerIndex = 0; playerIndex < Players.Length; playerIndex++)
            {
                var command = Action("timer-stop", round.Id, Players[playerIndex]) with
                {
                    ElapsedMs = round.TargetSeconds * 1000 + offsets[playerIndex],
                };
                room = await Run(room.Id, command);
                var replay = await Run(room.Id, command);
                Assert.Equal(room.Revision, replay.Revision);
                var result = room.Timer!.Rounds[index];
                Assert.Equal(playerIndex + 1, result.StoppedPlayers.Length);
                if (playerIndex < 3) Assert.Empty(result.ElapsedMilliseconds);
                else Assert.Equal(new[] { 4, 3, 3, 1 }, Players.Select(player => result.Points[player]));
            }
            Assert.True(room.Timer!.Rounds[index].Awarded);
            Assert.Equal(4 * (index + 1), room.Scores["timer"]["Andre"]);
            await Reject(room.Id, Action("timer-stop", round.Id, "Andre") with { ElapsedMs = 1000 }, 409);
            if (index < 4)
            {
                room = await Run(room.Id, Action("timer-next", round.Id));
                Assert.Equal(index + 1, room.Timer!.RoundIndex);
                await Reject(room.Id, Action("timer-next", round.Id), 409);
            }
        }
        room = await Run(room.Id, Action("game-status") with { Game = "timer", Completed = true });
        Assert.Contains("timer", room.CompletedGames);
        Assert.Equal(20, room.Scores["timer"]["Andre"]);
        await Assert.ThrowsAsync<BaseException>(() => _service.UpdateScoresAsync(room.Id, Score("Andre", "timer", 1), default));
    }

    [Fact]
    public async Task TimerRequiresFreshReadinessAndCanCloseMissingPlayersAfterNinetySeconds()
    {
        var room = await _service.CreateAsync(default);
        var round = room.Timer!.Rounds[0];
        foreach (var player in Players) await Run(room.Id, Action("timer-ready", round.Id, player));
        _clock.Advance(TimeSpan.FromSeconds(61));
        await Reject(room.Id, Action("timer-start", round.Id), 409);
        foreach (var player in Players) await Run(room.Id, Action("timer-ready", round.Id, player));
        room = await Run(room.Id, Action("timer-start", round.Id));
        using var restarted = new HalloweenSessionService(_directory, NullLogger<HalloweenSessionService>.Instance, _clock);
        var restored = await restarted.GetAsync(room.Id, default);
        Assert.Equal(room.Timer!.Rounds[0].StartsAt, restored.Timer!.Rounds[0].StartsAt);
        await Reject(room.Id, Action("timer-close", round.Id), 409);
        _clock.Advance(TimeSpan.FromSeconds(6));
        await Run(room.Id, Action("timer-stop", round.Id, "Andre") with { ElapsedMs = 1000 });
        _clock.Advance(TimeSpan.FromSeconds(89));
        room = await Run(room.Id, Action("timer-close", round.Id));
        Assert.Equal(4, room.Scores["timer"]["Andre"]);
        Assert.Equal(0, room.Scores["timer"]["Duda"]);
        Assert.Null(room.Timer!.Rounds[0].ElapsedMilliseconds["Duda"]);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public async Task DrinkKeepsBallotsSecretAndScoresGuessesAndBluff(int correctVotes, int holderPoints)
    {
        var room = await _service.CreateAsync(default);
        room = await Run(room.Id, Action("drink-start"));
        var roundId = room.DrinkRounds[0].Id;
        await Reject(room.Id, Action("drink-reveal", roundId, "Andre"), 409);
        for (var index = 0; index < Players.Length; index++)
        {
            var player = Players[index];
            var target = index > 0 && index <= correctVotes ? "Andre"
                : Players.First(other => other != player && other != "Andre");
            room = await Run(room.Id, Action("drink-vote", roundId, player) with { Target = target });
            Assert.Empty(room.DrinkRounds[0].Votes);
            Assert.Equal(index + 1, room.DrinkRounds[0].VotedPlayers.Length);
        }
        await Reject(room.Id, Action("drink-vote", roundId, "Duda") with { Target = "Andre" }, 409);
        var reveal = Action("drink-reveal", roundId, "Andre");
        room = await Run(room.Id, reveal);
        var replay = await Run(room.Id, reveal);
        Assert.Equal(room.Revision, replay.Revision);
        Assert.Equal(holderPoints, room.Scores["drink-russo"]["Andre"]);
        Assert.Equal(correctVotes * 2, Players.Skip(1).Sum(player => room.Scores["drink-russo"][player]));
        Assert.Equal(4, room.DrinkRounds[0].Votes.Count);
    }

    [Fact]
    public async Task TeamsUpdateAtomicallyAndLegacyRoomsAreUpgradedWithoutLosingScores()
    {
        var room = await _service.CreateAsync(default);
        var id = Guid.NewGuid();
        room.Scores["mimica"]["Andre"] = 2;
        room.Scores["mimica"]["Maicon"] = 1;
        room.Scores["mimica"]["Leh"] = 3;
        var legacy = new { id, revision = 7, scores = room.Scores, appliedOperations = Array.Empty<Guid>() };
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{id:N}.json"), JsonSerializer.Serialize(legacy));
        var upgraded = await _service.GetAsync(id, default);
        Assert.Equal(2, upgraded.RulesVersion);
        Assert.Equal(3, upgraded.Scores["mimica"]["Andre"]);
        Assert.Equal(3, upgraded.Scores["mimica"]["Maicon"]);
        Assert.Equal(upgraded.Timer!.Id, (await _service.GetAsync(id, default)).Timer!.Id);
        var changed = await _service.UpdateScoresAsync(id, Score("Andre", "mimica", 1), default);
        Assert.Equal(4, changed.Scores["mimica"]["Andre"]);
        Assert.Equal(4, changed.Scores["mimica"]["Maicon"]);
        Assert.Equal(3, changed.Scores["mimica"]["Duda"]);
        var invalidBatch = new UpdateHalloweenScoresRequest
        {
            OperationId = Guid.NewGuid(),
            Changes = [new() { Player = "Andre", Game = "mimica", Delta = 1 },
                new() { Player = "Leh", Game = "sobrevivencia", Delta = -1 }],
        };
        await Assert.ThrowsAsync<BaseException>(() => _service.UpdateScoresAsync(id, invalidBatch, default));
        Assert.Equal(4, (await _service.GetAsync(id, default)).Scores["mimica"]["Andre"]);
    }

    [Fact]
    public async Task InstagramResultsAndTenMinuteSurvivalCanBeFinalizedAndReopened()
    {
        var room = await _service.CreateAsync(default);
        await Reject(room.Id, Action("instagram") with { Url = "https://example.com/post" }, 400);
        room = await Run(room.Id, Action("instagram") with { Url = "https://www.instagram.com/p/example/" });
        room = await Run(room.Id, Action("fantasy-votes", player: "Leh") with { Votes = 123 });
        Assert.Equal(123, room.Scores["fantasia"]["Leh"]);
        await Run(room.Id, Action("game-status") with { Game = "fantasia", Completed = true });
        await Reject(room.Id, Action("fantasy-votes", player: "Leh") with { Votes = 124 }, 409);
        await Run(room.Id, Action("game-status") with { Game = "fantasia", Completed = false });
        room = await Run(room.Id, Action("fantasy-votes", player: "Leh") with { Votes = 124 });
        Assert.Equal(124, room.Scores["fantasia"]["Leh"]);
        await Reject(room.Id, Action("game-status") with { Game = "sobrevivencia", Completed = true }, 409);
        foreach (var player in Players)
        {
            await Run(room.Id, Action("survival-start", player: player));
            _clock.Advance(TimeSpan.FromMinutes(9));
            await Reject(room.Id, Action("survival-finish", player: player), 409);
            _clock.Advance(TimeSpan.FromMinutes(1));
            room = await Run(room.Id, Action("survival-finish", player: player));
            Assert.Contains(player, room.Survival.CompletedPlayers);
        }
        room = await Run(room.Id, Action("game-status") with { Game = "sobrevivencia", Completed = true });
        Assert.Contains("sobrevivencia", room.CompletedGames);
    }

    private Task<HalloweenSessionResponse> Run(Guid id, HalloweenActionRequest command) => _service.ExecuteAsync(id, command, default);
    private async Task Reject(Guid id, HalloweenActionRequest command, int status)
    {
        var error = await Assert.ThrowsAsync<BaseException>(() => Run(id, command));
        Assert.Equal(status, error.StatusCode);
    }
    private static HalloweenActionRequest Action(string action, Guid? roundId = null, string? player = null) =>
        new() { OperationId = Guid.NewGuid(), Action = action, RoundId = roundId, Player = player };
    private static UpdateHalloweenScoresRequest Score(string player, string game, int delta) => new()
    {
        OperationId = Guid.NewGuid(), Changes = [new() { Player = player, Game = game, Delta = delta }],
    };
    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 20, 22, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan amount) => _now += amount;
    }
}
