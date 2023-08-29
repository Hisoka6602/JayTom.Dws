using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.ImageSettingModels;

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
        public override DataTemplate? SelectTemplate(object item, DependencyObject container) {
            if (item is not ItemBaseTemplateModel itemTyeItem) return null;
            return itemTyeItem.Type switch {
                0 => TextBoxTemplate,
                1 => ButtonTemplate,
                2 => SeparatorTemplate,
                _ => null
            };
        }
    }
}