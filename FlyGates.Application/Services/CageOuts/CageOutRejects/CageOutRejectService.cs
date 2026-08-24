using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;

namespace FlyGates.Application.Services.CageOuts.CageOutRejects;

public class CageOutRejectService(ICageOutRejectRepository repository, IMapper mapper) : ICageOutRejectService
{
    public async Task CreateAsync(CageOutRejectDto cageOutRejectDto)
    {
        var entity = mapper.Map<CageOutReject>(cageOutRejectDto);
        await repository.CreateAsync(entity);
    }

    public async Task<List<CageOutRejectResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return mapper.Map<List<CageOutRejectResponseDto>>(entities);
    }
}
