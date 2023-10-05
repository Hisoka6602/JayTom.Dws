using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Plugin.Speech;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //物流代码识别页面
    public class LogisticsCodeRecognitionViewModel : BindableBase {
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly ILogisticsRegexRepository _logisticsRegexRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ISpeech _speech;
        private ObservableCollection<LogisticsCodeRecognitionItemInfoModel> _logisticsCodeRecognitionItems = new();
        private SnackbarMessageQueue _logisticsCodeRecognitionMessageQueue = new(TimeSpan.FromSeconds(2));

        public LogisticsCodeRecognitionViewModel(ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            ILogisticsRegexRepository logisticsRegexRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ISpeech speech) {
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _logisticsRegexRepository = logisticsRegexRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _speech = speech;
        }

        public ObservableCollection<LogisticsCodeRecognitionItemInfoModel> LogisticsCodeRecognitionItems {
            get => _logisticsCodeRecognitionItems;
            set => SetProperty(ref _logisticsCodeRecognitionItems, value);
        }

        public SnackbarMessageQueue LogisticsCodeRecognitionMessageQueue {
            get => _logisticsCodeRecognitionMessageQueue;
            set => SetProperty(ref _logisticsCodeRecognitionMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var recognitionEditor = new LogisticsCodeRecognitionEditor();
                if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model) {
                    model.Identifier = "LogisticsCodeRecognitionDialog";
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        LogisticsCodeRecognitionMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        //添加到数据库
                        var infoModel = new LogisticsCodeRecognitionInfoModel() {
                            CreateTime = DateTime.Now,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                            IconBytes = model.LogisticsCodeRecognitionItemInfo.Icon?.ImageSourceToByteArray(),
                            LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                            LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                            ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                            Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                            SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                            SoundBytes = model.LogisticsCodeRecognitionItemInfo.SoundBytes,
                        };
                        var insertOrUpdate = await _logisticsCodeRecognitionRepository.InsertOrUpdate(infoModel);
                        if (insertOrUpdate) {
                            var logisticsCodeRecognitionInfoModel = await _logisticsCodeRecognitionRepository.FirstOrDefault(f =>
                                f.LogisticsCode.Equals(infoModel.LogisticsCode));
                            var logisticsRegexInfoModels = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                LogisticsId = logisticsCodeRecognitionInfoModel.Id,
                                RegexPattern = s.RegexPattern,
                            })?.ToList();
                            var regexInfoModels = await _logisticsRegexRepository.Select(s =>
                                s.LogisticsId.Equals(logisticsCodeRecognitionInfoModel.Id), o => o.Id);

                            if (regexInfoModels?.Any() == true) {
                                //删除
                                await _logisticsRegexRepository.DeleteRange(regexInfoModels);
                            }
                            //添加
                            var syncEntities = await _logisticsRegexRepository.InsertRange(logisticsRegexInfoModels ?? new List<LogisticsRegexInfoModel>());
                            if (syncEntities) {
                                LogisticsCodeRecognitionMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                LogisticsCodeRecognitionMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            LogisticsCodeRecognitionMessageQueue.Enqueue("保存失败");
                        }
                    }

                }
            });
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadeDelegate);
        }

        private void LoadeDelegate(object obj) {
            RefreshData();
        }

        /// <summary>
        /// 修改
        /// </summary>
        public ICommand ModifyCommand {
            get => new DelegateCommand<LogisticsCodeRecognitionItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(LogisticsCodeRecognitionItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var recognitionEditor = new LogisticsCodeRecognitionEditor();
                if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model) {
                    model.Identifier = "LogisticsCodeRecognitionDialog";
                    model.LogisticsCodeRecognitionItemInfo = obj;
                    model.SoundFilePath = obj.SoundName ?? string.Empty;
                    model.LogisticsRegexItems = obj.LogisticsRegexItems;
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        LogisticsCodeRecognitionMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        //添加到数据库
                        var infoModel = new LogisticsCodeRecognitionInfoModel() {
                            CreateTime = DateTime.Now,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            IconName = model.LogisticsCodeRecognitionItemInfo.IconName,
                            IconBytes = model.LogisticsCodeRecognitionItemInfo.Icon?.ImageSourceToByteArray(),
                            LogisticsCode = model.LogisticsCodeRecognitionItemInfo.LogisticsCode,
                            LogisticsName = model.LogisticsCodeRecognitionItemInfo.LogisticsName,
                            ModifyTime = model.LogisticsCodeRecognitionItemInfo.ModifyTime,
                            Remarks = model.LogisticsCodeRecognitionItemInfo.Remarks,
                            SoundName = model.LogisticsCodeRecognitionItemInfo.SoundName,
                            SoundBytes = model.LogisticsCodeRecognitionItemInfo.SoundBytes,
                        };
                        var insertOrUpdate = await _logisticsCodeRecognitionRepository.InsertOrUpdate(infoModel);
                        if (insertOrUpdate) {
                            var logisticsCodeRecognitionInfoModel = await _logisticsCodeRecognitionRepository.FirstOrDefault(f =>
                                f.LogisticsCode.Equals(infoModel.LogisticsCode));
                            var logisticsRegexInfoModels = model.LogisticsRegexItems.Select(s => new LogisticsRegexInfoModel {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                LogisticsId = logisticsCodeRecognitionInfoModel.Id,
                                RegexPattern = s.RegexPattern,
                            })?.ToList();

                            var regexInfoModels = await _logisticsRegexRepository.Select(s =>
                                s.LogisticsId.Equals(logisticsCodeRecognitionInfoModel.Id), o => o.Id);

                            if (regexInfoModels?.Any() == true) {
                                //删除
                                await _logisticsRegexRepository.DeleteRange(regexInfoModels);
                            }
                            //添加
                            var syncEntities = await _logisticsRegexRepository.InsertRange(logisticsRegexInfoModels ?? new List<LogisticsRegexInfoModel>());
                            if (syncEntities) {
                                LogisticsCodeRecognitionMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                LogisticsCodeRecognitionMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            LogisticsCodeRecognitionMessageQueue.Enqueue("保存失败");
                        }

                    }

                    //同步到正则表
                }
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand {
            get => new DelegateCommand<LogisticsCodeRecognitionItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(LogisticsCodeRecognitionItemInfoModel obj) {
            var logisticsCodeRecognitionInfoModel = await _logisticsCodeRecognitionRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (logisticsCodeRecognitionInfoModel is not null) {
                var delete = await _logisticsCodeRecognitionRepository.Delete(logisticsCodeRecognitionInfoModel);
                if (delete) {
                    //刷新列表
                    RefreshData();
                }
            }
        }

        /// <summary>
        /// 播放声音
        /// </summary>
        public ICommand PlaySoundCommand {
            get => new DelegateCommand<LogisticsCodeRecognitionItemInfoModel>(PlaySoundDelegate);
        }

        private void PlaySoundDelegate(LogisticsCodeRecognitionItemInfoModel obj) {

            if (obj.SoundBytes?.Length > 0) {
                Task.Factory.StartNew(() => {
                    _speech.PlayByteFile(obj.SoundBytes);
                });


            }

        }


        private async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = "LogisticsCodeRecognitionDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _logisticsCodeRecognitionRepository.
                LogisticsCodes(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                LogisticsCodeRecognitionItems.Clear();
                var infoModels = models?.Select((s, i) => new LogisticsCodeRecognitionItemInfoModel {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    ExitId = s.ExitId,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
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
                    RegexPattern = string.Join("\n", s.LogisticsRegexItems?.Select(s => s.RegexPattern))
                })?.ToList();
                LogisticsCodeRecognitionItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}