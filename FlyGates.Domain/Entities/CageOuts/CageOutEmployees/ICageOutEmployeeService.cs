namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public interface ICageOutEmployeeService
{
    Task<CageOutEmployeeResponseDto> CreateAsync(CageOutEmployeeDto cageOutEmployeeDto);
    Task UpdateAsync(Guid id, CageOutEmployeeDto cageOutEmployeeDto);
    Task DeleteAsync(Guid id);
    Task<CageOutEmployeeResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutEmployeeResponseDto>> GetAllAsync();
    Task<CageOutEmployeeAuthResultDto?> AuthenticateAsync(CageOutEmployeeAuthDto request);
}
