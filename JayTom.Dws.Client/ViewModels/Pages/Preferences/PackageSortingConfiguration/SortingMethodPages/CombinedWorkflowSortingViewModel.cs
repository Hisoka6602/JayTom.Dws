using GongSolutions.Wpf.DragDrop;
using JayTom.Dws.Client.Models.PackageSorting;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DragDropEffects = System.Windows.DragDropEffects;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;
using ListView = System.Windows.Controls.ListView;
using Panel = System.Windows.Controls.Panel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {
    public class CombinedWorkflowSortingViewModel : BindableBase, IDropTarget {

        private ObservableCollection<CombinedWorkflowNodeModel> _combinedWorkflowNodes = new()
        {
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.ExitNode,
                Name = "出口1"
            },
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.ExitNode,
                Name = "出口2"
            },
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.ExitNode,
                Name = "出口3"
            },
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.ExitNode,
                Name = "出口4"
            },
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.BarcodeNode,
                Name = "条码规则1"
            },
            new CombinedWorkflowNodeModel()
            {
                Type = CombinedWorkflowNodeType.BarcodeNode,
                Name = "条码规则2"
            },
        };

        private ObservableCollection<CombinedWorkflowNodeModel> _canvasNodes = new();

        public ObservableCollection<CombinedWorkflowNodeModel> CombinedWorkflowNodes {
            get => _combinedWorkflowNodes;
            set => SetProperty(ref _combinedWorkflowNodes, value);
        }

        public ObservableCollection<CombinedWorkflowNodeModel> CanvasNodes {
            get => _canvasNodes;
            set => SetProperty(ref _canvasNodes, value);
        }

        public ICommand PreviewMouseMoveDragCommand {
            get => new DelegateCommand<object>(PreviewMouseMoveDragDelegate);
        }

        private void PreviewMouseMoveDragDelegate(object obj) {
        }

        public ICommand PreviewMouseDownDragCommand {
            get => new DelegateCommand<object>(PreviewMouseDownDragDelegate);
        }

        private void PreviewMouseDownDragDelegate(object obj) {
            // 将鼠标捕获到 ListBoxItem 上，以便在鼠标移出 ListBoxItem 范围时仍然能够接收鼠标事件
        }

        public ICommand DropCommand {
            get => new DelegateCommand<DropInfo>(ExecuteDrop);
        }

        private void ExecuteDrop(DropInfo dropInfo) {
            if (dropInfo.Data is CombinedWorkflowNodeModel item) {
                item.Left = (int)dropInfo.DropPosition.X;
                item.Top = (int)dropInfo.DropPosition.Y;
                Debug.WriteLine($"{item.Left},{item.Top}");
            }
        }

        public void DragOver(IDropInfo dropInfo) {
            if (dropInfo.TargetItem == dropInfo.DragInfo.SourceItem) {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }
            if (dropInfo.VisualTarget is ItemsControl) {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
                var model = CanvasNodes.FirstOrDefault(f => !f.ConnectPositiveResult
                    || !f.ConnectPositiveResult);
                if (model != null) {
                    //获取长度宽度
                    if (dropInfo.DropPosition.Y > model.PositiveConnectionPoint.Y) {
                        //在连接对象下面
                        //取出中心点
                        var element = dropInfo.DragInfo.VisualSourceItem;
                        if (element is FrameworkElement frameworkElement) {
                            var actualWidth = frameworkElement.ActualWidth;
                            var centerPoint = actualWidth / 2;
                            if (!model.ConnectPositiveResult &&
                                dropInfo.DropPosition.X < model.Left + centerPoint) {
                                //左边
                                //画线
                                Debug.WriteLine($"在左边");
                            }
                            else if (!model.ConnectNegativeResult &&
                                   dropInfo.DropPosition.X > model.Left + centerPoint) {
                                Debug.WriteLine($"在右边");
                                //画线
                            }
                            Debug.WriteLine($"frameworkElement.X:{dropInfo.DropPosition.X}-model.X:{model.Left + centerPoint}");
                        }

                        //获取中心点在左边还是右边
                    }


                }

            }
        }

        public void Drop(IDropInfo dropInfo) {
            if (dropInfo.VisualTarget is ItemsControl) {
                var item = (CombinedWorkflowNodeModel)dropInfo.Data;
                item.Left = (int)dropInfo.DropPosition.X;
                item.Top = (int)dropInfo.DropPosition.Y;
                if (dropInfo.VisualTargetItem is not null &&
                    dropInfo.DragInfo.VisualSourceItem is not null &&
                    dropInfo.VisualTargetItem.GetType() == dropInfo.DragInfo.VisualSourceItem.GetType()) {
                    Panel.SetZIndex(dropInfo.VisualTargetItem, 29);
                    Panel.SetZIndex(dropInfo.DragInfo.VisualSourceItem, 30);
                }

                if (dropInfo.DragInfo.VisualSourceItem is FrameworkElement frameworkElement) {
                    item.PositiveConnectionPoint = new Point() {
                        X = dropInfo.DropPosition.X + 10,
                        Y = dropInfo.DropPosition.Y + frameworkElement.ActualHeight
                    };
                    item.NegativeConnectionPoint = new Point() {
                        X = dropInfo.DropPosition.X + frameworkElement.ActualWidth - 10,
                        Y = dropInfo.DropPosition.Y + frameworkElement.ActualHeight
                    };
                }

                if (dropInfo.DragInfo.VisualSource is ListView) {
                    CanvasNodes.Add(item);
                    CombinedWorkflowNodes.Remove(item);
                }
            }
        }
    }
}