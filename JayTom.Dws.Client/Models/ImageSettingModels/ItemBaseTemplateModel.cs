using JayTom.Dws.Domain.Dto.BaseInfoModels;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ImageSettingModels
{

    public class ItemBaseTemplateModel : BindableBase
    {
        private long _id;
        private int _type;
        private string _content = string.Empty;
        private ItemApplicationType _applicationType;

        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 类型(0=编辑框、1=按钮、2=分隔符、3=自定义、4=运算符、5=参照值、6=拼接符(函数))
        /// </summary>
        public int Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 实际内容
        /// </summary>
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// 应用类型
        /// </summary>
        public ItemApplicationType ApplicationType
        {
            get => _applicationType;
            set => SetProperty(ref _applicationType, value);
        }
    }
}
