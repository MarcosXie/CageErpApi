using AutoMapper;
using FlyGates.Application.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Application.Entities.CageOuts.CageOutTransactions;

namespace FlyGates.Repository.Mapper;

public class DaoMapper : Profile
{
    public DaoMapper()
    {
		CreateMap<CageOutEmployeeDao, CageOutEmployee>().ReverseMap();
		CreateMap<CageOutRejectDao, CageOutReject>().ReverseMap();
		CreateMap<CageOutTransactionDao, CageOutTransaction>().ReverseMap();
		CreateMap<CageOutTransactionItemDao, CageOutTransactionItem>().ReverseMap();
	}
}
