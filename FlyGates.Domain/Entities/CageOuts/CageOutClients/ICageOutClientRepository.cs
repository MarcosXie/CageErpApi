using FlyGates.Application.Entities.Shared;
using FlyGates.Application.Entities.CageOuts.CageOutClients;
using FlyGates.Domain.Dao;

namespace FlyGates.Application.Entities.CageOuts.CageOutClients;

public interface ICageOutClientRepository : IBaseRepository<CageOutClientDao, CageOutClient>;
