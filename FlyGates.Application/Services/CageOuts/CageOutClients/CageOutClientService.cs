using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutClients;

namespace FlyGates.Application.Services.CageOuts.CageOutClients;

public interface ICageOutClientService
{
    Task<CageOutClientResponseDto> CreateAsync(CageOutClientDto dto);
    Task UpdateAsync(Guid id, CageOutClientDto dto);
    Task DeleteAsync(Guid id);
    Task<CageOutClientResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutClientResponseDto>> GetAllAsync();
}

public class CageOutClientService(ICageOutClientRepository repository, IMapper mapper) : ICageOutClientService
{
    public async Task<CageOutClientResponseDto> CreateAsync(CageOutClientDto dto)
    {
        var entity = mapper.Map<CageOutClient>(dto);
        var id = await repository.CreateAsync(entity);
        var created = await repository.GetByIdAsync(id);
        return mapper.Map<CageOutClientResponseDto>(created);
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
        return mapper.Map<CageOutClientResponseDto>(entity);
    }

    public async Task<List<CageOutClientResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return mapper.Map<List<CageOutClientResponseDto>>(entities);
    }
}
