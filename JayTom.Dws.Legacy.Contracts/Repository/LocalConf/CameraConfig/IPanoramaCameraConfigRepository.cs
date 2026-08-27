using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.CameraConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig {

    public interface IPanoramaCameraConfigRepository : IMemoryCacheRepository<PanoramaCameraConfigInfoModel> {
    }
}