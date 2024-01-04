using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class JtExpressApiPageViewModel : BindableBase {
        private JtExpressApiModel _jtExpressApiInfo = new();

        private ObservableCollection<StringItemModel> _scanTypeCodeItems = new()
        {
            new StringItemModel()
            {
                Name = "90: 中心到件(转运中心到件)",
                Value = "90"
            },
            new StringItemModel()
            {
                Name = "91: 集货到件-集散出港",
                Value = "91"
            },
            new StringItemModel()
            {
                Name = "92: 到件扫描-集散/网点进港",
                Value = "92"
            },
        };

        private StringItemModel _selectScanTypeCode = new();

        private ObservableCollection<StringItemModel> _transportTypeCodeItems = new()
        {
            new StringItemModel()
            {
                Name = "02: 公路运输",
                Value = "02"
            }
        };

        private StringItemModel _selectTransportTypeCode = new();

        private ObservableCollection<IntegerItemModel> _scanTypeItems = new()
        {
            new IntegerItemModel()
            {
                Name = "按运单",
                Value = 1,
            },
            new IntegerItemModel()
            {
                Name = "按包",
                Value = 2,
            },
            new IntegerItemModel()
            {
                Name = "空",
                Value = 3,
            },
        };

        private IntegerItemModel _selectScanType = new();

        private ObservableCollection<StringItemModel> _weightFlagItems = new()
        {
            new StringItemModel()
            {
                Name = "手输重",
                Value = "1"
            },
            new StringItemModel()
            {
                Name = "称重",
                Value = "2"
            },
        };

        private StringItemModel _selectWeightFlag = new();

        private ObservableCollection<IntegerItemModel> _typeItems = new()
        {
            new IntegerItemModel()
            {
                Name = "到件扫描",
                Value = 0
            },
            new IntegerItemModel()
            {
                Name = "出仓扫描",
                Value = 1
            },
        };

        private IntegerItemModel _selectType = new();

        public JtExpressApiModel JtExpressApiInfo {
            get => _jtExpressApiInfo;
            set => SetProperty(ref _jtExpressApiInfo, value);
        }

        public ObservableCollection<StringItemModel> ScanTypeCodeItems {
            get => _scanTypeCodeItems;
            set => SetProperty(ref _scanTypeCodeItems, value);
        }

        public StringItemModel SelectScanTypeCode {
            get => _selectScanTypeCode;
            set => SetProperty(ref _selectScanTypeCode, value);
        }

        public ObservableCollection<StringItemModel> TransportTypeCodeItems {
            get => _transportTypeCodeItems;
            set => SetProperty(ref _transportTypeCodeItems, value);
        }

        public StringItemModel SelectTransportTypeCode {
            get => _selectTransportTypeCode;
            set => SetProperty(ref _selectTransportTypeCode, value);
        }

        public ObservableCollection<IntegerItemModel> ScanTypeItems {
            get => _scanTypeItems;
            set => SetProperty(ref _scanTypeItems, value);
        }

        public IntegerItemModel SelectScanType {
            get => _selectScanType;
            set => SetProperty(ref _selectScanType, value);
        }

        public ObservableCollection<StringItemModel> WeightFlagItems {
            get => _weightFlagItems;
            set => SetProperty(ref _weightFlagItems, value);
        }

        public StringItemModel SelectWeightFlag {
            get => _selectWeightFlag;
            set => SetProperty(ref _selectWeightFlag, value);
        }

        public ObservableCollection<IntegerItemModel> TypeItems {
            get => _typeItems;
            set => SetProperty(ref _typeItems, value);
        }

        public IntegerItemModel SelectType {
            get => _selectType;
            set => SetProperty(ref _selectType, value);
        }
    }
}