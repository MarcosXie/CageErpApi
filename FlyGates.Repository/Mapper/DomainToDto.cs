using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;

namespace FlyGates.Repository.Mapper;

public class DomainToDto : Profile
{
    public DomainToDto()
    {
        CreateMap<CageOutClient, CageOutClientResponseDto>().ReverseMap();
        CreateMap<CageOutUnit, CageOutUnitResponseDto>().ReverseMap();
        CreateMap<CageOutEmployee, CageOutEmployeeResponseDto>().ReverseMap();
        CreateMap<CageOutReject, CageOutRejectResponseDto>().ReverseMap();
        CreateMap<CageOutTransaction, CageOutTransactionResponseDto>().ReverseMap();
        CreateMap<CageOutTransactionItem, CageOutTransactionItemResponseDto>().ReverseMap();
    }
}