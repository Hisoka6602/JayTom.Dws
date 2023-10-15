using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Net.Http.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors {

    public class WeightSortingRuleEditorViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;
        private ObservableCollection<ItemBaseTemplateModel> _formulaTemplate = new();
        private string _formula = string.Empty;
        private bool _isUseTemplateFormula = true;
        private bool _isUseCustomFormula;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private WeightSortingItemInfoModel _weightSortingItemInfo = new();
        private ObservableCollection<WeightRuleItemInfoModel> _weightRuleItems = new();

        public WeightSortingRuleEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<ItemBaseTemplateModel> FormulaTemplate {
            get => _formulaTemplate;
            set => SetProperty(ref _formulaTemplate, value);
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

        /// <summary>
        /// 公式
        /// </summary>
        public string Formula {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        /// <summary>
        /// 自定义公式
        /// </summary>
        public string CustomFormula { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用模板公式
        /// </summary>
        public bool IsUseTemplateFormula {
            get => _isUseTemplateFormula;
            set => SetProperty(ref _isUseTemplateFormula, value);
        }

        /// <summary>
        /// /是否使用自定义公式
        /// </summary>
        public bool IsUseCustomFormula {
            get => _isUseCustomFormula;
            set => SetProperty(ref _isUseCustomFormula, value);
        }

        public WeightSortingItemInfoModel WeightSortingItemInfo {
            get => _weightSortingItemInfo;
            set => SetProperty(ref _weightSortingItemInfo, value);
        }

        public ObservableCollection<WeightRuleItemInfoModel> WeightRuleItems {
            get => _weightRuleItems;
            set => SetProperty(ref _weightRuleItems, value);
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

        /// <summary>
        /// 添加数据模板
        /// </summary>
        public ICommand AddDataItemCommand {
            get => new DelegateCommand<string>(AddDataItemDelegate);
        }

        private async void AddDataItemDelegate(string obj) {
            //判断前面是空或者连接符
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (!FormulaTemplate.Any() || FormulaTemplate.LastOrDefault()?.Type == 6) {
                    obj = obj.Replace("'", string.Empty);
                    FormulaTemplate.Add(new ItemBaseTemplateModel {
                        Content = obj,
                        Id = FormulaTemplate.Count,
                        Type = 1,
                        ApplicationType = ItemApplicationType.Formula
                    });
                }
                Formula = ChangeFormulaContent(FormulaTemplate);
            });
        }

        /// <summary>
        /// 添加运算符
        /// </summary>
        public ICommand AddOperatorCommand {
            get => new DelegateCommand<string>(AddOperatorDelegate);
        }

        private async void AddOperatorDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                //添加之前判断前面是不是普通值
                if (FormulaTemplate.LastOrDefault()?.Type == 1) {
                    obj = obj.Replace("'", string.Empty);
                    FormulaTemplate.Add(new ItemBaseTemplateModel {
                        Content = obj,
                        Id = FormulaTemplate.Count,
                        Type = 4,
                        ApplicationType = ItemApplicationType.Formula
                    });
                }

                Formula = ChangeFormulaContent(FormulaTemplate);
            });
        }

        /// <summary>
        /// 添加参照值
        /// </summary>
        public ICommand AddReferenceValueCommand {
            get => new DelegateCommand<string>(AddReferenceValueDelegate);
        }

        private async void AddReferenceValueDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                //添加之前判断前面是不是普通值
                if (FormulaTemplate.LastOrDefault()?.Type == 4) {
                    obj = obj.Replace("'", string.Empty);
                    var tryParse = float.TryParse(obj, out _);
                    if (tryParse) {
                        FormulaTemplate.Add(new ItemBaseTemplateModel {
                            Content = obj,
                            Id = FormulaTemplate.Count,
                            Type = 5,
                            ApplicationType = ItemApplicationType.Formula
                        });
                    }
                }

                Formula = ChangeFormulaContent(FormulaTemplate);
            });
        }

        /// <summary>
        /// 添加拼接函数
        /// </summary>
        public ICommand AddStitchingCommand {
            get => new DelegateCommand<string>(AddStitchingDelegate);
        }

        private async void AddStitchingDelegate(string obj) {
            //判断前面是不是参照值
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                //添加之前判断前面是不是普通值
                if (FormulaTemplate.LastOrDefault()?.Type == 5) {
                    obj = obj.Replace("'", string.Empty);
                    FormulaTemplate.Add(new ItemBaseTemplateModel {
                        Content = obj,
                        Id = FormulaTemplate.Count,
                        Type = 6,
                        ApplicationType = ItemApplicationType.Formula
                    });
                }

                Formula = ChangeFormulaContent(FormulaTemplate);
            });
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.Formula) {
                    var indexOf = FormulaTemplate.IndexOf(model);

                    if (indexOf != -1) {
                        for (int i = FormulaTemplate.Count - 1; i >= indexOf; i--) {
                            FormulaTemplate.RemoveAt(i);
                        }
                    }
                    //FormulaTemplate.Remove(model);
                    foreach (var item in FormulaTemplate) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            FormulaTemplate.LastOrDefault() != item) {
                            FormulaTemplate.Remove(item);
                        }
                    }
                }

                Formula = ChangeFormulaContent(FormulaTemplate);
            });
        }

        private string ChangeFormulaContent(ObservableCollection<ItemBaseTemplateModel> dataTemplate) {
            var formulaContent = string.Empty;
            foreach (var templateModel in dataTemplate) {
                if (templateModel.ApplicationType == ItemApplicationType.Formula) {
                    switch (templateModel.Type) {
                        case 1: {
                                var replace = templateModel.Content.Replace("{", string.Empty).Replace("}", string.Empty);
                                formulaContent += $"{replace} ";
                                break;
                            }
                        default:
                            formulaContent += $"{templateModel.Content} ";
                            break;
                    }
                }
            }

            return formulaContent;
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
                        f.Id.Equals(WeightSortingItemInfo.ExitId));
                    SelectPackageExitDefinitionInfo =
                        packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
            });
        }

        public ICommand DeleteFormulaCommand {
            get => new DelegateCommand<WeightRuleItemInfoModel>(DeleteFormulaDelegate);
        }

        private async void DeleteFormulaDelegate(WeightRuleItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                WeightRuleItems.Remove(obj);
                //调整Num
                if (WeightRuleItems?.Any() == true) {
                    for (var i = 0; i < WeightRuleItems.Count; i++) {
                        WeightRuleItems[i].Num = i + 1;
                    }
                }
            });
        }

        public ICommand SaveRuleCommand {
            get => new DelegateCommand<object>(SaveRuleDelegate);
        }

        private async void SaveRuleDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                string formula = IsUseCustomFormula ? CustomFormula : Formula;
                if (!string.IsNullOrEmpty(formula) && !WeightRuleItems.Any(a => a.Formula.Equals(formula))) {
                    WeightRuleItems.Add(new WeightRuleItemInfoModel() {
                        CreateTime = DateTime.Now,
                        Formula = formula,
                        ModifyTime = DateTime.Now,
                        Num = WeightRuleItems.Count + 1,
                        WeightSortingId = WeightSortingItemInfo.Id
                    });
                }
            });
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private void ClearConditionsDelegate(object obj) {
            Formula = CustomFormula = string.Empty;
            FormulaTemplate.Clear();
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate() {
            try {
                IsOk = true;
                WeightSortingItemInfo.ModifyTime = DateTime.Now;
                Pitcher.Throw.ArgumentNull.WhenNull(WeightSortingItemInfo, nameof(WeightSortingItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(WeightSortingItemInfo.SortingName, nameof(WeightSortingItemInfo.SortingName));

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
    }
}