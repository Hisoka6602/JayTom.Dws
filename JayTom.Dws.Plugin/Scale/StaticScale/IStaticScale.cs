using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Scale.StaticScale {

    /// <summary>
    /// 静态称
    /// </summary>
    public interface IStaticScale : IScale {

        /// <summary>
        /// 实时重量
        /// </summary>
        event EventHandler<float> CurrentWeight;

        /// <summary>
        /// 重量清零
        /// </summary>
        public event EventHandler<WeightChangedEventArgs> WeightCleared;
    }

    public enum WeightAccessMode {

        /// <summary>
        /// 只读式
        /// </summary>
        Readonly,

        /// <summary>
        /// 问答式
        /// </summary>
        QuestionAnswer
    }
}