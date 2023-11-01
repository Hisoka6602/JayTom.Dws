using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors {

    public class OcrSortingRuleEditorViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;
        private ObservableCollection<OcrRuleItemInfoModel> _ocrRuleItems = new();
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private OcrSortingItemInfoModel _ocrSortingItemInfo = new();
        private bool _isUseRules = true;
        private bool _isUseRegex;
        private int _startPosition;
        private int _endPosition;
        private string _content = string.Empty;
        private string? _regexPattern;

        public OcrSortingRuleEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        public ObservableCollection<OcrRuleItemInfoModel> OcrRuleItems {
            get => _ocrRuleItems;
            set => SetProperty(ref _ocrRuleItems, value);
        }

        /// <summary>
        /// 格口列表
        /// </summary>
        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        /// <summary>
        /// 绑定的格口
        /// </summary>
        public PackageExitDefinitionItemInfoModel SelectPackageExitDefinitionInfo {
            get => _selectPackageExitDefinitionInfo;
            set => SetProperty(ref _selectPackageExitDefinitionInfo, value);
        }

        public OcrSortingItemInfoModel OcrSortingItemInfo {
            get => _ocrSortingItemInfo;
            set => SetProperty(ref _ocrSortingItemInfo, value);
        }

        /// <summary>
        /// 是否使用规则
        /// </summary>
        public bool IsUseRules {
            get => _isUseRules;
            set => SetProperty(ref _isUseRules, value);
        }

        /// <summary>
        /// 是否使用正则表达式
        /// </summary>
        public bool IsUseRegex {
            get => _isUseRegex;
            set => SetProperty(ref _isUseRegex, value);
        }

        /// <summary>
        /// 字符开始位置
        /// </summary>
        public int StartPosition {
            get => _startPosition;
            set => SetProperty(ref _startPosition, value);
        }

        /// <summary>
        /// 字符结束位置
        /// </summary>
        public int EndPosition {
            get => _endPosition;
            set => SetProperty(ref _endPosition, value);
        }

        /// <summary>
        /// 字符内容
        /// </summary>
        public string Content {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string? RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        public ICommand DeleteRegexCommand {
            get => new DelegateCommand<OcrRuleItemInfoModel>(DeleteRegexDelegate);
        }

        private async void DeleteRegexDelegate(OcrRuleItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                OcrRuleItems.Remove(obj);
                //调整Num
                if (OcrRuleItems?.Any() == true) {
                    for (var i = 0; i < OcrRuleItems.Count; i++) {
                        OcrRuleItems[i].Num = i + 1;
                    }
                }
            });
        }

        public ICommand AddRegexCommand {
            get => new DelegateCommand<object>(AddRegexDelegate);
        }

        private async void AddRegexDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (IsUseRules) {
                    if (EndPosition > StartPosition &&
                        !string.IsNullOrEmpty(Content)) {
                        try {
                            var regular = $"(?<=^.{{{StartPosition}}})({Content})";
                            if (!OcrRuleItems.Any(a => a.RegexPattern.Equals(regular))) {
                                OcrRuleItems.Add(new OcrRuleItemInfoModel() {
                                    CreateTime = DateTime.Now,
                                    ModifyTime = DateTime.Now,
                                    Num = OcrRuleItems.Count + 1,
                                    OcrSortingId = OcrSortingItemInfo.Id,
                                    RegexPattern = regular
                                });
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine(e);
                        }
                    }
                }
                else {
                    if (!string.IsNullOrEmpty(RegexPattern)) {
                        if (!OcrRuleItems.Any(a => a.RegexPattern.Equals(RegexPattern))) {
                            OcrRuleItems.Add(new OcrRuleItemInfoModel() {
                                CreateTime = DateTime.Now,
                                ModifyTime = DateTime.Now,
                                Num = OcrRuleItems.Count + 1,
                                OcrSortingId = OcrSortingItemInfo.Id,
                                RegexPattern = RegexPattern
                            });
                        }
                    }
                }
            });
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private async void ClearConditionsDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                StartPosition =
                    EndPosition = 0;
                Content = string.Empty;
            });
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private async void SaveDelegate() {
            try {
                IsOk = true;
                OcrSortingItemInfo.ModifyTime = DateTime.Now;

                Pitcher.Throw.ArgumentNull.WhenNull(OcrSortingItemInfo, nameof(OcrSortingItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(OcrSortingItemInfo.SortingName, nameof(OcrSortingItemInfo.SortingName));
                foreach (var ocrRuleItemInfoModel in OcrRuleItems) {
                    Regex.IsMatch("aa", ocrRuleItemInfoModel.RegexPattern);
                }
                if (SelectPackageExitDefinitionInfo.Id <= 0) {
                    throw new Exception("格口未选择!");
                }
            }
            catch (Exception e) {
                IsOk = false;
                ExceptionContent = e.Message;
            }

            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand {
            get => new DelegateCommand(CancelDelegate);
        }

        private void CancelDelegate() {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.CreateTime);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                PackageExitDefinitionItems.Clear();
                var packageExitDefinitionItemInfoModels = packageExitDefinitionInfoModels?.Select((s, i) =>
                    new PackageExitDefinitionItemInfoModel {
                        CreateTime = s.CreateTime,
                        ExitName = $"{s.ExitName}{(s.IsActive ? "" : "(未生效)")}",
                        Id = s.Id,
                        IsActive = s.IsActive,
                        ModifyTime = s.ModifyTime,
                        Num = i + 1,
                        Remarks = s.Remarks,
                        Type = s.Type
                    })?.ToList();

                if (packageExitDefinitionItemInfoModels?.Any() == true) {
                    PackageExitDefinitionItems.AddRange(packageExitDefinitionItemInfoModels);
                    var packageExitDefinitionItemInfoModel = PackageExitDefinitionItems.FirstOrDefault(f =>
                        f.Id.Equals(OcrSortingItemInfo.ExitId));
                    SelectPackageExitDefinitionInfo =
                        packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
            });
        }

        public ICommand StartPositionChangedCommand {
            get => new DelegateCommand<object>(StartPositionChangedDelegate);
        }

        private async void StartPositionChangedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                EndPosition = StartPosition + Content.Length;
            });
        }

        public ICommand ContentChangedCommand {
            get => new DelegateCommand<object>(ContentChangedDelegate);
        }

        private async void ContentChangedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                EndPosition = StartPosition + Content.Length;
            });
        }
    }
}