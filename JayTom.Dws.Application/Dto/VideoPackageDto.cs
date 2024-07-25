using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.CloudDto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Application.Dto {

    public class VideoPackageDto {

        /// <summary>
        /// 条数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 实体
        /// </summary>
        public List<PackageInfoModel> Packages { get; set; } = new();
    }
}