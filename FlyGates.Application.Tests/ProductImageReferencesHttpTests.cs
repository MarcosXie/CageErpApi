using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Services.ProductImageReferences;
using FlyGates.Controllers.CageOuts;
using FlyGates.Filters;
using FlyGates.Infraestructure.Configuration;
using FlyGates.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlyGates.Application.Tests;

public sealed class ProductImageReferencesHttpTests
{
    private const string BaseRoute = "/api/cageouts/product-image-references";
    private const string ImageRoute = BaseRoute + "/7891234567890/example.foreground.png";
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9d8AAAAASUVORK5CYII=");

    [Fact]
    public async Task SharedImageContractWorksOverHttp()
    {
        var key = Guid.NewGuid().ToString("N");
        await using var app = CreateHost(key);
        await app.StartAsync();
        using var client = CreateClient(app, key);

        using var empty = await client.GetAsync(BaseRoute);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("[]", await empty.Content.ReadAsStringAsync());

        using var created = await PutImageAsync(client);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(ImageRoute, created.Headers.Location?.AbsolutePath);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal("7891234567890", createdJson.RootElement.GetProperty("sku").GetString());
        Assert.Equal(Png.Length, createdJson.RootElement.GetProperty("length").GetInt64());

        using var repeated = await PutImageAsync(client);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        using var differentContent = new ByteArrayContent(Png.Concat(new byte[] { 1 }).ToArray());
        differentContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var conflict = await client.PutAsync(ImageRoute, differentContent);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var snapshot = await client.GetAsync(BaseRoute);
        using var snapshotJson = JsonDocument.Parse(await snapshot.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, snapshotJson.RootElement.ValueKind);
        var item = Assert.Single(snapshotJson.RootElement.EnumerateArray());
        Assert.Equal(
            new[] { "sku", "fileName", "deleted", "length", "lastModifiedUtc" },
            item.EnumerateObject().Select(property => property.Name));
        Assert.False(item.GetProperty("deleted").GetBoolean());

        using var download = await client.GetAsync(ImageRoute);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await download.Content.ReadAsByteArrayAsync());
        using var deleted = await client.DeleteAsync(ImageRoute);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var gone = await client.GetAsync(ImageRoute);
        Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        using var resurrection = await PutImageAsync(client);
        Assert.Equal(HttpStatusCode.Gone, resurrection.StatusCode);
        using var deletedSnapshot = await client.GetAsync(BaseRoute);
        using var deletedJson = JsonDocument.Parse(await deletedSnapshot.Content.ReadAsStringAsync());
        Assert.True(Assert.Single(deletedJson.RootElement.EnumerateArray()).GetProperty("deleted").GetBoolean());
    }

    [Theory]
    [InlineData("GET", false, false, 401)]
    [InlineData("GET", true, false, 401)]
    [InlineData("PUT", false, false, 401)]
    [InlineData("DELETE", true, false, 401)]
    [InlineData("LIST", false, false, 401)]
    [InlineData("LIST", true, true, 503)]
    [InlineData("GET", true, true, 503)]
    [InlineData("PUT", true, true, 503)]
    [InlineData("DELETE", false, true, 503)]
    public async Task AuthorizationFailsClosed(
        string method, bool sendWrongKey, bool unconfigured, int expectedStatus)
    {
        await using var app = CreateHost(unconfigured ? string.Empty : Guid.NewGuid().ToString("N"));
        await app.StartAsync();
        using var client = CreateClient(app, sendWrongKey ? "invalid-test-value" : null);
        using var request = new HttpRequestMessage(
            new HttpMethod(method == "LIST" ? "GET" : method),
            method == "LIST" ? BaseRoute : ImageRoute);
        if (method == "PUT")
        {
            request.Content = ImageContent();
        }
        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
    }

    private static WebApplication CreateHost(string key)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(ProductImageReferencesController).Assembly.FullName,
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(ProductImageReferencesController).Assembly);
        builder.Services.Configure<AwsS3Options>(options => options.ProductImageReferenceApiKey = key);
        builder.Services.AddSingleton<IProductImageReferenceStorage,
            ProductImageReferenceServiceTests.FakeProductImageReferenceStorage>();
        builder.Services.AddScoped<IProductImageReferenceService, ProductImageReferenceService>();
        var app = builder.Build();
        app.UseMiddleware<ExceptionMiddleware>();
        app.MapControllers();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app, string? key)
    {
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        if (key is not null)
        {
            client.DefaultRequestHeaders.Add(ProductImageReferenceAuthorizationFilter.HeaderName, key);
        }
        return client;
    }

    private static async Task<HttpResponseMessage> PutImageAsync(HttpClient client)
    {
        using var content = ImageContent();
        return await client.PutAsync(ImageRoute, content);
    }

    private static ByteArrayContent ImageContent()
    {
        var content = new ByteArrayContent(Png);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return content;
    }
}
