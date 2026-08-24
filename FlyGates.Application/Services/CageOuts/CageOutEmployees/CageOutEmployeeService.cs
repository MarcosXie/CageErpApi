using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Utils;

namespace FlyGates.Application.Services.CageOuts.CageOutEmployees;

public class CageOutEmployeeService(ICageOutEmployeeRepository repository, IMapper mapper) : ICageOutEmployeeService
{
    public async Task<CageOutEmployeeResponseDto> CreateAsync(CageOutEmployeeDto cageOutEmployeeDto)
    {
        var entity = mapper.Map<CageOutEmployee>(cageOutEmployeeDto);
        entity.Password = HashHelper.Hash(cageOutEmployeeDto.Password);

        var id = await repository.CreateAsync(entity);
        var created = await repository.GetByIdAsync(id);

        return mapper.Map<CageOutEmployeeResponseDto>(created);
    }

    public async Task UpdateAsync(Guid id, CageOutEmployeeDto cageOutEmployeeDto)
    {
        var dbEmployee = await repository.GetByIdAsync(id);
        var currentPassword = dbEmployee.Password;

        mapper.Map(cageOutEmployeeDto, dbEmployee);

        if (string.IsNullOrEmpty(cageOutEmployeeDto.Password))
            dbEmployee.Password = currentPassword;
        else
            dbEmployee.Password = HashHelper.Hash(cageOutEmployeeDto.Password);

        await repository.UpdateAsync(dbEmployee);
    }

    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    public async Task<CageOutEmployeeResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        return mapper.Map<CageOutEmployeeResponseDto>(entity);
    }

    public async Task<List<CageOutEmployeeResponseDto>> GetAllAsync()
    {
        var entities = await repository.GetAsync();
        return mapper.Map<List<CageOutEmployeeResponseDto>>(entities);
    }


    public async Task<CageOutEmployeeAuthResultDto?> AuthenticateAsync(CageOutEmployeeAuthDto request)
    {
        var matches = await repository.GetAsync(e => e.BadgeCode == request.BadgeCode);
        var employee = matches.FirstOrDefault(e => PasswordMatches(request.Password, e.Password));

        if (employee is null)
            return null;

        return new CageOutEmployeeAuthResultDto
        {
            Id = employee.Id,
            AllowedProcedures = employee.AllowedProcedures
        };
    }

    private static bool PasswordMatches(string password, string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
            return false;

        try
        {
            return HashHelper.Verify(password, storedPassword);
        }
        catch
        {
            // Backward compatibility for records that were stored without hash.
            return storedPassword == password;
        }
    }
}
