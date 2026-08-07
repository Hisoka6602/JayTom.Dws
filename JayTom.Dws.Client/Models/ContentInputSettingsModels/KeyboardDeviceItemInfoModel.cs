using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ContentInputSettingsModels
{

    public class KeyboardDeviceItemInfoModel : BindableBase
    {
        private int _vendorId;
        private int _productId;
        private string? _deviceName;
        private string? _devicePath;
        private string? _manufacturerName;
        private bool _isConnected;
        private int _num;
        private bool _hasBinding;

        public int Num
        {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        public int VendorId
        {
            get => _vendorId;
            set => SetProperty(ref _vendorId, value);
        }

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string? DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        public string? DevicePath
        {
            get => _devicePath;
            set => SetProperty(ref _devicePath, value);
        }

        public string? ManufacturerName
        {
            get => _manufacturerName;
            set => SetProperty(ref _manufacturerName, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool HasBinding
        {
            get => _hasBinding;
            set => SetProperty(ref _hasBinding, value);
        }
    }
}