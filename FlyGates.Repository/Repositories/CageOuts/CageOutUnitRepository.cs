using AutoMapper;
using FlyGates.Domain.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutUnitRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutUnitDao, CageOutUnit>(context, mapper), ICageOutUnitRepository
{
}
