using System;
using System.IO;
using System.Threading;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Speech;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Application.SortingConfigurations;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.Storage;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration
{

    //物流代码识别页面
    public class LogisticsCodeRecognitionViewModel : BulkOperationsTemplateViewModel<LogisticsCodeRecognitionItemInfoModel>
    {
        private readonly ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> _sortingCatalog;
        private readonly ISpeech _speech;
        private readonly IBinaryAssetStore _assetStore;
        private ObservableCollection<LogisticsCodeRecognitionItemInfoModel> _logisticsCodeRecognitionItems = new();

        public LogisticsCodeRecognitionViewModel(ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> sortingCatalog,
            ISpeech speech,
            IBinaryAssetStore assetStore,
            IExcel excel,
            IEventBus eventBus) : base(eventBus, excel)
        {
            _sortingCatalog = sortingCatalog;

            _speech = speech;
            _assetStore = assetStore;
        }

        public ObservableCollection<LogisticsCodeRecognitionItemInfoModel> LogisticsCodeRecognitionItems
        {
            get => _logisticsCodeRecognitionItems;
            set => SetProperty(ref _logisticsCodeRecognitionItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var recognitionEditor = new LogisticsCodeRecognitionEditor();
                if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk)
                    {
                        if (!await PersistPendingAssetsAsync(
                                model.LogisticsCodeRecognitionItemInfo)) {
                            MessageQueue.Enqueue("资源保存失败");
                            return;
                        }

                        //添加到数据库
                        var infoModel = new LogisticsCodeRecognitionInfoModel()
                        {
                            CreateTime = DateTime.Now,
                            IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                            IconFileReference = model.LogisticsCodeRecognitionItemInfo.IconFileReference,
                            LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                            LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                            ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                            Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                            SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                            SoundFileReference = model.LogisticsCodeRecognitionItemInfo.SoundFileReference,
                            LogisticsRegexItems = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel
                            {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                RegexPattern = s.RegexPattern,
                            })?.ToList()
                        };
                        var insertOrUpdate = await _sortingCatalog.AddAsync(infoModel);
                        if (insertOrUpdate)
                        {
                            _eventBus.Publish(infoModel);
                            _eventBus.Publish(new SettingsChangedEvent
                            {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });

                            MessageQueue.Enqueue("保存成功");
                            RefreshData();
                        }
                        else
                        {
                            MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public override string Identifier => "PackageSortingSettingsDialog";
        public override string ExcelTitle => "物流识别列表";
        public override string SheetName => "物流识别列表";

        public override string SettingsName => "LogisticsCodeRecognitionItemSettings";

        public override void LoadedDelegate(object obj)
        {
            RefreshData();
        }

        protected override async Task<bool> DeleteProcess(object obj)
        {
            if (obj is LogisticsCodeRecognitionItemInfoModel item)
            {
                return await _sortingCatalog.DeleteByIdAsync(item.Id);
            }

            return false;
        }

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = LogisticsCodeRecognitionItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            await _sortingCatalog.DeleteByIdsAsync(selectIds);
        }

        protected override List<LogisticsCodeRecognitionItemInfoModel> ExportProcess() =>
            [.. LogisticsCodeRecognitionItems.Select(s => new LogisticsCodeRecognitionItemInfoModel {
                CreateTime = s.CreateTime,
                Id = s.Id,
                Icon = s.Icon,
                IconName = s.IconName,
                LogisticsCode = s.LogisticsCode,
                LogisticsName = s.LogisticsName,
                RegexPattern = s.RegexPattern,
                Remarks = s.Remarks,
                Num = s.Num,
                ModifyTime = s.ModifyTime,
            })];

        protected override async Task<bool> ImportProcess(List<LogisticsCodeRecognitionItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var dateTime = DateTime.Now;
                var logisticsCodeRecognitionInfoModels = items
                    .Select(s => new LogisticsCodeRecognitionInfoModel()
                    {
                        CreateTime = dateTime,
                        Remarks = s.Remarks,
                        IconName = s.IconName,
                        LogisticsCode = s.LogisticsCode,
                        LogisticsName = s.LogisticsName,
                        SoundName = s.SoundName,
                        LogisticsRegexItems = new List<LogisticsRegexInfoModel>()
                        {
                            new()
                            {
                                RegexPattern = s.RegexPattern
                            }
                        }
                    })
                    .GroupBy(s => s.LogisticsName)
                    .Select(group => new LogisticsCodeRecognitionInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        IconName = group.First().IconName,
                        LogisticsCode = group.First().LogisticsCode,
                        LogisticsName = group.First().LogisticsName,
                        SoundName = group.First().SoundName,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        LogisticsRegexItems = [.. group.SelectMany(item => item.LogisticsRegexItems ?? new List<LogisticsRegexInfoModel>())]
                    })
                    .ToList();

                //批量添加
                return await _sortingCatalog.AddRangeAsync(logisticsCodeRecognitionInfoModels);
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is LogisticsCodeRecognitionItemInfoModel item)
            {
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var recognitionEditor = new LogisticsCodeRecognitionEditor();
                    if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.LogisticsCodeRecognitionItemInfo = item;
                        model.SoundFilePath = item.SoundName ?? string.Empty;
                        model.LogisticsRegexItems = item.LogisticsRegexItems;
                        await DialogHost.Show(recognitionEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            return;
                        }

                        if (model.IsOk)
                        {
                            if (!await PersistPendingAssetsAsync(
                                    model.LogisticsCodeRecognitionItemInfo)) {
                                MessageQueue.Enqueue("资源保存失败");
                                return;
                            }

                            //添加到数据库
                            var infoModel = new LogisticsCodeRecognitionInfoModel()
                            {
                                Id = model.LogisticsCodeRecognitionItemInfo.Id,
                                CreateTime = DateTime.Now,
                                IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                                IconFileReference = model.LogisticsCodeRecognitionItemInfo.IconFileReference,
                                LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                                LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                                ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                                Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                                SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                                SoundFileReference = model.LogisticsCodeRecognitionItemInfo.SoundFileReference,
                                LogisticsRegexItems = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel
                                {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    RegexPattern = s.RegexPattern,
                                })?.ToList()
                            };
                            var insertOrUpdate = await _sortingCatalog.UpdateAsync(infoModel);
                            if (insertOrUpdate)
                            {
                                MessageQueue.Enqueue("保存成功");
                                RefreshData();
                                _eventBus.Publish(new SettingsChangedEvent
                                {
                                    SettingsName = SettingsName,
                                    IsLocallySaved = true
                                });
                            }
                            else
                            {
                                MessageQueue.Enqueue("保存失败");
                            }
                        }

                        //同步到正则表
                    }
                });
            }
        }

        /// <summary>
        /// 播放声音
        /// </summary>
        public ICommand PlaySoundCommand => new DelegateCommand<LogisticsCodeRecognitionItemInfoModel>(PlaySoundDelegate);

        private async void PlaySoundDelegate(LogisticsCodeRecognitionItemInfoModel obj)
        {
            var bytes = obj.SoundBytes;
            if ((bytes is null || bytes.Length == 0) &&
                !string.IsNullOrWhiteSpace(obj.SoundFileReference)) {
                var asset = await _assetStore.ReadAsync(
                    new BinaryAssetReference(obj.SoundFileReference),
                    16 * 1024 * 1024,
                    CancellationToken.None);
                bytes = asset.Value;
            }

            if (bytes?.Length > 0)
            {
                await _speech.PlayByteFile(bytes);
            }
        }

        protected override async Task ClearProcess()
        {
            await _sortingCatalog.DeleteAllAsync();
        }

        protected override async Task RefreshDataProcess()
        {
            await Task.Delay(300);
            var models = await _sortingCatalog.ListAsync();
            LogisticsCodeRecognitionItems.Clear();
            var infoModels = models?.Select((s, i) => new LogisticsCodeRecognitionItemInfoModel
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                Icon = s.IconBytes?.ByteArrayToImageSource(),
                IconName = s.IconName,
                IconFileReference = s.IconFileReference,
                LogisticsCode = s.LogisticsCode,
                LogisticsName = s.LogisticsName,
                SoundName = s.SoundName,
                SoundFileReference = s.SoundFileReference,
                LogisticsRegexItems = new ObservableCollection<LogisticsRegexItemInfoModel>(s.LogisticsRegexItems?.Select((s1, i1) => new LogisticsRegexItemInfoModel
                {
                    CreateTime = s1.CreateTime,
                    Id = s1.Id,
                    LogisticsId = s1.LogisticsId,
                    ModifyTime = s1.ModifyTime,
                    Num = i1 + 1,
                    Remarks = s1.Remarks,
                    RegexPattern = s1.RegexPattern
                }).ToList() ?? new List<LogisticsRegexItemInfoModel>()),
                RegexPattern = string.Join("\n", s.LogisticsRegexItems?.Select(s2 => s2.RegexPattern) ?? Array.Empty<string>())
            })?.ToList();
            if (infoModels is not null) {
                foreach (var item in infoModels) {
                    if (!string.IsNullOrWhiteSpace(item.SoundFileReference)) {
                        var sound = await _assetStore.ReadAsync(
                            new BinaryAssetReference(item.SoundFileReference),
                            16 * 1024 * 1024,
                            CancellationToken.None);
                        item.SoundBytes = sound.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(item.IconFileReference)) {
                        var icon = await _assetStore.ReadAsync(
                            new BinaryAssetReference(item.IconFileReference),
                            8 * 1024 * 1024,
                            CancellationToken.None);
                        item.Icon = icon.Value?.ByteArrayToImageSource();
                    }
                }
            }
            LogisticsCodeRecognitionItems.AddRange(infoModels);
        }

        private async Task<bool> PersistPendingAssetsAsync(
            LogisticsCodeRecognitionItemInfoModel item) {
            if (item.SoundBytes is { Length: > 0 }) {
                await using var stream = new MemoryStream(item.SoundBytes, writable: false);
                var saved = await _assetStore.SaveAsync(
                    "sorting-sounds",
                    item.SoundName ?? "sound.bin",
                    stream,
                    CancellationToken.None);
                if (!saved.IsSuccess) {
                    return false;
                }

                item.SoundFileReference = saved.Value.Value;
            }

            var iconBytes = item.Icon?.ImageSourceToByteArray();
            if (iconBytes is { Length: > 0 }) {
                await using var stream = new MemoryStream(iconBytes, writable: false);
                var saved = await _assetStore.SaveAsync(
                    "sorting-icons",
                    string.IsNullOrWhiteSpace(item.IconName) ? "icon.png" : item.IconName,
                    stream,
                    CancellationToken.None);
                if (!saved.IsSuccess) {
                    return false;
                }

                item.IconFileReference = saved.Value.Value;
            }

            return true;
        }

        protected override bool IsSelectAnyItem() => LogisticsCodeRecognitionItems.Any(a => a.IsSelect);
    }
}
