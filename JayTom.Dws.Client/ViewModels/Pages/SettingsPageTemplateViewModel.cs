using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public abstract class SettingsPageTemplateViewModel : BindableBase {
        protected readonly IConfigRepository _configRepository;
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _messageQueue = new(TimeSpan.FromSeconds(2));

        protected SettingsPageTemplateViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public virtual void LoadedDelegate(object obj) {
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public SnackbarMessageQueue MessageQueue {
            get => _messageQueue;
            set => SetProperty(ref _messageQueue, value);
        }

        public abstract string Identifier { get; }
        public abstract string SettingsName { get; }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveSettingsCommand => new DelegateCommand(SaveDelegate);

        public virtual async void SaveDelegate() {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var settingsProcess = await SaveSettingsProcess();
                    if (settingsProcess) {
                        //通知事件
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = SettingsName
                        });
                    }
                    IsSavingInProgress = false;
                });
            }
        }

        protected abstract Task<bool> SaveSettingsProcess();
    }
}