using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

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
        private bool _isUseThreeSegmentCodeValidation;
        private string _threeSegmentCodeContainsChars = string.Empty;
        private bool _isUseRecipientAddressValidation;
        private string _recipientAddressContainsChars = string.Empty;
        private bool _isUseSenderAddressValidation;
        private string _senderAddressContainsChars = string.Empty;
        private bool _isUseSenderPhoneNumberValidation;
        private string _senderPhoneNumberEndsWith = string.Empty;

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

        #region 条件

        /// <summary>
        /// 是否使用三段码判断
        /// </summary>
        public bool IsUseThreeSegmentCodeValidation {
            get => _isUseThreeSegmentCodeValidation;
            set => SetProperty(ref _isUseThreeSegmentCodeValidation, value);
        }

        /// <summary>
        /// 三段码包含字符
        /// </summary>
        public string ThreeSegmentCodeContainsChars {
            get => _threeSegmentCodeContainsChars;
            set => SetProperty(ref _threeSegmentCodeContainsChars, value);
        }

        /// <summary>
        /// 是否使用收件人地址判断
        /// </summary>
        public bool IsUseRecipientAddressValidation {
            get => _isUseRecipientAddressValidation;
            set => SetProperty(ref _isUseRecipientAddressValidation, value);
        }

        /// <summary>
        /// 收件人地址包含字符
        /// </summary>
        public string RecipientAddressContainsChars {
            get => _recipientAddressContainsChars;
            set => SetProperty(ref _recipientAddressContainsChars, value);
        }

        /// <summary>
        /// 是否使用发件人地址判断
        /// </summary>
        public bool IsUseSenderAddressValidation {
            get => _isUseSenderAddressValidation;
            set => SetProperty(ref _isUseSenderAddressValidation, value);
        }

        /// <summary>
        /// 发件人地址包含字符
        /// </summary>
        public string SenderAddressContainsChars {
            get => _senderAddressContainsChars;
            set => SetProperty(ref _senderAddressContainsChars, value);
        }

        /// <summary>
        /// 是否使用发件人手机尾号判断
        /// </summary>
        public bool IsUseSenderPhoneNumberValidation {
            get => _isUseSenderPhoneNumberValidation;
            set => SetProperty(ref _isUseSenderPhoneNumberValidation, value);
        }

        /// <summary>
        /// 发件人手机尾号
        /// </summary>
        public string SenderPhoneNumberEndsWith {
            get => _senderPhoneNumberEndsWith;
            set => SetProperty(ref _senderPhoneNumberEndsWith, value);
        }

        #endregion 条件

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
                if (!IsUseRecipientAddressValidation &&
                    !IsUseSenderAddressValidation &&
                    !IsUseSenderPhoneNumberValidation &&
                    !IsUseThreeSegmentCodeValidation) {
                    return;
                }

                var ocrRuleJsonDto = new OcrRuleJsonDto() {
                    IsUseSenderPhoneNumberValidation = IsUseSenderPhoneNumberValidation,
                    IsUseRecipientAddressValidation = IsUseRecipientAddressValidation,
                    IsUseSenderAddressValidation = IsUseSenderAddressValidation,
                    IsUseThreeSegmentCodeValidation = IsUseThreeSegmentCodeValidation,
                    ThreeSegmentCodeContainsChars = ThreeSegmentCodeContainsChars,
                    RecipientAddressContainsChars = RecipientAddressContainsChars,
                    SenderAddressContainsChars = SenderAddressContainsChars,
                    SenderPhoneNumberEndsWith = SenderPhoneNumberEndsWith
                };

                var serializeObject = JsonConvert.SerializeObject(ocrRuleJsonDto);
                if (OcrRuleItems.Any(a => a.JsonContent.Equals(serializeObject)) != true) {
                    OcrRuleItems.Add(new OcrRuleItemInfoModel() {
                        OcrSortingId = OcrSortingItemInfo.Id,
                        CreateTime = DateTime.Now,
                        JsonContent = serializeObject,
                        ModifyTime = DateTime.Now,
                        Num = OcrRuleItems.Count + 1,
                        FormatJsonContent = FormatRule(serializeObject)
                    });
                }
            });
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private async void ClearConditionsDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                IsUseSenderPhoneNumberValidation =
                    IsUseRecipientAddressValidation =
                        IsUseRecipientAddressValidation =
                            IsUseThreeSegmentCodeValidation = false;
                SenderAddressContainsChars =
                    SenderPhoneNumberEndsWith =
                        RecipientAddressContainsChars =
                            ThreeSegmentCodeContainsChars = string.Empty;
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
                if (!OcrRuleItems.Any()) {
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
                /*EndPosition = StartPosition + Content.Length;*/
            });
        }

        public ICommand ContentChangedCommand {
            get => new DelegateCommand<object>(ContentChangedDelegate);
        }

        private async void ContentChangedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                /*EndPosition = StartPosition + Content.Length;*/
            });
        }

        public string FormatRule(string jsonContent) {
            var content = string.Empty;
            try {
                var ocrRuleJsonDto = JsonConvert.DeserializeObject<OcrRuleJsonDto>(jsonContent);
                if (ocrRuleJsonDto is not null) {
                    if (ocrRuleJsonDto.IsUseThreeSegmentCodeValidation) {
                        content += $"三段码包含:[{ocrRuleJsonDto.ThreeSegmentCodeContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseRecipientAddressValidation) {
                        content += $"收件人地址包含:[{ocrRuleJsonDto.RecipientAddressContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseSenderAddressValidation) {
                        content += $"发件人地址包含:[{ocrRuleJsonDto.SenderAddressContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseSenderPhoneNumberValidation) {
                        content += $"发件人手机尾号包含:[{ocrRuleJsonDto.SenderPhoneNumberEndsWith}]  ";
                    }

                    return content;
                }
            }
            catch (Exception e) {
                return "解析错误";
            }
            return "解析错误";
        }
    }
}