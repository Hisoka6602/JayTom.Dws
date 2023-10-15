using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using NPOI.SS.Formula;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors {
    public class LogisticsSortingRuleEditorViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private LogisticsSortingItemInfoModel _logisticsSortingItemInfo = new();
        private ObservableCollection<LogisticsCodeRecognitionItemInfoModel> _logisticsCodeRecognitionItems = new();
        private LogisticsCodeRecognitionItemInfoModel _selectLogisticsCodeRecognition = new();
        private ObservableCollection<LogisticsRuleItemInfoModel> _logisticsRuleItems = new();

        public LogisticsSortingRuleEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
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

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public PackageExitDefinitionItemInfoModel SelectPackageExitDefinitionInfo {
            get => _selectPackageExitDefinitionInfo;
            set => SetProperty(ref _selectPackageExitDefinitionInfo, value);
        }

        public ObservableCollection<LogisticsCodeRecognitionItemInfoModel> LogisticsCodeRecognitionItems {
            get => _logisticsCodeRecognitionItems;
            set => SetProperty(ref _logisticsCodeRecognitionItems, value);
        }

        public LogisticsCodeRecognitionItemInfoModel SelectLogisticsCodeRecognition {
            get => _selectLogisticsCodeRecognition;
            set => SetProperty(ref _selectLogisticsCodeRecognition, value);
        }

        public LogisticsSortingItemInfoModel LogisticsSortingItemInfo {
            get => _logisticsSortingItemInfo;
            set => SetProperty(ref _logisticsSortingItemInfo, value);
        }

        public ObservableCollection<LogisticsRuleItemInfoModel> LogisticsRuleItems {
            get => _logisticsRuleItems;
            set => SetProperty(ref _logisticsRuleItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.CreateTime);
            var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                o => o.ModifyTime);
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
                        f.Id.Equals(LogisticsSortingItemInfo.ExitId));
                    SelectPackageExitDefinitionInfo =
                        packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }

                if (logisticsCodeRecognitionInfoModels?.Any() == true) {
                    LogisticsCodeRecognitionItems.Clear();
                    var logisticsCodeRecognitionItemInfoModels = logisticsCodeRecognitionInfoModels.Select(s => new LogisticsCodeRecognitionItemInfoModel {
                        CreateTime = s.CreateTime,
                        ModifyTime = s.ModifyTime,
                        IconName = s.IconName,
                        LogisticsCode = s.LogisticsCode,
                        LogisticsName = s.LogisticsName,
                        Id = s.Id,
                    })?.ToList();

                    LogisticsCodeRecognitionItems.AddRange(logisticsCodeRecognitionItemInfoModels);
                }
            });
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate() {
            try {
                IsOk = true;
                LogisticsSortingItemInfo.ModifyTime = DateTime.Now;
                if (LogisticsRuleItems?.Any() != true) {
                    throw new Exception("未选择物流!");
                }
                Pitcher.Throw.ArgumentNull.WhenNull(LogisticsRuleItems, nameof(LogisticsRuleItems));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(LogisticsSortingItemInfo.SortingName, nameof(LogisticsSortingItemInfo.SortingName));
                if (!LogisticsRuleItems.Any()) {
                    throw new Exception("物流不能为空!");
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

        public ICommand AddLogisticsItemCommand {
            get => new DelegateCommand(AddLogisticsItemDelegate);
        }

        private async void AddLogisticsItemDelegate() {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (!LogisticsRuleItems.Any(a => a.LogisticsId.Equals(SelectLogisticsCodeRecognition.Id))) {
                    LogisticsRuleItems.Add(new LogisticsRuleItemInfoModel() {
                        CreateTime = DateTime.Now,
                        LogisticsId = SelectLogisticsCodeRecognition.Id,
                        LogisticsName = SelectLogisticsCodeRecognition.LogisticsName,
                        LogisticsSortingId = LogisticsSortingItemInfo.Id,
                        ModifyTime = DateTime.Now,
                        Num = LogisticsRuleItems.Count + 1,
                    });
                }
            });
        }

        public ICommand DeleteLogisticsItemCommand {
            get => new DelegateCommand<LogisticsRuleItemInfoModel>(DeleteLogisticsItemDelegate);
        }

        private async void DeleteLogisticsItemDelegate(LogisticsRuleItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LogisticsRuleItems.Remove(obj);
                //调整Num
                if (LogisticsRuleItems?.Any() == true) {
                    for (var i = 0; i < LogisticsRuleItems.Count; i++) {
                        LogisticsRuleItems[i].Num = i + 1;
                    }
                }
            });
        }
    }
}