using AutoMapper;
using FlyGates.Domain.Dao;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutClientRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutClientDao, CageOutClient>(context, mapper), ICageOutClientRepository
{
}
