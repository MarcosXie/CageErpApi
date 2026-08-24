using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;

namespace FlyGates.Repository.Mapper;

public class DomainToDto : Profile
{
    public DomainToDto()
    {
        CreateMap<CageOutEmployee, CageOutEmployeeResponseDto>().ReverseMap();
        CreateMap<CageOutReject, CageOutRejectResponseDto>().ReverseMap();
    }
}