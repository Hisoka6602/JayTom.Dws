using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.VideoApiClient.Models {

    public class PanoramaImageItemModel : BindableBase {
        private string? _imageUrl;

        /// <summary>
        /// 图片地址
        /// </summary>
        public string? ImageUrl {
            get => _imageUrl;
            set => SetProperty(ref _imageUrl, value);
        }

        /// <summary>
        /// 是否显示图片
        /// </summary>
        public bool ImageVisible { get; set; }
    }
}