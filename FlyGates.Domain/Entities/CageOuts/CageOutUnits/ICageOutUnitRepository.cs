using FlyGates.Application.Entities.Shared;
using FlyGates.Application.Entities.CageOuts.CageOutUnits;
using FlyGates.Domain.Dao;

namespace FlyGates.Application.Entities.CageOuts.CageOutUnits;

public interface ICageOutUnitRepository : IBaseRepository<CageOutUnitDao, CageOutUnit>;
