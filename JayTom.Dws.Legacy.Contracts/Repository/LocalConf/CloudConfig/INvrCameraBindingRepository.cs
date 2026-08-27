using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.CloudConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CloudConfig {

    public interface INvrCameraBindingRepository : IMemoryCacheRepository<NvrCameraBindingInfoModel> {
    }
}