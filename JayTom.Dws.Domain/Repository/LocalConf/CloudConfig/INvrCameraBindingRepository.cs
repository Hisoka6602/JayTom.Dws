using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.CloudConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf.CloudConfig {

    public interface INvrCameraBindingRepository : IMemoryCacheRepository<NvrCameraBindingInfoModel> {
    }
}