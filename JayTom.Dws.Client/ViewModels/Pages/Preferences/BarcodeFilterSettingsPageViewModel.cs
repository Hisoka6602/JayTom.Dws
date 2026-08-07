using System;
using ImTools;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.BarcodeFilterSettingsModel;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class BarcodeFilterSettingsPageViewModel : SettingsPageTemplateViewModel
    {
        private readonly IExcel _excel;
        private string _testBarcode = string.Empty;
        private bool _isLoaded;
        private BarcodeFilterSettingsInfoModel _barcodeFilterSettingsInfo = new();
        private int _filterTypeSelectedIndex = 0;
        private string _customRegexFilterTestBarcode = string.Empty;
        private string _customRegexReplacementTestBarcode = string.Empty;
        private string _replacedBarcode = string.Empty;

        public BarcodeFilterSettingsPageViewModel(IConfigRepository configRepository,
            IExcel excel) : base(configRepository)
        {
            _excel = excel;
        }

        public BarcodeFilterSettingsInfoModel BarcodeFilterSettingsInfo
        {
            get => _barcodeFilterSettingsInfo;
            set => SetProperty(ref _barcodeFilterSettingsInfo, value);
        }

        /// <summary>
        /// 测试的条码
        /// </summary>
        public string TestBarcode
        {
            get => _testBarcode;
            set => SetProperty(ref _testBarcode, value);
        }

        /// <summary>
        /// 自定义正则测试条码
        /// </summary>
        public string CustomRegexFilterTestBarcode
        {
            get => _customRegexFilterTestBarcode;
            set => SetProperty(ref _customRegexFilterTestBarcode, value);
        }

        /// <summary>
        /// 正则替换条码
        /// </summary>
        public string CustomRegexReplacementTestBarcode
        {
            get => _customRegexReplacementTestBarcode;
            set => SetProperty(ref _customRegexReplacementTestBarcode, value);
        }

        /// <summary>
        /// 替换后的条码
        /// </summary>
        public string ReplacedBarcode
        {
            get => _replacedBarcode;
            set => SetProperty(ref _replacedBarcode, value);
        }

        /// <summary>
        /// 过滤模式
        /// </summary>
        public int FilterTypeSelectedIndex
        {
            get => _filterTypeSelectedIndex;
            set => SetProperty(ref _filterTypeSelectedIndex, value);
        }

        public ICommand MinimumLengthChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand MaximumLengthChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand StartCharacterTypeChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand EndCharacterTypeChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand DisallowedCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand RequiredCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);
        public ICommand AnyCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);
        public ICommand AnyStartCodesChangedCommand => new DelegateCommand(UpdateRegularExpression);
        public ICommand TestCommand => new DelegateCommand(TestDelegate);
        public ICommand AddToRegexFilterListCommand => new DelegateCommand(AddToRegexFilterListDelegate);
        public ICommand CustomRegexFilterAddCommand => new DelegateCommand(CustomRegexFilterAddDelegate);

        public ICommand CustomRegexFilterClearCommand => new DelegateCommand(CustomRegexFilterClearClearDelegate);

        public ICommand CustomRegexReplacementAddCommand => new DelegateCommand(CustomRegexReplacementAddDelegate);
        public ICommand CustomRegexReplacementClearCommand => new DelegateCommand(CustomRegexReplacementClearDelegate);
        public ICommand CustomRegexFilterExportCommand => new DelegateCommand(CustomRegexFilterExportDelegate);
        public ICommand CustomRegexReplacementExportCommand => new DelegateCommand(CustomRegexReplacementExportDelegate);
        public ICommand CustomRegexFilterImportCommand => new DelegateCommand(CustomRegexFilterImportDelegate);
        public ICommand CustomRegexReplacementImportCommand => new DelegateCommand(CustomRegexReplacementImportDelegate);
        public ICommand CustomRegexFilterCommand => new DelegateCommand(TestDelegate);
        public ICommand CustomRegexReplacementTestCommand => new DelegateCommand(CustomRegexReplacementTestDelegate);
        public ICommand CustomRegexReplacementDeleteCommand => new DelegateCommand<CustomRegexReplacementItemInfoModel>(CustomRegexReplacementDeleteDelegate);

        public ICommand CustomRegexFilterDeleteCommand => new DelegateCommand<CustomRegexFilterItemInfoModel>(CustomRegexFilterDeleteDelegate);

        /// <summary>
        /// 删除过滤项
        /// </summary>
        private async void CustomRegexFilterDeleteDelegate(CustomRegexFilterItemInfoModel item)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BarcodeFilterSettingsInfo.CustomRegexFilterItems.Remove(item);
                for (var i = 0; i < BarcodeFilterSettingsInfo.CustomRegexFilterItems.Count; i++)
                {
                    BarcodeFilterSettingsInfo.CustomRegexFilterItems[i].Num = i + 1;
                }
            });
        }

        /// <summary>
        /// 删除项替换项
        /// </summary>
        private async void CustomRegexReplacementDeleteDelegate(CustomRegexReplacementItemInfoModel item)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Remove(item);
                for (var i = 0; i < BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Count; i++)
                {
                    BarcodeFilterSettingsInfo.CustomRegexReplacementItems[i].Num = i + 1;
                }
            });
        }

        /// <summary>
        /// 正则替换测试
        /// </summary>
        private async void CustomRegexReplacementTestDelegate()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var replacedBarcode = CustomRegexReplacementTestBarcode;
                try
                {
                    replacedBarcode = BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Where(w => w.IsActive).Aggregate(replacedBarcode, (current, customRegexReplacementItemInfoModel) => Regex.Replace(current, customRegexReplacementItemInfoModel.RegexPattern, customRegexReplacementItemInfoModel.ReplaceContent));
                }
                catch (Exception e)
                {
                    base.MessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("不是正确的正则表达式") ?? string.Empty);
                }

                ReplacedBarcode = replacedBarcode;
            });
        }

        private async void CustomRegexReplacementImportDelegate()
        {
            var infoModels = await ImportDelegate<CustomRegexReplacementItemInfoModel>();
            if (infoModels.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var existingRegexPatterns = BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Select(item => item.RegexPattern);
                    var distinctInfoModels = infoModels
                        .Where(model => !existingRegexPatterns.Contains(model.RegexPattern))
                        .GroupBy(model => model.RegexPattern)
                        .Select(group => group.First())
                        .ToList();
                    BarcodeFilterSettingsInfo.CustomRegexReplacementItems.AddRange(distinctInfoModels);
                    for (var i = 0; i < BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Count; i++)
                    {
                        BarcodeFilterSettingsInfo.CustomRegexReplacementItems[i].Num = i + 1;
                    }
                    base.MessageQueue.Enqueue("导入成功");
                });
            }
            else
            {
                base.MessageQueue.Enqueue("未获取到任何数据");
            }
        }

        private async void CustomRegexFilterImportDelegate()
        {
            var infoModels = await ImportDelegate<CustomRegexFilterItemInfoModel>();
            if (infoModels.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var existingRegexPatterns = BarcodeFilterSettingsInfo.CustomRegexFilterItems.Select(item => item.RegexPattern);
                    var distinctInfoModels = infoModels
                        .Where(model => !existingRegexPatterns.Contains(model.RegexPattern))
                        .GroupBy(model => model.RegexPattern)
                        .Select(group => group.First())
                        .ToList();
                    BarcodeFilterSettingsInfo.CustomRegexFilterItems.AddRange(distinctInfoModels);
                    for (var i = 0; i < BarcodeFilterSettingsInfo.CustomRegexFilterItems.Count; i++)
                    {
                        BarcodeFilterSettingsInfo.CustomRegexFilterItems[i].Num = i + 1;
                    }
                    base.MessageQueue.Enqueue("导入成功");
                });
            }
            else
            {
                base.MessageQueue.Enqueue("未获取到任何数据");
            }
        }

        private void CustomRegexReplacementExportDelegate()
        {
            ExportDelegate(BarcodeFilterSettingsInfo.CustomRegexReplacementItems.ToList(), "自定义正则替换", "自定义正则替换");
        }

        /// <summary>
        /// 正则导出
        /// </summary>
        private void CustomRegexFilterExportDelegate()
        {
            ExportDelegate(BarcodeFilterSettingsInfo.CustomRegexFilterItems.ToList(), "自定义正则过滤", "自定义正则过滤");
        }

        /// <summary>
        /// 自定义正则过滤清空
        /// </summary>
        private async void CustomRegexFilterClearClearDelegate()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BarcodeFilterSettingsInfo.CustomRegexFilterItems?.Clear();
            });
        }

        /// <summary>
        /// 自定义正则替换清空
        /// </summary>
        private async void CustomRegexReplacementClearDelegate()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BarcodeFilterSettingsInfo.CustomRegexReplacementItems?.Clear();
            });
        }

        /// <summary>
        /// 自定义正则替换添加
        /// </summary>
        private async void CustomRegexReplacementAddDelegate()
        {
            var regularExpressionEditor = new RegularExpressionEditor();
            if (regularExpressionEditor.DataContext is RegularExpressionEditorViewModel model)
            {
                model.Identifier = Identifier;
                model.IsUseReplace = true;
                await DialogHost.Show(regularExpressionEditor, model.Identifier);

                if (!string.IsNullOrEmpty(model.ExceptionContent))
                {
                    base.MessageQueue.Enqueue(model.ExceptionContent);
                    return;
                }

                if (model.IsOk)
                {
                    //判断是否重复
                    if (BarcodeFilterSettingsInfo.CustomRegexFilterItems?.
                            Any(a =>
                                a.RegexPattern.
                                    Equals(model.RegexPattern)) == true)
                    {
                        base.MessageQueue.Enqueue("该表达式已存在!");
                        return;
                    }
                    BarcodeFilterSettingsInfo.CustomRegexReplacementItems?.Add(new CustomRegexReplacementItemInfoModel()
                    {
                        Num = BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Count + 1,
                        IsActive = true,
                        RegexPattern = model.RegexPattern,
                        ReplaceContent = model.ReplaceContent,
                        Remarks = model.Remarks
                    });
                }
            }
        }

        /// <summary>
        /// 自定义正则添加
        /// </summary>
        private async void CustomRegexFilterAddDelegate()
        {
            var regularExpressionEditor = new RegularExpressionEditor();
            if (regularExpressionEditor.DataContext is RegularExpressionEditorViewModel model)
            {
                model.Identifier = Identifier;
                await DialogHost.Show(regularExpressionEditor, model.Identifier);

                if (!string.IsNullOrEmpty(model.ExceptionContent))
                {
                    base.MessageQueue.Enqueue(model.ExceptionContent);
                    return;
                }

                if (model.IsOk)
                {
                    //判断是否重复
                    if (BarcodeFilterSettingsInfo.CustomRegexFilterItems?.
                            Any(a =>
                                a.RegexPattern.
                                    Equals(model.RegexPattern)) == true)
                    {
                        base.MessageQueue.Enqueue("该表达式已存在!");
                        return;
                    }
                    BarcodeFilterSettingsInfo.CustomRegexFilterItems?.Add(new CustomRegexFilterItemInfoModel()
                    {
                        Num = BarcodeFilterSettingsInfo.CustomRegexFilterItems.Count + 1,
                        IsActive = true,
                        RegexPattern = model.RegexPattern,
                        Remarks = model.Remarks
                    });
                }
            }
        }

        /// <summary>
        /// 添加到自定义正则列表
        /// </summary>
        private void AddToRegexFilterListDelegate()
        {
            //判断是否重复
            if (string.IsNullOrEmpty(BarcodeFilterSettingsInfo.
                    BasicFilterInfo.RegularExpression))
            {
                base.MessageQueue.Enqueue("内容不能为空!");
                return;
            }
            if (BarcodeFilterSettingsInfo.CustomRegexFilterItems?.
                    Any(a =>
                        a.RegexPattern.
                            Equals(BarcodeFilterSettingsInfo.
                                BasicFilterInfo.RegularExpression)) == true)
            {
                base.MessageQueue.Enqueue("该表达式已存在!");
                return;
            }
            BarcodeFilterSettingsInfo.CustomRegexFilterItems?.Add(new CustomRegexFilterItemInfoModel()
            {
                Num = BarcodeFilterSettingsInfo.CustomRegexFilterItems.Count + 1,
                IsActive = true,
                RegexPattern = BarcodeFilterSettingsInfo.
                    BasicFilterInfo.RegularExpression
            });
        }

        private async void TestDelegate()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (BarcodeFilterSettingsInfo.BarCodeFilterOptions == BarCodeFilterOptions.BasicFilter)
                {
                    try
                    {
                        var isMatch = Regex.IsMatch(TestBarcode, BarcodeFilterSettingsInfo.BasicFilterInfo.RegularExpression);
                        base.MessageQueue.Enqueue(isMatch ? Languages.Language.ResourceManager.GetString("验证通过") ?? string.Empty
                            : Languages.Language.ResourceManager.GetString("验证不通过") ?? string.Empty);
                    }
                    catch (Exception e)
                    {
                        base.MessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("不是正确的正则表达式") ?? string.Empty);
                    }
                }
                else if (BarcodeFilterSettingsInfo.BarCodeFilterOptions == BarCodeFilterOptions.CustomRegexFilter)
                {
                    try
                    {
                        var isMatch = BarcodeFilterSettingsInfo.CustomRegexFilterItems.Where(w => w.IsActive).Any(a =>
                            Regex.IsMatch(CustomRegexFilterTestBarcode, a.RegexPattern));
                        base.MessageQueue.Enqueue(isMatch ? Languages.Language.ResourceManager.GetString("验证通过") ?? string.Empty
                            : Languages.Language.ResourceManager.GetString("验证不通过") ?? string.Empty);
                    }
                    catch (Exception e)
                    {
                        base.MessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("不是正确的正则表达式") ?? string.Empty);
                    }
                }
            });
        }

        public override string Identifier => "SettingDialog";
        public override string SettingsName => "BarcodeFilterSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel()
            {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new BarcodeFilterSettingsDto
                {
                    BasicFilterInfo = new BasicFilterInfo
                    {
                        MinimumLength = BarcodeFilterSettingsInfo.BasicFilterInfo.MinimumLength,
                        MaximumLength = BarcodeFilterSettingsInfo.BasicFilterInfo.MaximumLength,
                        StartCharacterType = BarcodeFilterSettingsInfo.BasicFilterInfo.StartCharacterType,
                        EndCharacterType = BarcodeFilterSettingsInfo.BasicFilterInfo.EndCharacterType,
                        DisallowedCharacters = BarcodeFilterSettingsInfo.BasicFilterInfo.DisallowedCharacters,
                        RequiredCharacters = BarcodeFilterSettingsInfo.BasicFilterInfo.RequiredCharacters,
                        RegularExpression = BarcodeFilterSettingsInfo.BasicFilterInfo.RegularExpression,
                        AnyCharacters = BarcodeFilterSettingsInfo.BasicFilterInfo.AnyCharacters,
                        AnyStartCodes = BarcodeFilterSettingsInfo.BasicFilterInfo.AnyStartCodes
                    },
                    ScanInterval = BarcodeFilterSettingsInfo.ScanInterval,
                    DuplicateBarcodeFilterCount = BarcodeFilterSettingsInfo.DuplicateBarcodeFilterCount,
                    FilterOutputType = BarcodeFilterSettingsInfo.FilterOutputType,
                    MergeTimeout = BarcodeFilterSettingsInfo.MergeTimeout,
                    MultiBarcodeDelimiter = BarcodeFilterSettingsInfo.MultiBarcodeDelimiter,
                    BarCodeFilterOptions = BarcodeFilterSettingsInfo.BarCodeFilterOptions,
                    IsUseCustomRegexReplacement = BarcodeFilterSettingsInfo.IsUseCustomRegexReplacement,
                    IsUseFilteredBarcodeTypes = BarcodeFilterSettingsInfo.IsUseFilteredBarcodeTypes,
                    CustomRegexFilterItems = BarcodeFilterSettingsInfo.CustomRegexFilterItems.Select(s =>
                        new CustomRegexFilterInfo
                        {
                            IsActive = s.IsActive,
                            RegexPattern = s.RegexPattern,
                            Remarks = s.Remarks
                        })?.ToList() ?? new List<CustomRegexFilterInfo>(),
                    CustomRegexReplacementItems = BarcodeFilterSettingsInfo.CustomRegexReplacementItems.Select(s =>
                        new CustomRegexReplacementInfo
                        {
                            IsActive = s.IsActive,
                            RegexPattern = s.RegexPattern,
                            ReplaceContent = s.ReplaceContent,
                            Remarks = s.Remarks
                        })?.ToList() ?? new List<CustomRegexReplacementInfo>()
                })
            });
            base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                Languages.Language.ResourceManager.GetString("Success") :
                Languages.Language.ResourceManager.GetString("Failure"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var settingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(SettingsName) ??
                                      new BarcodeFilterSettingsDto();

                    BarcodeFilterSettingsInfo = new BarcodeFilterSettingsInfoModel
                    {
                        BasicFilterInfo = new BasicFilterInfoModel
                        {
                            MinimumLength = settingsDto.BasicFilterInfo.MinimumLength,
                            MaximumLength = settingsDto.BasicFilterInfo.MaximumLength,
                            StartCharacterType = settingsDto.BasicFilterInfo.StartCharacterType,
                            EndCharacterType = settingsDto.BasicFilterInfo.EndCharacterType,
                            DisallowedCharacters = settingsDto.BasicFilterInfo.DisallowedCharacters,
                            RequiredCharacters = settingsDto.BasicFilterInfo.RequiredCharacters,
                            RegularExpression = settingsDto.BasicFilterInfo.RegularExpression,
                            AnyCharacters = settingsDto.BasicFilterInfo.AnyCharacters,
                            AnyStartCodes = settingsDto.BasicFilterInfo.AnyStartCodes
                        },
                        ScanInterval = settingsDto.ScanInterval,
                        DuplicateBarcodeFilterCount = settingsDto.DuplicateBarcodeFilterCount,
                        FilterOutputType = settingsDto.FilterOutputType,
                        MergeTimeout = settingsDto.MergeTimeout,
                        MultiBarcodeDelimiter = settingsDto.MultiBarcodeDelimiter,
                        BarCodeFilterOptions = settingsDto.BarCodeFilterOptions,
                        IsUseCustomRegexReplacement = settingsDto.IsUseCustomRegexReplacement,
                        IsUseFilteredBarcodeTypes = settingsDto.IsUseFilteredBarcodeTypes,
                        CustomRegexFilterItems = new ObservableCollection<CustomRegexFilterItemInfoModel>(settingsDto.CustomRegexFilterItems.Select((s, i) =>
                            new CustomRegexFilterItemInfoModel
                            {
                                Num = i + 1,
                                IsActive = s.IsActive,
                                RegexPattern = s.RegexPattern,
                                Remarks = s.Remarks
                            })?.ToList() ?? new List<CustomRegexFilterItemInfoModel>()),
                        CustomRegexReplacementItems = new ObservableCollection<CustomRegexReplacementItemInfoModel>(settingsDto.CustomRegexReplacementItems.
                            Select((s, i) => new CustomRegexReplacementItemInfoModel
                            {
                                Num = i + 1,
                                IsActive = s.IsActive,
                                RegexPattern = s.RegexPattern,
                                ReplaceContent = s.ReplaceContent,
                                Remarks = s.Remarks
                            })?.ToList() ?? new List<CustomRegexReplacementItemInfoModel>())
                    };

                    FilterTypeSelectedIndex = BarcodeFilterSettingsInfo.BarCodeFilterOptions switch
                    {
                        BarCodeFilterOptions.BasicFilter => 0,
                        BarCodeFilterOptions.CustomRegexFilter => 1,
                        _ => 0
                    };
                });
            }
        }

        /// <summary>
        /// 改变正则表达
        /// </summary>
        private async void UpdateRegularExpression()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var regularChars = new List<string>();
                //不能包含
                if (!string.IsNullOrWhiteSpace(BarcodeFilterSettingsInfo.BasicFilterInfo.DisallowedCharacters))
                {
                    var strings = BarcodeFilterSettingsInfo.BasicFilterInfo.DisallowedCharacters.Split(";");
                    strings.ForEach(f =>
                    {
                        regularChars.Add($"(^(?!.*{f}))");
                    });
                }
                //必须包含
                if (!string.IsNullOrWhiteSpace(BarcodeFilterSettingsInfo.BasicFilterInfo.RequiredCharacters))
                {
                    var strings = BarcodeFilterSettingsInfo.BasicFilterInfo.RequiredCharacters.Split(";");
                    strings.ForEach(f =>
                    {
                        regularChars.Add($"(?=.*{f})");
                    });
                }
                //包含任意
                if (!string.IsNullOrWhiteSpace(BarcodeFilterSettingsInfo.BasicFilterInfo.AnyCharacters))
                {
                    var strings = BarcodeFilterSettingsInfo.BasicFilterInfo.AnyCharacters.Replace(";", "|");

                    regularChars.Add($"(?=.*(?:{strings}))");
                }
                //开头字符
                switch (BarcodeFilterSettingsInfo.BasicFilterInfo.StartCharacterType)
                {
                    case CharacterType.Alphanumeric:
                        regularChars.Add("(?=^([0-9a-zA-Z]).*)");
                        break;

                    case CharacterType.Letter:
                        regularChars.Add("(?=^([a-zA-Z]).*)");
                        break;

                    case CharacterType.Number:
                        regularChars.Add("(?=^([0-9]).*)");
                        break;

                    case CharacterType.Any:
                        break;
                }
                //结尾字符
                switch (BarcodeFilterSettingsInfo.BasicFilterInfo.EndCharacterType)
                {
                    case CharacterType.Alphanumeric:
                        regularChars.Add("(?=.*([0-9a-zA-Z])$)");
                        break;

                    case CharacterType.Letter:
                        regularChars.Add("(?=.*([a-zA-Z])$)");
                        break;

                    case CharacterType.Number:
                        regularChars.Add("(?=.*([0-9])$)");
                        break;

                    case CharacterType.Any:
                        break;
                }
                //条码开头
                if (!string.IsNullOrWhiteSpace(BarcodeFilterSettingsInfo.BasicFilterInfo.AnyStartCodes))
                {
                    var strings = BarcodeFilterSettingsInfo.BasicFilterInfo.AnyStartCodes.Replace(";", "|");

                    regularChars.Add($"(^(?={strings}).*)");
                }
                //位数限制
                regularChars.Add($"(^.{{{BarcodeFilterSettingsInfo.BasicFilterInfo.MinimumLength},{BarcodeFilterSettingsInfo.BasicFilterInfo.MaximumLength}}}$)");

                BarcodeFilterSettingsInfo.BasicFilterInfo.RegularExpression = string.Join(string.Empty, regularChars);
            });
        }

        /// <summary>
        /// 导出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="excelTitle"></param>
        /// <param name="sheetName"></param>
        private async void ExportDelegate<T>(List<T> items, string excelTitle, string sheetName)
        {
            if (items?.Any() != true)
            {
                MessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
                return;
            }
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Please select the location to save the file.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model)
                {
                    model.FilePath = saveFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);
                    try
                    {
                        var export = await _excel.Export(saveFileDialog.FileName,
                            excelTitle,
                            sheetName, items,
                            new List<string>(), async p =>
                            {
                                model.Progress = p;
                                model.ProgressText = $"{p}%";
                                if (p == 100)
                                {
                                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (DialogHost.IsDialogOpen(model.Identifier))
                                        {
                                            DialogHost.Close(model.Identifier);
                                        }
                                    });
                                }
                            }, e =>
                            {
                                MessageQueue?.Enqueue(e.Message);
                            });
                        if (!export)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (DialogHost.IsDialogOpen(model.Identifier))
                                {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        MessageQueue?.Enqueue(e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 导入
        /// </summary>
        private async Task<List<T>> ImportDelegate<T>() where T : class, new()
        {
            //导入
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Please select the file to import.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model)
                {
                    model.FilePath = openFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);

                    var readExcel = await _excel.ReadExcel<T>(openFileDialog.FileName, async p =>
                    {
                        model.Progress = p;
                        model.ProgressText = $"{p}%";
                        if (p == 100)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (DialogHost.IsDialogOpen(model.Identifier))
                                {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }, async e =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (DialogHost.IsDialogOpen(model.Identifier))
                            {
                                DialogHost.Close(model.Identifier);
                            }
                        });
                        MessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (readExcel?.Any() == true)
                    {
                        return readExcel;
                    }
                }
            }

            return new List<T>();
        }
    }
}