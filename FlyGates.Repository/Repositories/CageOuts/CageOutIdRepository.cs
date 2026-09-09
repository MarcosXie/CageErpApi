using AutoMapper;
using FlyGates.Application.Entities.CageOuts.CageOutIds;
using FlyGates.Domain.Dao;
using FlyGates.Repository.Context;

namespace FlyGates.Repository.Repositories.CageOuts;

public class CageOutIdRepository(FlyGatesDbContext context, IMapper mapper)
    : BaseRepository<CageOutIdDao, CageOutId>(context, mapper), ICageOutIdRepository;