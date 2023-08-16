using System;
using ImTools;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using Mono.Unix.Native;
using System.Threading;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class BarcodeFilterSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private int _minimumLength = 10;
        private int _maximumLength = 22;
        private CharacterType _startCharacterType = CharacterType.Alphanumeric;
        private CharacterType _endCharacterType = CharacterType.Number;
        private string _disallowedCharacters = string.Empty;
        private string _requiredCharacters = string.Empty;
        private int _scanInterval = 1000;
        private string _regularExpression = "(?=^([0-9a-zA-Z]).*)(?=.*([0-9])$)(^.{10,22}$)";
        private string _testBarcode = string.Empty;
        private SnackbarMessageQueue _barcodeFilterSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;
        private bool _isLoaded;

        public BarcodeFilterSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
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
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval {
            get => _scanInterval;
            set => SetProperty(ref _scanInterval, value);
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

        public SnackbarMessageQueue BarcodeFilterSettingsMessageQueue {
            get => _barcodeFilterSettingsMessageQueue;
            set => SetProperty(ref _barcodeFilterSettingsMessageQueue, value);
        }

        public ICommand MinimumLengthChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand MaximumLengthChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand StartCharacterTypeChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand EndCharacterTypeChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand DisallowedCharactersChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand RequiredCharactersChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand ScanIntervalChangedCommand {
            get => new DelegateCommand(UpdateRegularExpression);
        }

        public ICommand TestCommand {
            get => new DelegateCommand(TestDelegate);
        }

        private async void TestDelegate() {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                try {
                    var isMatch = Regex.IsMatch(TestBarcode, RegularExpression);
                    BarcodeFilterSettingsMessageQueue.Enqueue(isMatch ? "验证通过" : "验证不通过!");
                }
                catch (Exception e) {
                    BarcodeFilterSettingsMessageQueue.Enqueue("不是正确的正则表达式!");
                }
            });
        }

        public ICommand SaveSettingsCommand {
            get => new DelegateCommand(SaveSettingsDelegate);
        }

        private async void SaveSettingsDelegate() {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    // var loadingDialog = new LoadingDialog();
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "BarcodeFilterSettings",
                        Value = JsonConvert.SerializeObject(new BarcodeFilterSettingsDto {
                            MinimumLength = MinimumLength,
                            MaximumLength = MaximumLength,
                            StartCharacterType = StartCharacterType,
                            EndCharacterType = EndCharacterType,
                            DisallowedCharacters = DisallowedCharacters,
                            RequiredCharacters = RequiredCharacters,
                            ScanInterval = ScanInterval,
                            RegularExpression = RegularExpression
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "BarcodeFilterSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    BarcodeFilterSettingsMessageQueue.Enqueue($"保存{(insertOrUpdate ? "成功" : "失败")}");
                });
            }
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("BarcodeFilterSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var settingsDto = JsonConvert.DeserializeObject<BarcodeFilterSettingsDto>(configInfoModel.Value);
                            if (settingsDto is not null) {
                                MinimumLength = settingsDto.MinimumLength;
                                MaximumLength = settingsDto.MaximumLength;
                                StartCharacterType = settingsDto.StartCharacterType;

                                EndCharacterType = settingsDto.EndCharacterType;
                                DisallowedCharacters = settingsDto.DisallowedCharacters;
                                RequiredCharacters = settingsDto.RequiredCharacters;

                                ScanInterval = settingsDto.ScanInterval;
                                RegularExpression = settingsDto.RegularExpression;
                            }
                        }
                        catch (Exception e) {
                            BarcodeFilterSettingsMessageQueue.Enqueue($"加载设置失败:{e.Message}");
                        }
                    }
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