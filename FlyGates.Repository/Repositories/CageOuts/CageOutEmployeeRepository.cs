using AutoMapper;
using FlyGates.Application.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutEmployees;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutEmployeeRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutEmployeeDao, CageOutEmployee>(context, mapper), ICageOutEmployeeRepository
{
}
