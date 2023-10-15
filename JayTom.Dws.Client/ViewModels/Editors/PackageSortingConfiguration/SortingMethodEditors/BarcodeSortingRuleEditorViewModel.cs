using System;
using ImTools;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors {
    public class BarcodeSortingRuleEditorViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;
        private bool _isUseRules = true;
        private bool _isUseRegex;
        private string? _requiredCharacters;
        private string? _startCharacterType;
        private string? _endCharacterType;
        private string? _regexPattern;

        private ObservableCollection<BarCodeRegexItemInfoModel> _barCodeRegexItems = new();

        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private BarCodeSortingItemInfoModel _barCodeSortingItemInfo = new();

        public BarcodeSortingRuleEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository) {
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

        public ObservableCollection<BarCodeRegexItemInfoModel> BarCodeRegexItems {
            get => _barCodeRegexItems;
            set => SetProperty(ref _barCodeRegexItems, value);
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

        public BarCodeSortingItemInfoModel BarCodeSortingItemInfo {
            get => _barCodeSortingItemInfo;
            set => SetProperty(ref _barCodeSortingItemInfo, value);
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
        /// 必须包含的字符
        /// </summary>
        public string? RequiredCharacters {
            get => _requiredCharacters;
            set => SetProperty(ref _requiredCharacters, value);
        }

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public string? StartCharacter {
            get => _startCharacterType;
            set => SetProperty(ref _startCharacterType, value);
        }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public string? EndCharacter {
            get => _endCharacterType;
            set => SetProperty(ref _endCharacterType, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string? RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        public ICommand DeleteRegexCommand {
            get => new DelegateCommand<BarCodeRegexItemInfoModel>(DeleteRegexDelegate);
        }

        private async void DeleteRegexDelegate(BarCodeRegexItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                BarCodeRegexItems.Remove(obj);
                //调整Num
                if (BarCodeRegexItems?.Any() == true) {
                    for (var i = 0; i < BarCodeRegexItems.Count; i++) {
                        BarCodeRegexItems[i].Num = i + 1;
                    }
                }
            });
        }

        public ICommand SaveRuleCommand {
            get => new DelegateCommand<object>(SaveRuleDelegate);
        }

        private async void SaveRuleDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (IsUseRules) {
                    var regularChars = new List<string>();
                    //必须包含
                    if (!string.IsNullOrWhiteSpace(RequiredCharacters)) {
                        var strings = RequiredCharacters.Split(";");
                        strings.ForEach(f => {
                            regularChars.Add($"(?=.*{f})");
                        });
                    }
                    //指定开头
                    if (!string.IsNullOrWhiteSpace(StartCharacter)) {
                        var replace = StartCharacter.Replace(";", "|");
                        regularChars.Add($"(?=^({replace}).*)");
                    }

                    //指定结尾
                    if (!string.IsNullOrWhiteSpace(EndCharacter)) {
                        var replace = EndCharacter.Replace(";", "|");
                        regularChars.Add($"(?=.*({replace})$)");
                    }
                    //字符限制
                    var join = string.Join(string.Empty, regularChars);
                    if (!string.IsNullOrEmpty(join) && !BarCodeRegexItems.Any(a => a.RegexPattern.Equals(join))) {
                        BarCodeRegexItems.Add(new BarCodeRegexItemInfoModel() {
                            CreateTime = DateTime.Now,
                            ModifyTime = DateTime.Now,
                            Num = BarCodeRegexItems.Count + 1,
                            RegexPattern = join
                        });
                    }
                }
                else if (IsUseRegex) {
                    if (!string.IsNullOrEmpty(RegexPattern) &&
                        !BarCodeRegexItems.Any(a => a.RegexPattern.Equals(RegexPattern))) {
                        BarCodeRegexItems.Add(new BarCodeRegexItemInfoModel() {
                            CreateTime = DateTime.Now,
                            ModifyTime = DateTime.Now,
                            Num = BarCodeRegexItems.Count + 1,
                            RegexPattern = RegexPattern
                        });
                    }
                }
            });
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private async void ClearConditionsDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                RequiredCharacters =
                        StartCharacter =
                            EndCharacter =
                                RegexPattern = null;
            });
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate() {
            //规则需要同步到表[使用同步:多删少增]
            try {
                IsOk = true;
                BarCodeSortingItemInfo.ModifyTime = DateTime.Now;

                Pitcher.Throw.ArgumentNull.WhenNull(BarCodeSortingItemInfo, nameof(BarCodeSortingItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(BarCodeSortingItemInfo.SortingName, nameof(BarCodeSortingItemInfo.SortingName));
                if (!BarCodeRegexItems.Any()) {
                    throw new Exception("规则不能为空!");
                }

                foreach (var barCodeRegexItemInfoModel in BarCodeRegexItems) {
                    Regex.IsMatch("aa", barCodeRegexItemInfoModel.RegexPattern);
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
                        f.Id.Equals(BarCodeSortingItemInfo.ExitId));
                    SelectPackageExitDefinitionInfo =
                        packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
            });
        }
    }
}