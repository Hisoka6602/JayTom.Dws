using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class CreatePackageSettingsInfoModel : BindableBase {
        private bool _isUsePackageExpiry;
        private int _packageExpiryTime;
        private BarcodeHandlingMethodEnum _barcodeHandlingMethod = BarcodeHandlingMethodEnum.UseOneBarcode;
        private PackageCreationMethodsEnum _packageCreationMethods = PackageCreationMethodsEnum.ScanBarcodeCamera;
        private bool _isUseNoRead = true;
        private BarcodeQueueOrderEnum _barcodeQueueOrder = BarcodeQueueOrderEnum.TimeAscending;
        private PackageRemoveMethodsEnum _packageRemoveMethods = PackageRemoveMethodsEnum.FillInformation;
        private bool _clearPackageQueueOnStop = true;

        private ObservableCollection<PackageCreationMethodItemInfoModel> _packageCreationMethodItems = new()
        {
            new PackageCreationMethodItemInfoModel { DisplayName = "扫码相机", EnumValue = PackageCreationMethodsEnum.ScanBarcodeCamera ,IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "稳定重量",EnumValue = PackageCreationMethodsEnum.StableWeight , IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "控件输入",EnumValue = PackageCreationMethodsEnum.ControlInput , IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "体积输入",EnumValue = PackageCreationMethodsEnum.VolumeInput , IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "下位机创建",EnumValue = PackageCreationMethodsEnum.LowerMachineCreation , IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "Tcp内容输入",EnumValue = PackageCreationMethodsEnum.TcpInput , IsChecked = false },
            new PackageCreationMethodItemInfoModel { DisplayName = "Ocr信息",EnumValue = PackageCreationMethodsEnum.OcrInfo , IsChecked = false },
        };

        private bool _isUseEmptyPackageExpiry;
        private int _emptyPackageExpiryTime;

        /// <summary>
        /// 是否使用包裹过期
        /// </summary>
        public bool IsUsePackageExpiry {
            get => _isUsePackageExpiry;
            set => SetProperty(ref _isUsePackageExpiry, value);
        }

        /// <summary>
        /// 包裹过期时间(设置为0则不验证)
        /// </summary>
        public int PackageExpiryTime {
            get => _packageExpiryTime;
            set => SetProperty(ref _packageExpiryTime, value);
        }

        /// <summary>
        /// 是否使用空包裹过期
        /// </summary>
        public bool IsUseEmptyPackageExpiry {
            get => _isUseEmptyPackageExpiry;
            set => SetProperty(ref _isUseEmptyPackageExpiry, value);
        }

        /// <summary>
        /// 空包裹过期时间(设置为0则不验证)
        /// </summary>
        public int EmptyPackageExpiryTime {
            get => _emptyPackageExpiryTime;
            set => SetProperty(ref _emptyPackageExpiryTime, value);
        }

        /// <summary>
        /// 多条码返回处理方式
        /// </summary>
        public BarcodeHandlingMethodEnum BarcodeHandlingMethod {
            get => _barcodeHandlingMethod;
            set => SetProperty(ref _barcodeHandlingMethod, value);
        }

        /// <summary>
        /// 创建包裹方式
        /// </summary>
        public PackageCreationMethodsEnum PackageCreationMethods {
            get => _packageCreationMethods;
            set => SetProperty(ref _packageCreationMethods, value);
        }

        /// <summary>
        /// 是否使用NoRead
        /// </summary>
        public bool IsUseNoRead {
            get => _isUseNoRead;
            set => SetProperty(ref _isUseNoRead, value);
        }

        /// <summary>
        /// 填充条码队列
        /// </summary>
        public BarcodeQueueOrderEnum BarcodeQueueOrder {
            get => _barcodeQueueOrder;
            set => SetProperty(ref _barcodeQueueOrder, value);
        }

        /// <summary>
        /// 移除包裹方式
        /// </summary>
        public PackageRemoveMethodsEnum PackageRemoveMethods {
            get => _packageRemoveMethods;
            set => SetProperty(ref _packageRemoveMethods, value);
        }

        /// <summary>
        /// 停止时是否清空包裹
        /// </summary>
        public bool ClearPackageQueueOnStop {
            get => _clearPackageQueueOnStop;
            set => SetProperty(ref _clearPackageQueueOnStop, value);
        }

        public ObservableCollection<PackageCreationMethodItemInfoModel> PackageCreationMethodItems {
            get => _packageCreationMethodItems;
            set => SetProperty(ref _packageCreationMethodItems, value);
        }
    }

    public class PackageCreationMethodItemInfoModel {
        public string DisplayName { get; set; } = string.Empty;
        public PackageCreationMethodsEnum EnumValue { get; set; }
        public bool IsChecked { get; set; }
    }
}