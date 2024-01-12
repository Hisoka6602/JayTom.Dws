using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.License {

    public interface ILicensePermissionTemplateRepository : IMemoryCacheRepository<LicensePermissionTemplateInfo> {

        public Task<KeyValuePair<bool, object>> Details([NotNull] Expression<Func<LicensePermissionTemplateInfo, bool>> @where, CancellationToken token = default);

        public Task<KeyValuePair<bool, object>> FirstDetails([NotNull] Expression<Func<LicensePermissionTemplateInfo, bool>> @where, CancellationToken token = default);
    }
}