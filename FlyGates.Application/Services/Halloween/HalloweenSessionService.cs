using System.Text.Json;
using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlyGates.Application.Services.Halloween;

public sealed partial class HalloweenSessionService(
    string directory, ILogger<HalloweenSessionService> logger, TimeProvider? clock = null)
    : IHalloweenSessionService, IDisposable
{
    private static readonly string[] Players = ["Andre", "Duda", "Leh", "Maicon"];
    private static readonly string[] Games =
        ["missoes", "mimica", "drink-russo", "quem-sou-eu", "timer", "sobrevivencia", "fantasia"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public Task<HalloweenSessionResponse> CreateAsync(CancellationToken cancellationToken) =>
        WithStorageAsync(async () =>
        {
            var session = new HalloweenSessionResponse(Guid.NewGuid(), 0,
                Games.ToDictionary(game => game, _ => Players.ToDictionary(player => player, _ => 0)),
                []) { RulesVersion = 2, Timer = NewTimerMatch() };
            await SaveAsync(session, cancellationToken);
            return session;
        }, cancellationToken);

    public Task<HalloweenSessionResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        WithStorageAsync(async () => PublicSnapshot(await ReadAsync(id, cancellationToken)), cancellationToken);

    public Task<HalloweenSessionResponse> UpdateScoresAsync(
        Guid id, UpdateHalloweenScoresRequest request, CancellationToken cancellationToken) =>
        WithStorageAsync(async () =>
        {
            if (request.OperationId == Guid.Empty || request.Changes is not { Length: >= 1 and <= 4 }
                || request.Changes.Any(change => change is null
                    || !Players.Contains(change.Player) || !Games.Contains(change.Game)
                    || change.Delta is < -100 or > 100 or 0))
            {
                throw new BaseException("Informe uma operacao, jogadores, jogos e pontos validos.", 400);
            }

            var session = await ReadAsync(id, cancellationToken);
            if (session.AppliedOperations.Contains(request.OperationId))
            {
                return PublicSnapshot(session);
            }

            foreach (var change in request.Changes)
            {
                EnsureOpen(session, change.Game);
                AddScore(session, change.Game, change.Player, change.Delta);
                if (change.Game == "mimica")
                {
                    AddScore(session, change.Game, Partner(change.Player), change.Delta);
                }
            }
            session.AppliedOperations.Add(request.OperationId);
            session = session with { Revision = session.Revision + 1 };
            await SaveAsync(session, cancellationToken);
            return PublicSnapshot(session);
        }, cancellationToken);

    private async Task<HalloweenSessionResponse> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = FilePath(id);
        if (!File.Exists(path))
        {
            throw new BaseException("Sala nao encontrada. Confira o link compartilhado.", 404);
        }

        HalloweenSessionResponse? session;
        await using (var stream = File.OpenRead(path))
        {
            session = await JsonSerializer.DeserializeAsync<HalloweenSessionResponse>(
                stream, JsonOptions, cancellationToken);
        }
        if (session is null || session.Id != id || session.Scores is null || session.AppliedOperations is null
            || Games.Any(game => !session.Scores.TryGetValue(game, out var scores)
                || scores is null || Players.Any(player => !scores.ContainsKey(player))))
        {
            throw new JsonException("Invalid Halloween room file.");
        }
        var upgrade = session.RulesVersion < 2 || session.Timer is null;
        if (session.RulesVersion < 2)
        {
            foreach (var (first, second) in new[] { ("Andre", "Maicon"), ("Leh", "Duda") })
            {
                var teamScore = session.Scores["mimica"][first] + session.Scores["mimica"][second];
                session.Scores["mimica"][first] = teamScore;
                session.Scores["mimica"][second] = teamScore;
            }
            session = session with { RulesVersion = 2 };
        }
        session = session with { Timer = session.Timer ?? NewTimerMatch() };
        if (session.Timer.Rounds is not { Count: 5 } || session.Timer.RoundIndex is < 0 or > 4
            || session.Timer.Rounds.Any(round => round.ReadyPlayers is null || round.ElapsedMilliseconds is null
                || round.Points is null || round.TargetSeconds is < 3 or > 30)
            || session.CompletedGames is null || session.DrinkRounds is null || session.Survival is null)
            throw new JsonException("Invalid Halloween game state.");
        if (upgrade) await SaveAsync(session, cancellationToken);
        return session;
    }

    private async Task SaveAsync(HalloweenSessionResponse session, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        var target = FilePath(session.Id);
        var temporary = target + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, session, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private string FilePath(Guid id) => Path.Combine(_directory, $"{id:N}.json");

    private async Task<T> WithStorageAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogError(exception, "Halloween room storage failed.");
            throw new BaseException("Nao foi possivel acessar o placar. Tente novamente; nao reinicie a sala.", 503);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
