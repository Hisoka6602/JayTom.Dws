using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Legacy.Contracts.Dto
{

    public class SortingMethodDto {

        /// <summary>
        /// 选中的分拣模式
        /// </summary>
        public SortMode SortMode { get; set; }
    }
}