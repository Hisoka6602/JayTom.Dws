using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;

namespace JayTom.Dws.Domain.Repository.License {

    public interface ILicenseCodeRepository : IRepository<LicenseCodeInfo> {
    }
}