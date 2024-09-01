using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Repository.VideoApi {

    public interface IVideoConfigRepository : IMemoryCacheRepository<ConfigInfoModel> {
    }
}