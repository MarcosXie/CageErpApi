namespace FlyGates.Application.Entities.CageOuts.CageOutRejects;

public interface ICageOutRejectService
{
    Task<CageOutRejectResponseDto> CreateAsync(CageOutRejectDto cageOutRejectDto);
    Task<List<CageOutRejectResponseDto>> GetAllAsync();
    Task<CageOutRejectResponseDto> ResolveAsync(Guid id);
    Task<CageOutRejectResponseDto> UpdateVideoAsync(Guid id, string productVideo);
}
