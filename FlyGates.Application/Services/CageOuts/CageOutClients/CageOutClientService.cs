using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.CageOuts.CageOutClients;

public interface ICageOutClientService
{
    Task<CageOutClientResponseDto> CreateAsync(CageOutClientDto dto);
    Task UpdateAsync(Guid id, CageOutClientDto dto);
    Task DeleteAsync(Guid id);
    Task<CageOutClientResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutClientResponseDto>> GetAllAsync();
    Task<CageOutClientResponseDto> UploadBackgroundImageAsync(Guid id, Stream content, string contentType, long contentLength);
    Task<CageOutClientResponseDto> RemoveBackgroundImageAsync(Guid id);
}

public class CageOutClientService(
    ICageOutClientRepository repository,
    IMediaStorageService mediaStorage,
    IMapper mapper) : ICageOutClientService
{
    private const long MaxBackgroundImageBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
    };

    public async Task<CageOutClientResponseDto> CreateAsync(CageOutClientDto dto)
    {
        var entity = mapper.Map<CageOutClient>(dto);
        var id = await repository.CreateAsync(entity);
        var created = await repository.GetByIdAsync(id);
        return ToResponse(created);
    }

    public async Task UpdateAsync(Guid id, CageOutClientDto dto)
    {
        var entity = await repository.GetByIdAsync(id);
        mapper.Map(dto, entity);
        await repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    public async Task<CageOutClientResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        return ToResponse(entity);
    }

    public async Task<List<CageOutClientResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return entities.Select(ToResponse).ToList();
    }

    public async Task<CageOutClientResponseDto> UploadBackgroundImageAsync(Guid id, Stream content, string contentType, long contentLength)
    {
        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
            throw new BadRequestException("Formato de imagem inválido. Envie um arquivo JPG ou PNG.");
        if (contentLength <= 0 || contentLength > MaxBackgroundImageBytes)
            throw new BadRequestException("A imagem deve ter no máximo 5MB.");

        var entity = await repository.GetByIdAsync(id);
        var previousKey = entity.BackgroundImageKey;

        // Chave nova a cada upload: permite ao CageOuts detectar a troca comparando apenas a chave.
        var objectKey = $"clients/backgrounds/{id}/{Guid.NewGuid()}{extension}";
        await mediaStorage.UploadObjectAsync(content, objectKey, contentType).ConfigureAwait(false);

        entity.BackgroundImageKey = objectKey;
        await repository.UpdateAsync(entity).ConfigureAwait(false);

        await mediaStorage.DeleteObjectAsync(previousKey).ConfigureAwait(false);

        return ToResponse(entity);
    }

    public async Task<CageOutClientResponseDto> RemoveBackgroundImageAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        var previousKey = entity.BackgroundImageKey;

        entity.BackgroundImageKey = null;
        await repository.UpdateAsync(entity).ConfigureAwait(false);

        await mediaStorage.DeleteObjectAsync(previousKey).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private CageOutClientResponseDto ToResponse(CageOutClient entity)
    {
        var response = mapper.Map<CageOutClientResponseDto>(entity);
        response.BackgroundImageUrl = mediaStorage.GeneratePresignedUrl(entity.BackgroundImageKey);
        return response;
    }
}
