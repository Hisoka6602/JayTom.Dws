using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudApiDto {

    public class PackageListInfoDto {
        public List<PackageInfoModel> PackageInfos { get; set; } = new();
        public int Total { get; set; }
    }
}