using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ImageSettingModels {
    public class ItemBaseTemplateModel : BindableBase {
        private int _id;
        private int _type;
        private string _content = string.Empty;
        private ItemApplicationType _applicationType;

        /// <summary>
        /// Id
        /// </summary>
        public int Id {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 类型(0=编辑框、1=按钮)
        /// </summary>
        public int Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 实际内容
        /// </summary>
        public string Content {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// 应用类型
        /// </summary>
        public ItemApplicationType ApplicationType {
            get => _applicationType;
            set => SetProperty(ref _applicationType, value);
        }
    }

    public enum ItemApplicationType {

        /// <summary>
        /// 水印
        /// </summary>
        Watermark = 0,

        /// <summary>
        /// 子路径
        /// </summary>
        SubDirectory = 1,

        /// <summary>
        /// 图片命名
        /// </summary>
        ImageNaming = 2
    }
}