using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace JayTom.Dws.Client.ViewModels.Dialog {
    public class ResolutionConstraintViewModel : BindableBase {
        private string _identifier = string.Empty;
        private int _currentWidth;
        private int _currentHeight;
        private int _minimumWidth;
        private int _minimumHeight;
        private bool _continueRunning;

        /// <summary>
        /// 标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 当前宽度
        /// </summary>
        public int CurrentWidth {
            get => _currentWidth;
            set => SetProperty(ref _currentWidth, value);
        }

        /// <summary>
        /// 当前高度
        /// </summary>
        public int CurrentHeight {
            get => _currentHeight;
            set => SetProperty(ref _currentHeight, value);
        }

        /// <summary>
        /// 最小宽度
        /// </summary>
        public int MinimumWidth {
            get => _minimumWidth;
            set => SetProperty(ref _minimumWidth, value);
        }

        /// <summary>
        /// 最小高度
        /// </summary>
        public int MinimumHeight {
            get => _minimumHeight;
            set => SetProperty(ref _minimumHeight, value);
        }

        /// <summary>
        /// 是否继续运行
        /// </summary>
        public bool ContinueRunning {
            get => _continueRunning;
            set => SetProperty(ref _continueRunning, value);
        }
        public ICommand ContinueCommand {
            get => new DelegateCommand<object>(ContinueDelegate);
        }

        private void ContinueDelegate(object obj) {
            ContinueRunning = true;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand ExitCommand {
            get => new DelegateCommand<object>(ExitDelegate);
        }

        private void ExitDelegate(object obj) {
            ContinueRunning = false;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}