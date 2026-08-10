using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration
{

    public class CommunicationConnectionConfigEditorViewModel : BindableBase
    {
        private string _identifier = string.Empty;
        private ObservableCollection<string> _portItems = new();

        private ObservableCollection<CommunicationProtocolInfoModel> _communicationProtocolItems = new()
        {
            new CommunicationProtocolInfoModel()
            {
                Name = "无协议",
                Value = CommunicationProtocol.None,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Modbus",
                Value = CommunicationProtocol.ModBus,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "CC-Link",
                Value = CommunicationProtocol.CCLink,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Profibus",
                Value = CommunicationProtocol.ProfiBus,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Profinet",
                Value = CommunicationProtocol.Profinet,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "CANopen",
                Value = CommunicationProtocol.CANopen,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "无限创科协议",
                Value = CommunicationProtocol.Wxkc,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "江腾-窄带协议",
                Value = CommunicationProtocol.JT_ST,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "菜鸟分拣协议",
                Value = CommunicationProtocol.CaiNiao,
            },
        };

        private ObservableCollection<CommunicationsTypeInfoModel> _communicationsTypeItems = new()
        {
            new CommunicationsTypeInfoModel()
            {
                Name = "不使用分拣",
                Value = CommunicationsType.None,
            },
            new CommunicationsTypeInfoModel()
            {
                Name = "串口通讯",
                Value = CommunicationsType.SerialPort,
            },
            new CommunicationsTypeInfoModel()
            {
                Name = "TCP通讯",
                Value = CommunicationsType.TCP,
            },
            new CommunicationsTypeInfoModel()
            {
                Name = "USB通讯",
                Value = CommunicationsType.USB,
            },

            new CommunicationsTypeInfoModel()
            {
                Name = "CAN总线通讯",
                Value = CommunicationsType.CAN,
            },
        };

        private ObservableCollection<ParityInfoModel> _parityItems = new()
        {
            new ParityInfoModel()
            {
                Name = "None",
                Value = Parity.None
            },
            new ParityInfoModel()
            {
                Name = "Odd",
                Value = Parity.Odd
            },
            new ParityInfoModel()
            {
                Name = "Even",
                Value = Parity.Even
            },
            new ParityInfoModel()
            {
                Name = "Mark",
                Value = Parity.Mark
            },
            new ParityInfoModel()
            {
                Name = "Space",
                Value = Parity.Space
            },
        };

        private ObservableCollection<StopBitsInfoModel> _stopBitsItems = new()
        {
            new StopBitsInfoModel()
            {
                Name = "None",
                Value = 0,
            },
            new StopBitsInfoModel()
            {
                Name = "One",
                Value = StopBits.One,
            },
            new StopBitsInfoModel()
            {
                Name = "Two",
                Value = StopBits.Two,
            },
            new StopBitsInfoModel()
            {
                Name = "OnePointFive",
                Value = StopBits.OnePointFive,
            },
        };

        private ObservableCollection<int> _baudRateItems = new()
        {
            4800,9600,14400,19200,38400,115200
        };

        private ObservableCollection<int> _dataBitsItems = new()
        {
            5,6,7,8,
        };

        private ObservableCollection<DataFormatTypeInfoModel> _dataFormatTypeItems = new()
        {
            new DataFormatTypeInfoModel()
            {
                Name = "Ascii",
                Value = DataFormatType.Ascii
            },
            new DataFormatTypeInfoModel()
            {
                Name = "Hex",
                Value = DataFormatType.Hex
            },
        };

        private bool _isSavingInProgress;
        private ParityInfoModel _selectParity = new();
        private StopBitsInfoModel _selectStopBits = new();
        private CommunicationConnectionItemInfoModel _communicationConnectionItem = new();
        private bool _isOk;
        private string _exceptionContent = string.Empty;

        public CommunicationConnectionItemInfoModel CommunicationConnectionItem
        {
            get => _communicationConnectionItem;
            set => SetProperty(ref _communicationConnectionItem, value);
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public ObservableCollection<CommunicationProtocolInfoModel> CommunicationProtocolItems
        {
            get => _communicationProtocolItems;
            set => SetProperty(ref _communicationProtocolItems, value);
        }

        public ObservableCollection<CommunicationsTypeInfoModel> CommunicationsTypeItems
        {
            get => _communicationsTypeItems;
            set => SetProperty(ref _communicationsTypeItems, value);
        }

        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems
        {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems
        {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 效验位下拉选项
        /// </summary>
        public ObservableCollection<ParityInfoModel> ParityItems
        {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems
        {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public ObservableCollection<int> BaudRateItems
        {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public ObservableCollection<int> DataBitsItems
        {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress
        {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent
        {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand => new DelegateCommand(PortUpdateDelegate);

        private async void PortUpdateDelegate()
        {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate()
        {
            //保存返回
            try
            {
                if (CommunicationConnectionItem.CommunicationType.Value == CommunicationsType.None)
                {
                    throw new Exception("CommunicationType is None");
                }
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(CommunicationConnectionItem.ConnectionName, nameof(CommunicationConnectionItem.ConnectionName));
                IsOk = true;
            }
            catch (Exception e)
            {
                IsOk = false;
                ExceptionContent = e.Message;
            }

            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate()
        {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 加载方法
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
                //加载内容
            });
        }
    }
}