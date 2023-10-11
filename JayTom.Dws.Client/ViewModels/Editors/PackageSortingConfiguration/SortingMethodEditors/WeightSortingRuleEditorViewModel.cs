using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors {

    public class WeightSortingRuleEditorViewModel : BindableBase {
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }
    }
}