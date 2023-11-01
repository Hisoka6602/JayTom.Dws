using JayTom.Dws.Client.Models.PackageSorting;
using System.Windows;
using System.Windows.Controls;

namespace JayTom.Dws.Client.DataTemplateSelectors {

    public class CombinedWorkflowNodeDataTemplateSelector : DataTemplateSelector {

        /// <summary>
        /// 出口节点模板
        /// </summary>
        public DataTemplate? ExitTemplate { get; set; }

        /// <summary>
        /// 条码规则节点模板
        /// </summary>
        public DataTemplate? BarcodeRuleTemplate { get; set; }

        /// <summary>
        /// 重量规则节点模板
        /// </summary>
        public DataTemplate? WeightRuleTemplate { get; set; }

        // 体积规则节点模板
        public DataTemplate? VolumeRuleTemplate { get; set; }

        /// <summary>
        /// OCR规则节点模板
        /// </summary>
        public DataTemplate? OcrRuleTemplate { get; set; }

        /// <summary>
        /// API规则节点模板
        /// </summary>
        public DataTemplate? ApiRuleTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container) {
            if (item is not CombinedWorkflowNodeModel itemTyeItem) return null;
            return itemTyeItem.Type switch {
                CombinedWorkflowNodeType.ExitNode => ExitTemplate,
                CombinedWorkflowNodeType.BarcodeNode => BarcodeRuleTemplate,
                CombinedWorkflowNodeType.WeightNode => WeightRuleTemplate,
                CombinedWorkflowNodeType.VolumeNode => VolumeRuleTemplate,
                CombinedWorkflowNodeType.OcrNode => OcrRuleTemplate,
                CombinedWorkflowNodeType.ApiNode => ApiRuleTemplate,
                _ => null
            };
        }
    }
}