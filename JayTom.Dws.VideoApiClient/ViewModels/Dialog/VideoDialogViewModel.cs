using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;

namespace JayTom.Dws.VideoApiClient.ViewModels.Dialog {

    public class VideoDialogViewModel : BindableBase, IDialogAware {

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
        }

        public string Title => "视频播放";

        public event Action<IDialogResult>? RequestClose;
    }
}