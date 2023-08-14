using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
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


}