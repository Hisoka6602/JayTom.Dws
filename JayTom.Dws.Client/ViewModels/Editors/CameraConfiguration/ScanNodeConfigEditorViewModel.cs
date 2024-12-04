using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Windows.Forms;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.ViewModels.Editors.Enums;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class ScanNodeConfigEditorViewModel : BindableBase {
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private bool _isOk;
        private ScanNodeItemInfoModel _scanNodeItemInfo = new();

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public ScanNodeItemInfoModel ScanNodeItemInfo {
            get => _scanNodeItemInfo;
            set => SetProperty(ref _scanNodeItemInfo, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate() {
            IsOk = true;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate() {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand BrowseDirectoryCommand => new DelegateCommand(BrowseDirectoryDelegate);

        private void BrowseDirectoryDelegate() {
            var folderBrowserDialog = new FolderBrowserDialog() {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                ScanNodeItemInfo.ImagePath = folderBrowserDialog.SelectedPath;
            }
        }
    }
}