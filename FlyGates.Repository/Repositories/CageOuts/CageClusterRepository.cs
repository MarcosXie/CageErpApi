using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageClusters;
using FlyGates.Domain.Dao;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageClusterRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageClusterDao, CageCluster>(context, mapper), ICageClusterRepository;