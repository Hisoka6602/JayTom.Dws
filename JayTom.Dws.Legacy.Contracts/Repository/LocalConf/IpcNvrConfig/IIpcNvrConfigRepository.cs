using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.IpcNvrConfig {

    public interface IIpcNvrConfigRepository : IMemoryCacheRepository<IpcNvrConfigInfoModel> {
    }
}