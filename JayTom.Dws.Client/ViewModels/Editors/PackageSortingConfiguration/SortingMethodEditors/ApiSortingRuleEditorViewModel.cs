using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors
{
    public class ApiSortingRuleEditorViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;
        private ObservableCollection<ApiRuleItemInfoModel> _apiRuleItems = new();
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private UploadStatus _responseStatus;
        private bool _isUseStringComparison;
        private bool _isUseStringSearch;
        private bool _isUseJsonField = true;
        private string _searchStringContent = string.Empty;
        private string _jsonField = string.Empty;
        private string _jsonFieldValue = string.Empty;
        private ObservableCollection<UploadStatus> _uploadStatusItems = new(Enum.GetValues(typeof(UploadStatus)).Cast<UploadStatus>());
        private ApiSortingItemInfoModel _apiSortingItemInfo = new();

        public ApiSortingRuleEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository) {
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

        public ObservableCollection<ApiRuleItemInfoModel> ApiRuleItems {
            get => _apiRuleItems;
            set => SetProperty(ref _apiRuleItems, value);
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

        public ObservableCollection<UploadStatus> UploadStatusItems {
            get => _uploadStatusItems;
            set => SetProperty(ref _uploadStatusItems, value);
        }

        public ApiSortingItemInfoModel ApiSortingItemInfo {
            get => _apiSortingItemInfo;
            set => SetProperty(ref _apiSortingItemInfo, value);
        }

        /// <summary>
        /// 上传状态
        /// </summary>
        public UploadStatus ResponseStatus {
            get => _responseStatus;
            set => SetProperty(ref _responseStatus, value);
        }

        /// <summary>
        /// 是否使用字符串判断
        /// </summary>
        public bool IsUseStringComparison {
            get => _isUseStringComparison;
            set => SetProperty(ref _isUseStringComparison, value);
        }

        /// <summary>
        /// 是否使用字符串查找
        /// </summary>
        public bool IsUseStringSearch {
            get => _isUseStringSearch;
            set => SetProperty(ref _isUseStringSearch, value);
        }

        /// <summary>
        /// 是否使用字符串取值
        /// </summary>
        public bool IsUseJsonField {
            get => _isUseJsonField;
            set => SetProperty(ref _isUseJsonField, value);
        }

        /// <summary>
        /// 查找字符串内容
        /// </summary>
        public string SearchStringContent {
            get => _searchStringContent;
            set => SetProperty(ref _searchStringContent, value);
        }

        /// <summary>
        /// Json字段
        /// </summary>
        public string JsonField {
            get => _jsonField;
            set => SetProperty(ref _jsonField, value);
        }

        /// <summary>
        /// Json字段值
        /// </summary>
        public string JsonFieldValue {
            get => _jsonFieldValue;
            set => SetProperty(ref _jsonFieldValue, value);
        }

        public ICommand DeleteJsonCommand {
            get => new DelegateCommand<ApiRuleItemInfoModel>(DeleteJsonDelegate);
        }

        private async void DeleteJsonDelegate(ApiRuleItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                ApiRuleItems.Remove(obj);
                //调整Num
                if (ApiRuleItems?.Any() == true) {
                    for (var i = 0; i < ApiRuleItems.Count; i++) {
                        ApiRuleItems[i].Num = i + 1;
                    }
                }
            });
        }

        public ICommand AddJsonCommand {
            get => new DelegateCommand<object>(AddJsonDelegate);
        }

        private async void AddJsonDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                var apiRuleJsonDto = new ApiRuleJsonDto() {
                    JsonField = JsonField,
                    JsonFieldValue = JsonFieldValue,
                    ResponseStatus = ResponseStatus,
                    SearchStringContent = SearchStringContent,
                    IsUseJsonField = IsUseJsonField,
                    IsUseStringComparison = IsUseStringComparison,
                    IsUseStringSearch = IsUseStringSearch
                };
                var serializeObject = JsonConvert.SerializeObject(apiRuleJsonDto);
                if (ApiRuleItems.Any(a => a.JsonContent.Equals(serializeObject)) != true) {
                    ApiRuleItems.Add(new ApiRuleItemInfoModel() {
                        ApiSortingId = ApiSortingItemInfo.Id,
                        CreateTime = DateTime.Now,
                        JsonContent = serializeObject,
                        ModifyTime = DateTime.Now,
                        Num = ApiRuleItems.Count + 1,
                    });
                }
            });
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private async void ClearConditionsDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                IsUseStringComparison = false;

                SearchStringContent =
                    JsonField =
                        JsonFieldValue = string.Empty;
            });
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private async void SaveDelegate() {
            try {
                IsOk = true;
                ApiSortingItemInfo.ModifyTime = DateTime.Now;

                Pitcher.Throw.ArgumentNull.WhenNull(ApiSortingItemInfo, nameof(ApiSortingItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(ApiSortingItemInfo.SortingName, nameof(ApiSortingItemInfo.SortingName));
                if (!ApiRuleItems.Any()) {
                    throw new Exception("规则不能为空!");
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
                        f.Id.Equals(ApiSortingItemInfo.ExitId));
                    SelectPackageExitDefinitionInfo =
                        packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
            });
        }
    }
}