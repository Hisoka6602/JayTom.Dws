using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 过滤或拦截
    /// </summary>
    public interface IFilterPlugin : IPlugin {

        /// <summary>
        /// 执行过滤
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="weight"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="volume"></param>
        /// <param name="image"></param>
        /// <param name="panoramaImage"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, BarCodeResult>> Execute([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, Image? image = default, Image? panoramaImage = default, CancellationToken token = default);

        /// <summary>
        /// 执行过滤
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="weight"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, BarCodeResult>> Execute([NotNull] string barcode, [NotNull] double weight, CancellationToken token = default);

        /// <summary>
        /// 执行过滤
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, BarCodeResult>> Execute([NotNull] string barcode, CancellationToken token = default);
    }

    public class BarCodeResult {
        public string? Barcode { get; set; }
        public double Weight { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Volume { get; set; }
        public Image? Image { get; set; }
        public Image? PanoramaImage { get; set; }
    }
}