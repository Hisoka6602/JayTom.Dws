using JayTom.Dws.Client.Models.ImageSettingModels;
using System.Windows;
using System.Windows.Controls;

namespace JayTom.Dws.Client.DataTemplateSelectors {

    public class ImageSettingItemDataTemplateSelectors : DataTemplateSelector {

        /// <summary>
        /// 按钮
        /// </summary>
        public DataTemplate? ButtonTemplate { get; set; }

        /// <summary>
        /// 输入框
        /// </summary>
        public DataTemplate? TextBoxTemplate { get; set; }

        /// <summary>
        /// 分隔符
        /// </summary>
        public DataTemplate? SeparatorTemplate { get; set; }

        /// <summary>
        /// 自定义内容
        /// </summary>
        public DataTemplate? CustomTemplate { get; set; }

        /// <summary>
        /// 运算符
        /// </summary>
        public DataTemplate? OperatorTemplate { get; set; }

        /// <summary>
        /// 参照值
        /// </summary>
        public DataTemplate? ReferenceValueTemplate { get; set; }

        /// <summary>
        /// 拼接符
        /// </summary>
        public DataTemplate? StitchingTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container) {
            if (item is not ItemBaseTemplateModel itemTyeItem) return null;
            return itemTyeItem.Type switch {
                0 => TextBoxTemplate,
                1 => ButtonTemplate,
                2 => SeparatorTemplate,
                3 => CustomTemplate,
                4 => OperatorTemplate,
                5 => ReferenceValueTemplate,
                6 => StitchingTemplate,
                _ => null
            };
        }
    }
}