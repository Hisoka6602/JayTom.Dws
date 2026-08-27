using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Windows.Controls;
using JayTom.Dws.Models.Package;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Models.LocalConf;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration
{

    public class SortingMethodViewModel : BindableBase
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly ISettingsStore _settingsStore;

        private ObservableCollection<SortModeInfoModel> _sortModeItems = new()
        {
            new SortModeInfoModel()
            {
                Name = "不分拣",
                Value = SortMode.None
            },
            new SortModeInfoModel()
            {
                Name = "根据条码分拣",
                Value = SortMode.BarcodeSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据重量分拣",
                Value = SortMode.WeightSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据体积分拣",
                Value = SortMode.VolumeSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据物流分拣",
                Value = SortMode.LogisticsSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据Ocr分拣",
                Value = SortMode.OcrSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据Api响应分拣",
                Value = SortMode.ApiResponseSorting
            },
            new SortModeInfoModel()
            {
                Name = "根据组合工作流分拣",
                Value = SortMode.CombinedWorkflowSorting
            },
        };

        private SortModeInfoModel _selectSortMode = new();
        private SnackbarMessageQueue _sortingMethodMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public SortingMethodViewModel(ISettingsStore settingsStore,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _settingsStore = settingsStore;
        }

        public ObservableCollection<SortModeInfoModel> SortModeItems
        {
            get => _sortModeItems;
            set => SetProperty(ref _sortModeItems, value);
        }

        public SortModeInfoModel SelectSortMode
        {
            get => _selectSortMode;
            set => SetProperty(ref _selectSortMode, value);
        }

        public SnackbarMessageQueue SortingMethodMessageQueue
        {
            get => _sortingMethodMessageQueue;
            set => SetProperty(ref _sortingMethodMessageQueue, value);
        }

        public ICommand OptionSelectionChangedCommand => new DelegateCommand<SelectionChangedEventArgs>(OptionSelectionChangedDelegate);

        private async void OptionSelectionChangedDelegate(SelectionChangedEventArgs obj)
        {
            var insertOrUpdate = await _settingsStore.SaveAsync("SortingMethodSettings",new SortingMethodDto()
                {
                    SortMode = SelectSortMode?.Value ?? SortMode.None
                });
            if (!insertOrUpdate)
            {
                SelectSortMode = SortModeItems.FirstOrDefault(f => f.Value == SortMode.None) ?? new SortModeInfoModel();
                SortingMethodMessageQueue.Enqueue($"切换分拣模式失败");
            }
            else
            {
                _eventBus.Publish(new SettingsChangedEvent
                {
                    SettingsName = "SortingMethodSettings"
                });
            }
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var settingsDto = await _settingsStore
                        .GetAsync<SortingMethodDto>("SortingMethodSettings");
                    if (settingsDto is not null)
                    {
                        SelectSortMode = SortModeItems.FirstOrDefault(f => f.Value == settingsDto.SortMode) ??
                                         new SortModeInfoModel();
                    }
                });
            }
        }
    }
}
