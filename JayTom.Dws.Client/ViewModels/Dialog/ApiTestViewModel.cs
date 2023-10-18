using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Interface;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Dialog {
    public class ApiTestViewModel : BindableBase, IDialogAware {
        private string _apiJsonContent = string.Empty;

        public string ApiJsonContent {
            get => _apiJsonContent;
            set => SetProperty(ref _apiJsonContent, value);
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
            ApiJsonContent = string.Empty;
        }

        public async void OnDialogOpened(IDialogParameters parameters) {
            var itemModel = parameters.GetValue<UploadResponse>("UploadResponse");
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (itemModel is not null) {
                    try {
                        var settings = new JsonSerializerSettings {
                            Formatting = Formatting.Indented,
                            DateFormatString = "yyyy-MM-dd HH:mm:ss"
                        };
                        ApiJsonContent = JsonConvert.SerializeObject(itemModel, Formatting.Indented, settings);
                    }
                    catch (Exception e) {
                        ApiJsonContent = e.Message;
                    }
                }
            });
        }

        public string Title { get; } = string.Empty;

        public event Action<IDialogResult>? RequestClose;
    }
}