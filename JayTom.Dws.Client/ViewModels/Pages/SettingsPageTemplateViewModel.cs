using JayTom.Dws.Application.Configuration;
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
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Repository.LocalConf;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages
{

    public abstract class SettingsPageTemplateViewModel : BindableBase
    {
        protected readonly ISettingsStore _settingsStore;

        private bool _isSavingInProgress;
        private SnackbarMessageQueue _messageQueue = new(TimeSpan.FromSeconds(2));

        protected SettingsPageTemplateViewModel(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public virtual void LoadedDelegate(object obj)
        {
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress
        {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public SnackbarMessageQueue MessageQueue
        {
            get => _messageQueue;
            set => SetProperty(ref _messageQueue, value);
        }

        public abstract string Identifier { get; }
        public abstract string SettingsName { get; }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveSettingsCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate() => _ = SaveAsync();

        /// <summary>异步保存当前设置。</summary>
        public virtual async Task SaveAsync()
        {
            if (IsSavingInProgress)
            {
                return;
            }

            IsSavingInProgress = true;
            try
            {
                var settingsProcess = await SaveSettingsProcess();
                if (settingsProcess)
                {
                    //通知事件
                    EventAggregator.Instance.Publish(new SettingsChangedEvent
                    {
                        SettingsName = SettingsName,
                        IsLocallySaved = true
                    });
                }
            }
            catch (Exception exception)
            {
                MessageQueue.Enqueue($"保存配置失败:{exception.Message}");
            }
            finally
            {
                IsSavingInProgress = false;
            }
        }

        protected abstract Task<bool> SaveSettingsProcess();
    }
}
