using AutoMapper;
using FlyGates.Application.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutRejects;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutRejectRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutRejectDao, CageOutReject>(context, mapper), ICageOutRejectRepository
{
}
