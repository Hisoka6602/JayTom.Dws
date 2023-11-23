using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration {
    public class CommunicationConnectionConfigEditorViewModel : BindableBase {
        private string _identifier = string.Empty;
        private ObservableCollection<string> _portItems = new();
        private CommunicationsTypeInfoModel _selectCommunicationsType = new();
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

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }
        public ObservableCollection<CommunicationProtocolInfoModel> CommunicationProtocolItems {
            get => _communicationProtocolItems;
            set => SetProperty(ref _communicationProtocolItems, value);
        }
        public ObservableCollection<CommunicationsTypeInfoModel> CommunicationsTypeItems {
            get => _communicationsTypeItems;
            set => SetProperty(ref _communicationsTypeItems, value);
        }
        public CommunicationsTypeInfoModel SelectCommunicationsType {
            get => _selectCommunicationsType;
            set => SetProperty(ref _selectCommunicationsType, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 效验位下拉选项
        /// </summary>
        public ObservableCollection<ParityInfoModel> ParityItems {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        /// <summary>
        /// 效验位
        /// </summary>
        public ParityInfoModel SelectParity {
            get => _selectParity;
            set => SetProperty(ref _selectParity, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBitsInfoModel SelectStopBits {
            get => _selectStopBits;
            set => SetProperty(ref _selectStopBits, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public ObservableCollection<int> BaudRateItems {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public ObservableCollection<int> DataBitsItems {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand {
            get => new DelegateCommand(PortUpdateDelegate);
        }

        private async void PortUpdateDelegate() {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }
    }
}