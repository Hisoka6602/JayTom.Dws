using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Scale.ScaleValueParameters {

    public class DefaultDynamicScaleValueParameters : BaseScaleValueParameters {

        /// <summary>
        /// 保留的小数位数
        /// </summary>
        public int DecimalPlaces { get; set; } = 3;
    }
}