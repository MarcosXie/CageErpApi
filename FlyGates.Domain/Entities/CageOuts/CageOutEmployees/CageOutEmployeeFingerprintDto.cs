namespace FlyGates.Application.Entities.CageOuts.CageOutEmployees;

public class CageOutEmployeeFingerprintDto
{
    /// <summary>Template (TextFIR) retornado pelo SDK do leitor; string vazia remove a digital cadastrada.</summary>
    public required string FingerprintData { get; set; }
}
