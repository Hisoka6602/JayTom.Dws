using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Domain.Dto
{

    public class SortingMethodDto {

        /// <summary>
        /// 选中的分拣模式
        /// </summary>
        public SortMode SortMode { get; set; }
    }
}