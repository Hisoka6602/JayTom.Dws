using Prism.Mvvm;
using System.Windows;
using System.Windows.Input;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class CombinedWorkflowNodeModel : BindableBase {
        private CombinedWorkflowNodeType _type;
        private string _name = string.Empty;
        private string _rule = string.Empty;
        private ICommand? _ruleCommand;
        private ICommand? _instructionCommand;
        private double _left;
        private double _top;
        private bool _isInCanvas;
        private bool _connectPositiveResult;
        private bool _connectNegativeResult;
        private Point _positiveConnectionPoint;
        private Point _negativeConnectionPoint;
        private Point _receiverConnectionPoint;
        private int _id;
        private int _parentId;

        public int Id {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int ParentId {
            get => _parentId;
            set => SetProperty(ref _parentId, value);
        }

        /// <summary>
        /// 节点类型
        /// </summary>
        public CombinedWorkflowNodeType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 规则内容
        /// </summary>
        public string Rule {
            get => _rule;
            set => SetProperty(ref _rule, value);
        }

        /// <summary>
        /// 查看规则
        /// </summary>
        public ICommand? RuleCommand {
            get => _ruleCommand;
            set => SetProperty(ref _ruleCommand, value);
        }

        /// <summary>
        /// 查看指令
        /// </summary>
        public ICommand? InstructionCommand {
            get => _instructionCommand;
            set => SetProperty(ref _instructionCommand, value);
        }

        /// <summary>
        /// 左边位置
        /// </summary>
        public double Left {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        /// <summary>
        /// 上边位置
        /// </summary>
        public double Top {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        /// <summary>
        /// 是否在画布中显示
        /// </summary>
        public bool IsInCanvas {
            get => _isInCanvas;
            set => SetProperty(ref _isInCanvas, value);
        }

        /// <summary>
        /// 是否连接到肯定结果(针对规则类型)
        /// </summary>
        public bool ConnectPositiveResult {
            get => _connectPositiveResult;
            set => SetProperty(ref _connectPositiveResult, value);
        }

        /// <summary>
        /// 是否连接到否定结果 (针对规则类型)
        /// </summary>
        public bool ConnectNegativeResult {
            get => _connectNegativeResult;
            set => SetProperty(ref _connectNegativeResult, value);
        }
        /// <summary>
        /// 肯定连接点位置
        /// </summary>
        public Point PositiveConnectionPoint {
            get => _positiveConnectionPoint;
            set => SetProperty(ref _positiveConnectionPoint, value);
        }

        /// <summary>
        /// 否定连接点位置
        /// </summary>
        public Point NegativeConnectionPoint {
            get => _negativeConnectionPoint;
            set => SetProperty(ref _negativeConnectionPoint, value);
        }

        /// <summary>
        /// 接收连接到位置
        /// </summary>
        public Point ReceiverConnectionPoint {
            get => _receiverConnectionPoint;
            set => SetProperty(ref _receiverConnectionPoint, value);
        }
    }

    public enum CombinedWorkflowNodeType {

        /// <summary>
        /// 出口节点
        /// </summary>
        ExitNode,

        /// <summary>
        /// 条码判断节点
        /// </summary>
        BarcodeNode,

        /// <summary>
        /// 重量判断节点
        /// </summary>
        WeightNode,

        /// <summary>
        /// 体积判断节点
        /// </summary>
        VolumeNode,

        /// <summary>
        /// OCR判断节点
        /// </summary>
        OcrNode,

        /// <summary>
        /// API判断节点
        /// </summary>
        ApiNode,
        /// <summary>
        /// 连接线
        /// </summary>
        ConnectionLine,
    }

    public class ConnectionLine {
        public Point StartPoint { get; set; }
        public Point BendPoint { get; set; }
        public Point EndPoint { get; set; }
        //线颜色
    }
}