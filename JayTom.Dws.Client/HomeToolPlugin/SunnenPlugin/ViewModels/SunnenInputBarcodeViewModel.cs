using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using PluginType = JayTom.Dws.Application.Events.PluginType;
using PluginParamChangedEvent = JayTom.Dws.Application.Events.PluginParamChangedEvent;
using BarcodeTypeProviderEvent = JayTom.Dws.Application.Events.BarcodeTypeProviderEvent;

namespace JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels
{

    public class SunnenInputBarcodeViewModel : BindableBase, IDialogAware
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private string _barCode = string.Empty;
        private PackageType _packageType = PackageType.Pallet;
        private int _deductedLength;
        private int _deductedWidth;
        private int _deductedHeight;

        /// <summary>创建条码输入展示模型。</summary>
        public SunnenInputBarcodeViewModel(
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode
        {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 包装物类型
        /// </summary>
        public PackageType PackageType
        {
            get => _packageType;
            set => SetProperty(ref _packageType, value);
        }

        /// <summary>
        /// 扣除的长度
        /// </summary>
        public int DeductedLength
        {
            get => _deductedLength;
            set => SetProperty(ref _deductedLength, value);
        }

        /// <summary>
        /// 扣除的宽度
        /// </summary>
        public int DeductedWidth
        {
            get => _deductedWidth;
            set => SetProperty(ref _deductedWidth, value);
        }

        /// <summary>
        /// 扣除的高度
        /// </summary>
        public int DeductedHeight
        {
            get => _deductedHeight;
            set => SetProperty(ref _deductedHeight, value);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.Name.Equals("SunnenInputBarcodeWindows"))
                {
                    window.Close();
                }
            }
        }

        public string Title => string.Empty;

        public event Action<IDialogResult>? RequestClose;

        public ICommand CloseWinCommand
        {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj)
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public ICommand MinWinCommand
        {
            get => new DelegateCommand<object>(MinWinDelegate);
        }

        private async void MinWinDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 获取对话框所属的窗口对象
                Window dialogWindow = System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);

                // 将窗口状态设置为最小化
                if (dialogWindow != null)
                {
                    dialogWindow.WindowState = WindowState.Minimized;
                }

                //System.Windows.Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });
        }

        public ICommand SwitchPackageCommand
        {
            get => new DelegateCommand<object>(SwitchPackageDelegate);
        }

        private void SwitchPackageDelegate(object obj)
        {
            if (!string.IsNullOrEmpty(obj.ToString()))
            {
                PackageType = obj.ToString() switch
                {
                    "Box" => PackageType.Box,
                    "Pallet" => PackageType.Pallet,
                    _ => PackageType
                };
                _eventBus.Publish(new PluginParamChangedEvent
                {
                    Type = PluginType.HomeTool,
                    PluginName = "SunnenPlugin",
                    Content = obj.ToString() ?? string.Empty
                });
            }
        }

        public ICommand BarcodeInputCommand
        {
            get => new DelegateCommand<object>(BarcodeInputDelegate);
        }

        private async void BarcodeInputDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(BarCode))
                {
                    _eventBus.Publish(new BarcodeTypeProviderEvent
                    {
                        Barcode = BarCode,

                        /*LengthToDeduct = PackageType == PackageType.Pallet ? DeductedLength : 0,
                        WidthToDeduct = PackageType == PackageType.Pallet ? DeductedWidth : 0,
                        HeightToDeduct = PackageType == PackageType.Pallet ? DeductedHeight : 0,*/
                        VolumeToDeduct = PackageType == PackageType.Pallet ? (DeductedLength * DeductedWidth * DeductedHeight) : 0,
                    });
                    BarCode = string.Empty;
                }
            });
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<UserControl>(LoadedDelegate);
        }

        private async void LoadedDelegate(UserControl obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var textBox = PluginInterface.Utils.Utils.GetVisualChild<TextBox>(obj, b => b.Name.Equals("BarCodeTextBox"));
                if (textBox is not null)
                {
                    textBox.Focus();
                }
                _eventBus.Publish(new PluginParamChangedEvent
                {
                    Type = PluginType.HomeTool,
                    PluginName = "SunnenPlugin",
                    Content = "Pallet"
                });
            });
            var dialogWindow = System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (dialogWindow is not null)
            {
                dialogWindow.Owner = null;
                dialogWindow.Name = "SunnenInputBarcodeWindows";
            }
        }
    }

    public enum PackageType
    {
        Box,
        Pallet
    }
}
