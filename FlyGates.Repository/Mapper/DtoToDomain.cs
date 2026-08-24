using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;

namespace FlyGates.Repository.Mapper;

public class DtoToDomain : Profile
{
    public DtoToDomain()
    {
	    CreateMap<CageOutEmployeeDto, CageOutEmployee>();
	    CreateMap<CageOutRejectDto, CageOutReject>();
	}
}
