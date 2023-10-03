using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration {

    public class PackageExitDefinitionEditorViewModel : BindableBase {
        private string _identifier = string.Empty;

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }
    }
}