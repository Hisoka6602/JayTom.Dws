using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Legacy.Contracts.Repositories.License {

    public interface ILicenseApplicationRepository : IMemoryCacheRepository<LicenseApplicationInfo> {

        public Task<KeyValuePair<bool, object>> Details(Expression<Func<LicenseApplicationInfo, bool>> @where, CancellationToken token = default);

        public Task<KeyValuePair<bool, object>> FirstDetails(Expression<Func<LicenseApplicationInfo, bool>> @where, CancellationToken token = default);
    }
}
