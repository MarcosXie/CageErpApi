namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public class CageOutEmployeeAuthResultDto
{
    public Guid Id { get; set; }
    public List<string> AllowedProcedures { get; set; } = [];
}
