using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;
using FlyGates.Application.Services.ProductImageReferences;
using Xunit;

namespace FlyGates.Application.Tests;

public sealed class ProductImageReferenceServiceTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2];

    [Fact]
    public async Task PutIsImmutableAndSameBytesAreIdempotent()
    {
        var storage = new FakeProductImageReferenceStorage();
        var service = new ProductImageReferenceService(storage);

        var created = await service.PutAsync("12345", "12345.foreground.png", "image/png", Png, default);
        var repeated = await service.PutAsync("12345", "12345.foreground.png", "image/png", Png, default);
        var conflictingBytes = Png.Concat(new byte[] { 3 }).ToArray();
        var conflict = await Assert.ThrowsAsync<BaseException>(() =>
            service.PutAsync("12345", "12345.foreground.png", "image/png", conflictingBytes, default));

        Assert.True(created.Created);
        Assert.False(repeated.Created);
        Assert.Equal(Png.LongLength, repeated.Reference.Length);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task GetReturnsImageBytesAndMissingReferenceIsNotFound()
    {
        var service = new ProductImageReferenceService(new FakeProductImageReferenceStorage());
        await service.PutAsync("12345", "legacy.jpg", "image/jpeg", [255, 216, 255, 1], default);

        var image = await service.GetImageAsync("12345", "legacy.jpg", default);
        var missing = await service.GetImageAsync("12345", "missing.jpg", default);

        Assert.Equal(new byte[] { 255, 216, 255, 1 }, image);
        Assert.Null(missing);
    }

    [Fact]
    public async Task TombstoneWinsOverOrphanImageAndCannotBeResurrected()
    {
        var storage = new FakeProductImageReferenceStorage { KeepImageWhenDeleted = true };
        var service = new ProductImageReferenceService(storage);
        await service.PutAsync("12345", "legacy.jpg", "image/jpeg", [255, 216, 255, 1], default);

        await service.DeleteAsync("12345", "legacy.jpg", default);
        var snapshot = await service.GetAllAsync(default);
        var reference = Assert.Single(snapshot);
        var getError = await Assert.ThrowsAsync<BaseException>(() =>
            service.GetImageAsync("12345", "legacy.jpg", default));
        var putError = await Assert.ThrowsAsync<BaseException>(() =>
            service.PutAsync("12345", "legacy.jpg", "image/jpeg", [255, 216, 255, 1], default));

        Assert.True(reference.Deleted);
        Assert.Equal(0, reference.Length);
        Assert.Equal(410, getError.StatusCode);
        Assert.Equal(410, putError.StatusCode);
        await service.DeleteAsync("12345", "legacy.jpg", default);
    }

    [Theory]
    [InlineData(".shared-sync", "product.foreground.png", "image/png", 400)]
    [InlineData(".SHARED-SYNC", "product.foreground.png", "image/png", 400)]
    [InlineData("12345", "../product.foreground.png", "image/png", 400)]
    [InlineData("12345", "product.foreground.png", "image/jpeg", 400)]
    public async Task InvalidIdentityOrTypeIsRejected(
        string sku,
        string fileName,
        string contentType,
        int expectedStatusCode)
    {
        var service = new ProductImageReferenceService(new FakeProductImageReferenceStorage());
        var error = await Assert.ThrowsAsync<BaseException>(() =>
            service.PutAsync(sku, fileName, contentType, Png, default));

        Assert.Equal(expectedStatusCode, error.StatusCode);
    }

    internal sealed class FakeProductImageReferenceStorage : IProductImageReferenceStorage
    {
        private readonly Dictionary<(string Sku, string FileName), ProductImageReferenceStorageImage> images = [];
        private readonly HashSet<(string Sku, string FileName)> tombstones = [];
        private static readonly DateTimeOffset Modified = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        public bool KeepImageWhenDeleted { get; init; }

        public Task<IReadOnlyList<ProductImageReferenceStorageObject>> ListAsync(CancellationToken cancellationToken)
        {
            var objects = images.Select(pair => new ProductImageReferenceStorageObject(
                    $"images/{pair.Key.Sku}/{pair.Key.FileName}",
                    pair.Value.Content.LongLength,
                    pair.Value.LastModifiedUtc))
                .Concat(tombstones.Select(pair => new ProductImageReferenceStorageObject(
                    $"tombstones/{pair.Sku}/{pair.FileName}", 0, Modified)))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ProductImageReferenceStorageObject>>(objects);
        }

        public Task<ProductImageReferenceStorageImage?> GetImageAsync(
            string sku,
            string fileName,
            CancellationToken cancellationToken)
        {
            images.TryGetValue((sku, fileName), out var image);
            return Task.FromResult(image);
        }

        public Task<bool> IsDeletedAsync(string sku, string fileName, CancellationToken cancellationToken) =>
            Task.FromResult(tombstones.Contains((sku, fileName)));

        public Task<bool> PutImageIfAbsentAsync(
            string sku,
            string fileName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var key = (sku, fileName);
            if (images.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            images[key] = new ProductImageReferenceStorageImage(content.ToArray(), Modified);
            return Task.FromResult(true);
        }

        public Task CreateTombstoneIfAbsentAsync(string sku, string fileName, CancellationToken cancellationToken)
        {
            tombstones.Add((sku, fileName));
            return Task.CompletedTask;
        }

        public Task DeleteImageAsync(string sku, string fileName, CancellationToken cancellationToken)
        {
            if (!KeepImageWhenDeleted)
            {
                images.Remove((sku, fileName));
            }

            return Task.CompletedTask;
        }
    }
}
