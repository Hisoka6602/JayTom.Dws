using Prism.Mvvm;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class LoadingDialogViewModel : BindableBase {
        private string _description = "Loading...";
        private string _identifier = string.Empty;

        /// <summary>
        /// 说明
        /// </summary>
        public string Description {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }
    }
}