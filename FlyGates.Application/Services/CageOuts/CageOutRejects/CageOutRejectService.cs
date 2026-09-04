using AutoMapper;
using FlyGates.Application.Entities.CageOuts;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.Storage;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.CageOuts.CageOutRejects;

public class CageOutRejectService(
    ICageOutRejectRepository repository,
    IMediaStorageService mediaStorage,
    IMapper mapper) : ICageOutRejectService
{
    public async Task<CageOutRejectResponseDto> CreateAsync(CageOutRejectDto cageOutRejectDto)
    {
        var entity = mapper.Map<CageOutReject>(cageOutRejectDto);
        var id = await repository.CreateAsync(entity);
        var created = await repository.GetByIdAsync(id);
        return MapWithMedia(created);
    }

    public async Task<List<CageOutRejectResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return entities.Select(MapWithMedia).ToList();
    }

    public async Task<CageOutRejectResponseDto> ResolveAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);

        if (entity.Reason == CageOutRejectReason.Estorno)
        {
            throw new BadRequestException("Rejeitos de estorno não podem ser marcados como resolvidos.");
        }

        if (!entity.IsResolved)
        {
            await mediaStorage.DeleteObjectAsync(entity.ProductImage);
            await mediaStorage.DeleteObjectAsync(entity.ProductVideo);

            entity.ProductImage = string.Empty;
            entity.ProductVideo = string.Empty;
            entity.IsResolved = true;
            entity.ResolvedAt = DateTime.UtcNow;

            await repository.UpdateAsync(entity);
        }

        return MapWithMedia(entity);
    }

    public async Task<CageOutRejectResponseDto> UpdateVideoAsync(Guid id, string productVideo)
    {
        var entity = await repository.GetByIdAsync(id);
        entity.ProductVideo = productVideo;
        await repository.UpdateAsync(entity);
        return MapWithMedia(entity);
    }

    private CageOutRejectResponseDto MapWithMedia(CageOutReject entity)
    {
        var dto = mapper.Map<CageOutRejectResponseDto>(entity);
        dto.ProductImageUrl = mediaStorage.GeneratePresignedUrl(entity.ProductImage);
        dto.ProductVideoUrl = mediaStorage.GeneratePresignedUrl(entity.ProductVideo);
        return dto;
    }
}
