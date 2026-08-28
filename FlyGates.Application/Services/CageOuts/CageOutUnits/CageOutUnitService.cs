using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;

namespace FlyGates.Application.Services.CageOuts.CageOutUnits;

public interface ICageOutUnitService
{
    Task<CageOutUnitResponseDto> CreateAsync(CageOutUnitDto dto);
    Task UpdateAsync(Guid id, CageOutUnitDto dto);
    Task DeleteAsync(Guid id);
    Task<CageOutUnitResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutUnitResponseDto>> GetAllAsync();
}

public class CageOutUnitService(ICageOutUnitRepository repository, IMapper mapper) : ICageOutUnitService
{
    public async Task<CageOutUnitResponseDto> CreateAsync(CageOutUnitDto dto)
    {
        var entity = mapper.Map<CageOutUnit>(dto);
        var id = await repository.CreateAsync(entity);
        var created = await repository.GetByIdAsync(id);
        return mapper.Map<CageOutUnitResponseDto>(created);
    }

    public async Task UpdateAsync(Guid id, CageOutUnitDto dto)
    {
        var entity = await repository.GetByIdAsync(id);
        mapper.Map(dto, entity);
        await repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    public async Task<CageOutUnitResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        return mapper.Map<CageOutUnitResponseDto>(entity);
    }

    public async Task<List<CageOutUnitResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return mapper.Map<List<CageOutUnitResponseDto>>(entities);
    }
}
