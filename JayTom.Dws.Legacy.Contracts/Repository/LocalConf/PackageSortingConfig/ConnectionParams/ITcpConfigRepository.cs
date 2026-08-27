using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams {

    public interface ITcpConfigRepository : IRepository<TcpConfigInfoModel> {
    }
}