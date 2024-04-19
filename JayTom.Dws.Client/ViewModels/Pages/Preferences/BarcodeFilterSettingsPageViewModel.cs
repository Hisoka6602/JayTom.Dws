using System;
using ImTools;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class BarcodeFilterSettingsPageViewModel : SettingsPageTemplateViewModel {
        private int _minimumLength = 10;
        private int _maximumLength = 22;
        private CharacterType _startCharacterType = CharacterType.Alphanumeric;
        private CharacterType _endCharacterType = CharacterType.Number;
        private string _disallowedCharacters = string.Empty;
        private string _requiredCharacters = string.Empty;
        private int _scanInterval = 1000;
        private string _regularExpression = "(?=^([0-9a-zA-Z]).*)(?=.*([0-9])$)(^.{10,22}$)";
        private string _testBarcode = string.Empty;
        private bool _isLoaded;
        private int _duplicateBarcodeFilterCount;
        private string _anyCharacters = string.Empty;
        private FilterOutputType _filterOutputType = FilterOutputType.NotOutput;
        private int _mergeTimeout = 300;
        private string _multiBarcodeDelimiter = "_";

        public BarcodeFilterSettingsPageViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int MinimumLength {
            get => _minimumLength;
            set => SetProperty(ref _minimumLength, value);
        }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int MaximumLength {
            get => _maximumLength;
            set => SetProperty(ref _maximumLength, value);
        }

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public CharacterType StartCharacterType {
            get => _startCharacterType;
            set => SetProperty(ref _startCharacterType, value);
        }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public CharacterType EndCharacterType {
            get => _endCharacterType;
            set => SetProperty(ref _endCharacterType, value);
        }

        /// <summary>
        /// 不能包含的字符
        /// </summary>
        public string DisallowedCharacters {
            get => _disallowedCharacters;
            set => SetProperty(ref _disallowedCharacters, value);
        }

        /// <summary>
        /// 必须包含的字符
        /// </summary>
        public string RequiredCharacters {
            get => _requiredCharacters;
            set => SetProperty(ref _requiredCharacters, value);
        }

        /// <summary>
        /// 包含任意字符
        /// </summary>
        public string AnyCharacters {
            get => _anyCharacters;
            set => SetProperty(ref _anyCharacters, value);
        }

        /// <summary>
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval {
            get => _scanInterval;
            set => SetProperty(ref _scanInterval, value);
        }

        /// <summary>
        /// 重复条码过滤数量
        /// </summary>
        public int DuplicateBarcodeFilterCount {
            get => _duplicateBarcodeFilterCount;
            set => SetProperty(ref _duplicateBarcodeFilterCount, value);
        }

        /// <summary>
        /// 过滤输出类型
        /// </summary>
        public FilterOutputType FilterOutputType {
            get => _filterOutputType;
            set => SetProperty(ref _filterOutputType, value);
        }

        /// <summary>
        /// 融合超时时间
        /// </summary>
        public int MergeTimeout {
            get => _mergeTimeout;
            set => SetProperty(ref _mergeTimeout, value);
        }

        /// <summary>
        /// 多条码分隔符
        /// </summary>
        public string MultiBarcodeDelimiter {
            get => _multiBarcodeDelimiter;
            set => SetProperty(ref _multiBarcodeDelimiter, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression {
            get => _regularExpression;
            set => SetProperty(ref _regularExpression, value);
        }

        /// <summary>
        /// 测试的条码
        /// </summary>
        public string TestBarcode {
            get => _testBarcode;
            set => SetProperty(ref _testBarcode, value);
        }

        public ICommand MinimumLengthChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand MaximumLengthChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand StartCharacterTypeChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand EndCharacterTypeChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand DisallowedCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand RequiredCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);
        public ICommand AnyCharactersChangedCommand => new DelegateCommand(UpdateRegularExpression);
        public ICommand ScanIntervalChangedCommand => new DelegateCommand(UpdateRegularExpression);

        public ICommand TestCommand => new DelegateCommand(TestDelegate);

        private async void TestDelegate() {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                try {
                    var isMatch = Regex.IsMatch(TestBarcode, RegularExpression);
                    base.MessageQueue.Enqueue(isMatch ? Languages.Language.ResourceManager.GetString("验证通过") ?? string.Empty
                        : Languages.Language.ResourceManager.GetString("验证不通过") ?? string.Empty);
                }
                catch (Exception e) {
                    base.MessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("不是正确的正则表达式") ?? string.Empty);
                }
            });
        }

        public override string Identifier => "BarcodeFilterSettingsDialogHost";
        public override string SettingsName => "BarcodeFilterSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new BarcodeFilterSettingsDto {
                    MinimumLength = MinimumLength,
                    MaximumLength = MaximumLength,
                    StartCharacterType = StartCharacterType,
                    EndCharacterType = EndCharacterType,
                    DisallowedCharacters = DisallowedCharacters,
                    RequiredCharacters = RequiredCharacters,
                    ScanInterval = ScanInterval,
                    RegularExpression = RegularExpression,
                    DuplicateBarcodeFilterCount = DuplicateBarcodeFilterCount,
                    FilterOutputType = FilterOutputType,
                    MergeTimeout = MergeTimeout,
                    MultiBarcodeDelimiter = MultiBarcodeDelimiter,
                    AnyCharacters = AnyCharacters
                })
            });
            base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                Languages.Language.ResourceManager.GetString("Success") :
                Languages.Language.ResourceManager.GetString("Failure"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var settingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(SettingsName) ??
                                      new BarcodeFilterSettingsDto();
                    MinimumLength = settingsDto.MinimumLength;
                    MaximumLength = settingsDto.MaximumLength;
                    StartCharacterType = settingsDto.StartCharacterType;
                    EndCharacterType = settingsDto.EndCharacterType;
                    DisallowedCharacters = settingsDto.DisallowedCharacters;
                    RequiredCharacters = settingsDto.RequiredCharacters;
                    AnyCharacters = settingsDto.AnyCharacters;
                    ScanInterval = settingsDto.ScanInterval;
                    RegularExpression = settingsDto.RegularExpression;
                    DuplicateBarcodeFilterCount = settingsDto.DuplicateBarcodeFilterCount;
                    FilterOutputType = settingsDto.FilterOutputType;
                    MergeTimeout = settingsDto.MergeTimeout;
                    MultiBarcodeDelimiter = settingsDto.MultiBarcodeDelimiter;
                });
            }
        }

        /// <summary>
        /// 改变正则表达
        /// </summary>
        private async void UpdateRegularExpression() {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                var regularChars = new List<string>();
                //不能包含
                if (!string.IsNullOrWhiteSpace(DisallowedCharacters)) {
                    var strings = DisallowedCharacters.Split(";");
                    strings.ForEach(f => {
                        regularChars.Add($"(^(?!.*{f}))");
                    });
                }
                //必须包含
                if (!string.IsNullOrWhiteSpace(RequiredCharacters)) {
                    var strings = RequiredCharacters.Split(";");
                    strings.ForEach(f => {
                        regularChars.Add($"(?=.*{f})");
                    });
                }
                //包含任意
                if (!string.IsNullOrWhiteSpace(AnyCharacters)) {
                    var strings = AnyCharacters.Replace(";", "|");

                    regularChars.Add($"(?=.*(?:{strings}))");
                }
                //开头字符
                switch (StartCharacterType) {
                    case CharacterType.Alphanumeric:
                        regularChars.Add("(?=^([0-9a-zA-Z]).*)");
                        break;

                    case CharacterType.Letter:
                        regularChars.Add("(?=^([a-zA-Z]).*)");
                        break;

                    case CharacterType.Number:
                        regularChars.Add("(?=^([0-9]).*)");
                        break;
                }
                //结尾字符
                switch (EndCharacterType) {
                    case CharacterType.Alphanumeric:
                        regularChars.Add("(?=.*([0-9a-zA-Z])$)");
                        break;

                    case CharacterType.Letter:
                        regularChars.Add("(?=.*([a-zA-Z])$)");
                        break;

                    case CharacterType.Number:
                        regularChars.Add("(?=.*([0-9])$)");
                        break;
                }
                //位数限制
                regularChars.Add($"(^.{{{MinimumLength},{MaximumLength}}}$)");

                RegularExpression = string.Join(string.Empty, regularChars);
            });
        }
    }
}