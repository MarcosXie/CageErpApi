using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;

namespace FlyGates.Repository.Mapper;

public class DtoToDomain : Profile
{
    public DtoToDomain()
    {
	    CreateMap<CageOutEmployeeDto, CageOutEmployee>();
	    CreateMap<CageOutRejectDto, CageOutReject>();
	    CreateMap<CageOutTransactionDto, CageOutTransaction>();
	    CreateMap<CageOutTransactionItemDto, CageOutTransactionItem>();
	}
}
