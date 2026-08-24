namespace FlyGates.Application.Entities.CageOuts.CageOutRejects;

public interface ICageOutRejectService
{
    Task CreateAsync(CageOutRejectDto cageOutRejectDto);
    Task<List<CageOutRejectResponseDto>> GetAllAsync();
}
