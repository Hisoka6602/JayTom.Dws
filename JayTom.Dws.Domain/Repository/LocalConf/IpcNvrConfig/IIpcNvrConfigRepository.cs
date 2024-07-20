using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig {

    public interface IIpcNvrConfigRepository : IMemoryCacheRepository<IpcNvrConfigInfoModel> {
    }
}