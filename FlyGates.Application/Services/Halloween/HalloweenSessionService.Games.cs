using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.Halloween;

public sealed partial class HalloweenSessionService
{
    public Task<HalloweenSessionResponse> ExecuteAsync(
        Guid id, HalloweenActionRequest request, CancellationToken cancellationToken) =>
        WithStorageAsync(async () =>
        {
            if (request.OperationId == Guid.Empty)
                throw new BaseException("Informe uma operacao valida.", 400);
            var session = await ReadAsync(id, cancellationToken);
            if (session.AppliedOperations.Contains(request.OperationId))
                return PublicSnapshot(session);

            var now = _clock.GetUtcNow();
            switch (request.Action)
            {
                case "game-status":
                    if (request.Game is null || !Games.Contains(request.Game) || request.Completed is null)
                        throw new BaseException("Informe o jogo e se ele esta concluido.", 400);
                    if (request.Completed.Value)
                    {
                        if (request.Game == "timer" && !session.Timer!.Rounds.All(round => round.Awarded)
                            || request.Game == "sobrevivencia" && session.Survival.CompletedPlayers.Count != 4
                            || request.Game == "drink-russo" && (session.DrinkRounds.Count == 0
                                || session.DrinkRounds.Any(round => round.Holder is null))
                            || request.Game == "fantasia" && session.InstagramPostUrl is null)
                            throw new BaseException("Conclua as etapas deste jogo antes de fechar o ranking.", 409);
                        session.CompletedGames.Add(request.Game);
                    }
                    else session.CompletedGames.Remove(request.Game);
                    break;
                case "instagram":
                    EnsureOpen(session, "fantasia");
                    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
                        || url.Scheme != "https" || url.Host is not ("instagram.com" or "www.instagram.com")
                        || !string.IsNullOrEmpty(url.UserInfo) || !url.IsDefaultPort)
                        throw new BaseException("Use um link HTTPS do post em instagram.com.", 400);
                    session = session with { InstagramPostUrl = url.AbsoluteUri };
                    break;
                case "fantasy-votes":
                    EnsureOpen(session, "fantasia");
                    RequirePlayer(request.Player);
                    if (request.Votes is null or < 0 or > 1_000_000)
                        throw new BaseException("Informe a quantidade de votos, de 0 a 1.000.000.", 400);
                    session.Scores["fantasia"][request.Player!] = request.Votes.Value;
                    break;
                case "timer-ready":
                case "timer-start":
                case "timer-stop":
                case "timer-next":
                case "timer-close":
                    EnsureOpen(session, "timer");
                    session = session with { Timer = ExecuteTimer(session, request, now) };
                    break;
                case "drink-start":
                    EnsureOpen(session, "drink-russo");
                    if (session.DrinkRounds.LastOrDefault() is { Holder: null })
                        throw new BaseException("Revele o resultado da rodada anterior.", 409);
                    session.DrinkRounds.Add(new HalloweenDrinkRound(Guid.NewGuid()));
                    break;
                case "drink-vote":
                case "drink-reveal":
                    EnsureOpen(session, "drink-russo");
                    ExecuteDrink(session, request);
                    break;
                case "survival-start":
                    EnsureOpen(session, "sobrevivencia");
                    RequirePlayer(request.Player);
                    if (session.Survival.Player is not null
                        || session.Survival.CompletedPlayers.Contains(request.Player!))
                        throw new BaseException("Aguarde a tentativa atual ou escolha quem ainda nao jogou.", 409);
                    session = session with { Survival = session.Survival with { Player = request.Player, StartsAt = now } };
                    break;
                case "survival-finish":
                case "survival-cancel":
                    EnsureOpen(session, "sobrevivencia");
                    if (session.Survival.Player is null || request.Player != session.Survival.Player)
                        throw new BaseException("Esta tentativa nao esta mais ativa.", 409);
                    if (request.Action == "survival-finish")
                    {
                        if (now < session.Survival.StartsAt!.Value.AddMinutes(10))
                            throw new BaseException("A tentativa tem duracao de 10 minutos.", 409);
                        session.Survival.CompletedPlayers.Add(session.Survival.Player);
                    }
                    session = session with { Survival = session.Survival with { Player = null, StartsAt = null } };
                    break;
                default:
                    throw new BaseException("Acao de jogo desconhecida.", 400);
            }
            session.AppliedOperations.Add(request.OperationId);
            session = session with { Revision = session.Revision + 1 };
            await SaveAsync(session, cancellationToken);
            return PublicSnapshot(session);
        }, cancellationToken);

    private static HalloweenTimerMatch NewTimerMatch() => new(Guid.NewGuid(), 0,
        new[] { (3, 6), (7, 11), (12, 17), (18, 24), (25, 30) }
            .Select(range => new HalloweenTimerRound(Guid.NewGuid(), Random.Shared.Next(range.Item1, range.Item2 + 1)))
            .ToList());

    private static HalloweenTimerMatch ExecuteTimer(
        HalloweenSessionResponse session, HalloweenActionRequest request, DateTimeOffset now)
    {
        var match = session.Timer!;
        var round = match.Rounds[match.RoundIndex];
        if (request.RoundId != round.Id)
            throw new BaseException("A rodada mudou. Atualize a tela antes de continuar.", 409);
        switch (request.Action)
        {
            case "timer-ready":
                RequirePlayer(request.Player);
                if (round.StartsAt is not null)
                    throw new BaseException("A largada ja foi marcada.", 409);
                round.ReadyPlayers[request.Player!] = now;
                break;
            case "timer-start":
                if (round.StartsAt is not null || !Players.All(player =>
                    round.ReadyPlayers.TryGetValue(player, out var ready) && now - ready < TimeSpan.FromSeconds(60)))
                    throw new BaseException("Os quatro jogadores precisam confirmar que estao prontos (vale por 60s).", 409);
                match.Rounds[match.RoundIndex] = round with { StartsAt = now.AddSeconds(5) };
                break;
            case "timer-stop":
                RequirePlayer(request.Player);
                if (round.StartsAt is null || now < round.StartsAt || round.Awarded
                    || round.ElapsedMilliseconds.ContainsKey(request.Player!))
                    throw new BaseException("Nao e possivel parar este cronometro agora.", 409);
                if (request.ElapsedMs is null or < 0 or > 90000
                    || request.ElapsedMs > (now - round.StartsAt.Value).TotalMilliseconds + 1000)
                    throw new BaseException("Tempo invalido. O limite de uma tentativa e 90 segundos.", 400);
                round.ElapsedMilliseconds[request.Player!] = request.ElapsedMs.Value;
                AwardTimerRound(session, match, round);
                break;
            case "timer-close":
                if (round.StartsAt is null || now < round.StartsAt.Value.AddSeconds(90) || round.Awarded)
                    throw new BaseException("Aguarde 90s apos a largada para encerrar jogadores ausentes.", 409);
                foreach (var player in Players) round.ElapsedMilliseconds.TryAdd(player, null);
                AwardTimerRound(session, match, round);
                break;
            case "timer-next":
                if (!round.Awarded || match.RoundIndex >= 4)
                    throw new BaseException("A rodada ainda nao terminou ou a partida ja acabou.", 409);
                return match with { RoundIndex = match.RoundIndex + 1 };
        }
        return match;
    }

    private static void AwardTimerRound(
        HalloweenSessionResponse session, HalloweenTimerMatch match, HalloweenTimerRound round)
    {
        if (round.ElapsedMilliseconds.Count != 4 || round.Awarded) return;
        var errors = round.ElapsedMilliseconds.Values.Where(value => value.HasValue)
            .Select(value => Math.Abs(value!.Value - round.TargetSeconds * 1000)).ToArray();
        foreach (var player in Players)
        {
            var elapsed = round.ElapsedMilliseconds[player];
            var points = elapsed.HasValue
                ? 4 - errors.Count(error => error < Math.Abs(elapsed.Value - round.TargetSeconds * 1000)) : 0;
            round.Points[player] = points;
            AddScore(session, "timer", player, points);
        }
        match.Rounds[match.RoundIndex] = round with { Awarded = true };
    }

    private static void ExecuteDrink(HalloweenSessionResponse session, HalloweenActionRequest request)
    {
        var round = session.DrinkRounds.LastOrDefault();
        if (round is null || round.Id != request.RoundId || round.Holder is not null)
            throw new BaseException("Esta rodada de Drink Russo nao esta aberta.", 409);
        RequirePlayer(request.Player);
        if (request.Action == "drink-vote")
        {
            RequirePlayer(request.Target);
            if (request.Player == request.Target || round.Votes.ContainsKey(request.Player!))
                throw new BaseException("Vote uma vez em outra pessoa.", 409);
            round.Votes[request.Player!] = request.Target!;
            return;
        }
        if (round.Votes.Count != 4)
            throw new BaseException("Aguarde os quatro votos antes de revelar quem recebeu alcool.", 409);
        var holder = request.Player!;
        var correct = Players.Count(player => player != holder && round.Votes[player] == holder);
        foreach (var player in Players)
        {
            var points = player == holder ? 3 - correct + (correct == 0 ? 2 : 0)
                : round.Votes[player] == holder ? 2 : 0;
            round.Points[player] = points;
            AddScore(session, "drink-russo", player, points);
        }
        session.DrinkRounds[^1] = round with { Holder = holder };
    }

    private static string Partner(string player) => player switch
    {
        "Andre" => "Maicon", "Maicon" => "Andre", "Leh" => "Duda", "Duda" => "Leh",
        _ => throw new BaseException("Jogador invalido.", 400),
    };

    private static void RequirePlayer(string? player)
    {
        if (player is null || !Players.Contains(player))
            throw new BaseException("Jogador invalido.", 400);
    }

    private static void EnsureOpen(HalloweenSessionResponse session, string game)
    {
        if (session.CompletedGames.Contains(game))
            throw new BaseException("Reabra o jogo antes de alterar resultados.", 409);
    }

    private static void AddScore(HalloweenSessionResponse session, string game, string player, int delta)
    {
        var score = (long)session.Scores[game][player] + delta;
        if (score is < -1_000_000 or > 1_000_000 || game is "sobrevivencia" or "fantasia" && score < 0)
            throw new BaseException("Quantidade fora dos limites do placar.", 409);
        session.Scores[game][player] = (int)score;
    }

    private static HalloweenSessionResponse PublicSnapshot(HalloweenSessionResponse session) => session with
    {
        Timer = session.Timer is null ? null : session.Timer with
        {
            Rounds = session.Timer.Rounds.Select(round => round with
            {
                StoppedPlayers = round.ElapsedMilliseconds.Keys.ToArray(),
                ElapsedMilliseconds = round.Awarded ? round.ElapsedMilliseconds : [],
            }).ToList(),
        },
        DrinkRounds = session.DrinkRounds.Select(round => round with
        {
            VotedPlayers = round.Votes.Keys.ToArray(),
            Votes = round.Holder is null ? [] : round.Votes,
        }).ToList(),
    };
}
