using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Converters {

    public static class UnitConverter {

        public static float WeightConverter1(this float weight, WeightUnit unit) {
            return unit switch {
                WeightUnit.Gram => weight * 1000,
                WeightUnit.Kilogram => weight,
                WeightUnit.Pound => (float)(weight * 2.20462),
                _ => 0
            };
        }

        /// <summary>
        /// 体积转换(以数据源为mm单位基础)
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static float VolumeConverter1(this float volume, VolumeUnit unit) {
            return unit switch {
                VolumeUnit.Centimeter => volume / 10,
                VolumeUnit.Meter => volume / 1000,
                VolumeUnit.Millimeter => volume,
                _ => 0
            };
        }

        public static double WeightConverter1(this double weight, WeightUnit unit) {
            return unit switch {
                WeightUnit.Gram => weight * 1000,
                WeightUnit.Kilogram => weight,
                WeightUnit.Pound => weight * 2.20462,
                _ => 0
            };
        }

        /// <summary>
        /// 体积转换(以数据源为mm单位基础)
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static double VolumeConverter1(this double volume, VolumeUnit unit) {
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