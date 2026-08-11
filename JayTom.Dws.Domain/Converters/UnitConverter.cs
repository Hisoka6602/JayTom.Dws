using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Converters {

    public static class UnitConverter {

        public static decimal WeightConverter1(this decimal weight, WeightUnit unit) {
            return unit switch {
                WeightUnit.Gram => weight * 1000,
                WeightUnit.Kilogram => weight,
                WeightUnit.Pound => weight * 2.20462m,
                _ => 0
            };
        }

        /// <summary>
        /// 体积转换(以数据源为mm单位基础)
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static decimal VolumeConverter1(this decimal volume, VolumeUnit unit) {
            return unit switch {
                VolumeUnit.Centimeter => volume / 10,
                VolumeUnit.Meter => volume / 1000,
                VolumeUnit.Millimeter => volume,
                _ => 0
            };
        }

        private static decimal LegacyWeightConverter(decimal weight, WeightUnit unit) {
            return unit switch {
                WeightUnit.Gram => weight * 1000,
                WeightUnit.Kilogram => weight,
                WeightUnit.Pound => weight * 2.20462m,
                _ => 0
            };
        }

        /// <summary>
        /// 体积转换(以数据源为mm单位基础)
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        private static decimal LegacyVolumeConverter(decimal volume, VolumeUnit unit) {
            return unit switch {
                VolumeUnit.Centimeter => volume / 10,
                VolumeUnit.Meter => volume / 1000,
                VolumeUnit.Millimeter => volume,
                _ => 0
            };
        }
    }

    /// <summary>
    /// 体积单位
    /// </summary>
    public enum VolumeUnit {

        /// <summary>
        /// 毫米(mm)
        /// </summary>
        Millimeter,

        /// <summary>
        /// 厘米(cm)
        /// </summary>
        Centimeter,

        /// <summary>
        /// 米(m)
        /// </summary>
        Meter
    }

    public enum WeightUnit {

        /// <summary>
        /// 克
        /// </summary>
        Gram,

        /// <summary>
        /// 千克
        /// </summary>
        Kilogram,

        /// <summary>
        /// 磅
        /// </summary>
        Pound
    }
}
