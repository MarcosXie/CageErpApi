using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlyGates.Application.Entities.Halloween;
using FlyGates.Application.Services.Halloween;
using FlyGates.Controllers;
using FlyGates.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlyGates.Application.Tests;

public sealed class HalloweenHttpTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "halloween-tests-" + Guid.NewGuid().ToString("N"));
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private const string Route = "/api/halloween/sessions";

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(HalloweenController).Assembly.FullName,
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(HalloweenController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IHalloweenSessionService>(provider => new HalloweenSessionService(
            _directory, provider.GetRequiredService<ILogger<HalloweenSessionService>>()));
        _app = builder.Build();
        _app.UseMiddleware<ExceptionMiddleware>();
        _app.UseSwagger();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
    }

    [Fact]
    public async Task SharedRoomPersistsConcurrentAndRetriedScoresAcrossServiceRestart()
    {
        using var created = await _client.PostAsync(Route, null);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var room = (await created.Content.ReadFromJsonAsync<HalloweenSessionResponse>())!;
        Assert.Equal($"{Route}/{room.Id}", created.Headers.Location!.AbsolutePath);
        Assert.Equal(7, room.Scores.Count);
        Assert.All(room.Scores.Values, scores => Assert.Equal(
            new[] { "Andre", "Duda", "Leh", "Maicon" }, scores.Keys));

        var operation = Change("Andre", "missoes", 1);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            using var response = await _client.PostAsJsonAsync($"{Route}/{room.Id}/scores", operation);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }));
        await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            using var response = await _client.PostAsJsonAsync(
                $"{Route}/{room.Id}/scores", Change("Duda", "timer", 1));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }));
        using var decrement = await _client.PostAsJsonAsync(
            $"{Route}/{room.Id}/scores", Change("Leh", "mimica", -1));
        decrement.EnsureSuccessStatusCode();

        using var restarted = new HalloweenSessionService(_directory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<HalloweenSessionService>.Instance);
        var restored = await restarted.GetAsync(room.Id, default);
        Assert.Equal(1, restored.Scores["missoes"]["Andre"]);
        Assert.Equal(12, restored.Scores["timer"]["Duda"]);
        Assert.Equal(-1, restored.Scores["mimica"]["Leh"]);
        Assert.Equal(14, restored.Revision);
        Assert.Contains(operation.OperationId, restored.AppliedOperations);

        using var openApi = await _client.GetAsync("/swagger/v1/swagger.json");
        openApi.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty(Route).TryGetProperty("post", out _));
        Assert.True(paths.GetProperty(Route + "/{id}").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty(Route + "/{id}/scores").TryGetProperty("post", out _));
    }

    [Fact]
    public async Task InvalidBatchDoesNotPartiallyUpdateAndRoomsRemainIsolated()
    {
        using var created = await _client.PostAsync(Route, null);
        var room = (await created.Content.ReadFromJsonAsync<HalloweenSessionResponse>())!;
        var invalid = new UpdateHalloweenScoresRequest
        {
            OperationId = Guid.NewGuid(),
            Changes = [
                new() { Player = "Andre", Game = "missoes", Delta = 2 },
                new() { Player = "Unknown", Game = "missoes", Delta = 2 },
            ],
        };
        using var bad = await _client.PostAsJsonAsync($"{Route}/{room.Id}/scores", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var unchanged = (await _client.GetFromJsonAsync<HalloweenSessionResponse>($"{Route}/{room.Id}"))!;
        Assert.Equal(0, unchanged.Revision);
        Assert.Equal(0, unchanged.Scores["missoes"]["Andre"]);
        using var missing = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var zero = await _client.PostAsJsonAsync($"{Route}/{room.Id}/scores", Change("Andre", "timer", 0));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

        using var another = await _client.PostAsync(Route, null);
        var otherRoom = (await another.Content.ReadFromJsonAsync<HalloweenSessionResponse>())!;
        using var change = await _client.PostAsJsonAsync(
            $"{Route}/{otherRoom.Id}/scores", Change("Andre", "missoes", 4));
        change.EnsureSuccessStatusCode();
        var isolated = (await _client.GetFromJsonAsync<HalloweenSessionResponse>($"{Route}/{room.Id}"))!;
        Assert.Equal(0, isolated.Scores["missoes"]["Andre"]);
    }

    [Fact]
    public async Task CorruptStorageIsReportedInsteadOfReturningAnEmptyScoreboard()
    {
        using var created = await _client.PostAsync(Route, null);
        var room = (await created.Content.ReadFromJsonAsync<HalloweenSessionResponse>())!;
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{room.Id:N}.json"), "{broken");
        using var response = await _client.GetAsync($"{Route}/{room.Id}");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static UpdateHalloweenScoresRequest Change(string player, string game, int delta) => new()
    {
        OperationId = Guid.NewGuid(),
        Changes = [new() { Player = player, Game = game, Delta = delta }],
    };

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
