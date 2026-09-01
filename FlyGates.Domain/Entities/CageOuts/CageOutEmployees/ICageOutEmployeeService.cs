namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public interface ICageOutEmployeeService
{
    Task<CageOutEmployeeResponseDto> CreateAsync(CageOutEmployeeDto cageOutEmployeeDto);
    Task UpdateAsync(Guid id, CageOutEmployeeUpdateDto cageOutEmployeeUpdateDto);
    Task DeleteAsync(Guid id);
    Task<CageOutEmployeeResponseDto> GetByIdAsync(Guid id);
    Task<List<CageOutEmployeeResponseDto>> GetAllAsync();
    Task<bool> IsValidBadgeAsync(string badgeCode);
    Task<CageOutEmployeeAuthResultDto?> AuthenticateAsync(CageOutEmployeeAuthDto request);
}
