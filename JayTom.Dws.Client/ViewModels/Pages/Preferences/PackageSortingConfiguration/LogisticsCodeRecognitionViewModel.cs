using System;
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
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //物流代码识别页面
    public class LogisticsCodeRecognitionViewModel : BulkOperationsTemplateViewModel<LogisticsCodeRecognitionItemInfoModel> {
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly ISpeech _speech;
        private ObservableCollection<LogisticsCodeRecognitionItemInfoModel> _logisticsCodeRecognitionItems = new();

        public LogisticsCodeRecognitionViewModel(ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            ISpeech speech, IExcel excel) : base(excel) {
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;

            _speech = speech;
        }

        public ObservableCollection<LogisticsCodeRecognitionItemInfoModel> LogisticsCodeRecognitionItems {
            get => _logisticsCodeRecognitionItems;
            set => SetProperty(ref _logisticsCodeRecognitionItems, value);
        }

        protected override async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var recognitionEditor = new LogisticsCodeRecognitionEditor();
                if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model) {
                    model.Identifier = Identifier;
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        //添加到数据库
                        var infoModel = new LogisticsCodeRecognitionInfoModel() {
                            CreateTime = DateTime.Now,
                            IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                            IconBytes = model.LogisticsCodeRecognitionItemInfo.Icon?.ImageSourceToByteArray(),
                            LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                            LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                            ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                            Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                            SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                            SoundBytes = model.LogisticsCodeRecognitionItemInfo.SoundBytes,
                            LogisticsRegexItems = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                RegexPattern = s.RegexPattern,
                            })?.ToList()
                        };
                        var insertOrUpdate = await _logisticsCodeRecognitionRepository.InsertDetailAsync(infoModel);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(infoModel);
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });

                            MessageQueue.Enqueue("保存成功");
                            RefreshData();
                        }
                        else {
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

        public override void LoadedDelegate(object obj) {
            RefreshData();
        }

        protected override async Task<bool> DeleteProcess(object obj) {
            if (obj is LogisticsCodeRecognitionItemInfoModel item) {
                var logisticsCodeRecognitionInfoModel = await _logisticsCodeRecognitionRepository.
                    FirstOrDefault(f => f.Id.Equals(item.Id));
                if (logisticsCodeRecognitionInfoModel is not null) {
                    return await _logisticsCodeRecognitionRepository.Delete(logisticsCodeRecognitionInfoModel);
                }
            }

            return false;
        }

        protected override async Task BulkDeleteProcess() {
            var selectIds = LogisticsCodeRecognitionItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.Select(s => selectIds.Contains(s.Id),
                o => o.Id);
            await _logisticsCodeRecognitionRepository.DeleteRange(logisticsCodeRecognitionInfoModels);
        }

        protected override List<LogisticsCodeRecognitionItemInfoModel> ExportProcess() =>
            LogisticsCodeRecognitionItems.Select(s => new LogisticsCodeRecognitionItemInfoModel {
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
            }).ToList();

        protected override async Task<bool> ImportProcess(List<LogisticsCodeRecognitionItemInfoModel> items) {
            if (items?.Any() == true) {
                var dateTime = DateTime.Now;
                var logisticsCodeRecognitionInfoModels = items
                    .Select(s => new LogisticsCodeRecognitionInfoModel() {
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
                    .Select(group => new LogisticsCodeRecognitionInfoModel {
                        CreateTime = group.First().CreateTime,
                        IconName = group.First().IconName,
                        LogisticsCode = group.First().LogisticsCode,
                        LogisticsName = group.First().LogisticsName,
                        SoundName = group.First().SoundName,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        LogisticsRegexItems = group.SelectMany(item => item.LogisticsRegexItems ?? new List<LogisticsRegexInfoModel>()).ToList()
                    })
                    .ToList();

                //批量添加
                return await _logisticsCodeRecognitionRepository.InsertRangeDetailAsync(logisticsCodeRecognitionInfoModels);
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj) {
            if (obj is LogisticsCodeRecognitionItemInfoModel item) {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var recognitionEditor = new LogisticsCodeRecognitionEditor();
                    if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model) {
                        model.Identifier = Identifier;
                        model.LogisticsCodeRecognitionItemInfo = item;
                        model.SoundFilePath = item.SoundName ?? string.Empty;
                        model.LogisticsRegexItems = item.LogisticsRegexItems;
                        await DialogHost.Show(recognitionEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            return;
                        }

                        if (model.IsOk) {
                            //添加到数据库
                            var infoModel = new LogisticsCodeRecognitionInfoModel() {
                                Id = model.LogisticsCodeRecognitionItemInfo.Id,
                                CreateTime = DateTime.Now,
                                IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                                IconBytes = model.LogisticsCodeRecognitionItemInfo.Icon?.ImageSourceToByteArray(),
                                LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                                LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                                ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                                Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                                SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                                SoundBytes = model.LogisticsCodeRecognitionItemInfo.SoundBytes,
                                LogisticsRegexItems = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    RegexPattern = s.RegexPattern,
                                })?.ToList()
                            };
                            var insertOrUpdate = await _logisticsCodeRecognitionRepository.UpdateDetailAsync(infoModel);
                            if (insertOrUpdate) {
                                MessageQueue.Enqueue("保存成功");
                                RefreshData();
                                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                    SettingsName = SettingsName,
                                    IsLocallySaved = true
                                });
                            }
                            else {
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

        private void PlaySoundDelegate(LogisticsCodeRecognitionItemInfoModel obj) {
            if (obj.SoundBytes?.Length > 0) {
                Task.Factory.StartNew(() => {
                    _speech.PlayByteFile(obj.SoundBytes);
                });
            }
        }

        protected override async Task ClearProcess() {
            var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                o => o.Id);
            await _logisticsCodeRecognitionRepository.DeleteRange(logisticsCodeRecognitionInfoModels);
        }

        protected override async Task RefreshDataProcess() {
            await Task.Delay(300);
            var models = await _logisticsCodeRecognitionRepository.
                LogisticsCodes(s => s.Id > 0);
            LogisticsCodeRecognitionItems.Clear();
            var infoModels = models?.Select((s, i) => new LogisticsCodeRecognitionItemInfoModel {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                Icon = s.IconBytes?.ByteArrayToImageSource(),
                IconName = s.IconName,
                LogisticsCode = s.LogisticsCode,
                LogisticsName = s.LogisticsName,
                SoundName = s.SoundName,
                SoundBytes = s.SoundBytes,
                LogisticsRegexItems = new ObservableCollection<LogisticsRegexItemInfoModel>(s.LogisticsRegexItems?.Select((s1, i1) => new LogisticsRegexItemInfoModel {
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
            LogisticsCodeRecognitionItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => LogisticsCodeRecognitionItems.Any(a => a.IsSelect);
    }
}