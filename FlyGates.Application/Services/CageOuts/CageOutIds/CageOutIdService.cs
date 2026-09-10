using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Application.Exceptions;

namespace FlyGates.Application.Services.CageOuts.CageOutIds;

public interface ICageOutIdService
{
    Task<CageOutIdResponseDto> CreateAsync(CageOutIdDto dto);
    Task UpdateAsync(Guid id, CageOutIdDto dto);
    Task DeleteAsync(Guid id);
    Task<CageOutIdResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutIdResponseDto>> GetAllAsync();
    Task HeartbeatAsync(string identifier);
}

public class CageOutIdService(
    ICageOutIdRepository repository,
    ICageOutUnitRepository unitRepository,
    IMapper mapper) : ICageOutIdService
{
    public async Task<CageOutIdResponseDto> CreateAsync(CageOutIdDto dto)
    {
        await ValidateAsync(dto);
        var entity = mapper.Map<CageOutId>(dto);
        var id = await repository.CreateAsync(entity);
        return mapper.Map<CageOutIdResponseDto>(await repository.GetByIdAsync(id));
    }

    public async Task UpdateAsync(Guid id, CageOutIdDto dto)
    {
        await ValidateAsync(dto, id);
        var entity = await repository.GetByIdAsync(id);
        mapper.Map(dto, entity);
        await repository.UpdateAsync(entity);
    }

    public Task DeleteAsync(Guid id) => repository.DeleteAsync(id);

    public async Task<CageOutIdResponseDto> GetByIdAsync(Guid id) =>
        mapper.Map<CageOutIdResponseDto>(await repository.GetByIdAsync(id));

    public async Task<List<CageOutIdResponseDto>> GetAllAsync() =>
        mapper.Map<List<CageOutIdResponseDto>>(await repository.GetAsync());

    public async Task HeartbeatAsync(string identifier)
    {
        var entity = await repository.FirstOrDefaultAsync(x => x.Identifier == identifier)
            ?? throw new NotFoundException("Cage ID");
        // Mesma convenção de CreatedAt/UpdatedAt (hora local do servidor), não UTC —
        // evita desalinhamento de 3h ao comparar com "agora" no front (América/São Paulo).
        entity.LastSeenAt = DateTime.Now;
        await repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(CageOutIdDto dto, Guid? currentId = null)
    {
        dto.Identifier = dto.Identifier.Trim();
        if (string.IsNullOrWhiteSpace(dto.Identifier) || dto.Identifier.Any(char.IsWhiteSpace))
            throw new BadRequestException("O identificador é obrigatório e não pode conter espaços.");
        if (dto.Identifier.Length > 80)
            throw new BadRequestException("O identificador deve ter no máximo 80 caracteres.");

        _ = await unitRepository.GetByIdAsync(dto.UnitId);

        var duplicate = (await repository.GetAsync()).Any(item =>
            item.Id != currentId &&
            string.Equals(item.Identifier, dto.Identifier, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
            throw new BadRequestException("Já existe um Cage ID com este identificador.");
    }
}