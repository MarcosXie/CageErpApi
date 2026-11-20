using System.Text.Json;
using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlyGates.Application.Services.Halloween;

public sealed class HalloweenSessionService(string directory, ILogger<HalloweenSessionService> logger)
    : IHalloweenSessionService, IDisposable
{
    private static readonly string[] Players = ["Andre", "Duda", "Leh", "Maicon"];
    private static readonly string[] Games =
        ["missoes", "mimica", "drink-russo", "quem-sou-eu", "timer", "sobrevivencia", "fantasia"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _directory = Path.GetFullPath(directory);

    public Task<HalloweenSessionResponse> CreateAsync(CancellationToken cancellationToken) =>
        WithStorageAsync(async () =>
        {
            var session = new HalloweenSessionResponse(Guid.NewGuid(), 0,
                Games.ToDictionary(game => game, _ => Players.ToDictionary(player => player, _ => 0)),
                []);
            await SaveAsync(session, cancellationToken);
            return session;
        }, cancellationToken);

    public Task<HalloweenSessionResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        WithStorageAsync(() => ReadAsync(id, cancellationToken), cancellationToken);

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
                return session;
            }

            foreach (var change in request.Changes)
            {
                var score = (long)session.Scores[change.Game][change.Player] + change.Delta;
                if (score is < -1_000_000 or > 1_000_000)
                {
                    throw new BaseException("O placar atingiu o limite de pontos.", 409);
                }
                session.Scores[change.Game][change.Player] = (int)score;
            }
            session.AppliedOperations.Add(request.OperationId);
            session = session with { Revision = session.Revision + 1 };
            await SaveAsync(session, cancellationToken);
            return session;
        }, cancellationToken);

    private async Task<HalloweenSessionResponse> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = FilePath(id);
        if (!File.Exists(path))
        {
            throw new BaseException("Sala nao encontrada. Confira o link compartilhado.", 404);
        }

        await using var stream = File.OpenRead(path);
        var session = await JsonSerializer.DeserializeAsync<HalloweenSessionResponse>(
            stream, JsonOptions, cancellationToken);
        if (session is null || session.Id != id || session.Scores is null || session.AppliedOperations is null
            || Games.Any(game => !session.Scores.TryGetValue(game, out var scores)
                || scores is null || Players.Any(player => !scores.ContainsKey(player))))
        {
            throw new JsonException("Invalid Halloween room file.");
        }
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
