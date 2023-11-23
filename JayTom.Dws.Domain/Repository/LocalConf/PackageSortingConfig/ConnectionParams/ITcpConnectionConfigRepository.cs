using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams {

    public interface ITcpConnectionConfigRepository : IRepository<TcpConnectionConfigInfoModel> {
    }
}