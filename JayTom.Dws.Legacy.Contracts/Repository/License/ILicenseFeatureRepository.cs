using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;

namespace JayTom.Dws.Legacy.Contracts.Repositories.License {

    public interface ILicenseFeatureRepository : IRepository<LicenseFeatureInfo> {

        public new Task<bool> DeleteRange(List<LicenseFeatureInfo> entities, CancellationToken token);
    }
}