using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;

namespace FlyGates.Repository.Mapper;

public class DtoToDomain : Profile
{
    public DtoToDomain()
    {
	    CreateMap<CageOutClientDto, CageOutClient>();
	    CreateMap<CageOutUnitDto, CageOutUnit>();
	    CreateMap<CageOutEmployeeDto, CageOutEmployee>();
	    CreateMap<CageOutRejectDto, CageOutReject>();
	    CreateMap<CageOutTransactionDto, CageOutTransaction>();
	    CreateMap<CageOutTransactionItemDto, CageOutTransactionItem>();
	}
}
