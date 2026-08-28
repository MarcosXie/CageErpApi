using AutoMapper;
using FlyGates.Application.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Domain.Dao;

namespace FlyGates.Repository.Mapper;

public class DaoMapper : Profile
{
    public DaoMapper()
    {
		CreateMap<CageOutClientDao, CageOutClient>().ReverseMap();
		CreateMap<CageOutUnitDao, CageOutUnit>().ReverseMap();
		CreateMap<CageOutEmployeeDao, CageOutEmployee>().ReverseMap();
		CreateMap<CageOutRejectDao, CageOutReject>().ReverseMap();
		CreateMap<CageOutTransactionDao, CageOutTransaction>().ReverseMap();
		CreateMap<CageOutTransactionItemDao, CageOutTransactionItem>().ReverseMap();
	}
}
