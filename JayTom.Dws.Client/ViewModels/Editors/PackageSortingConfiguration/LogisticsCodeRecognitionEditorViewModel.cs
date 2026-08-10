using System;
using ImTools;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows.Forms;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration
{

    public class LogisticsCodeRecognitionEditorViewModel : BindableBase
    {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private LogisticsCodeRecognitionItemInfoModel _logisticsCodeRecognitionItemInfo = new();

        private ObservableCollection<LogisticsRegexItemInfoModel> _logisticsRegexItems = new();

        private int? _minimumLength;
        private int? _maximumLength;
        private CharacterType? _characterType;
        private string? _disallowedCharacters;
        private string? _requiredCharacters;
        private string? _startCharacterType;
        private string? _endCharacterType;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectPackageExitDefinitionInfo = new();
        private bool _isOk;
        private string _soundFilePath = string.Empty;
        private string _exceptionContent = string.Empty;

        public LogisticsCodeRecognitionEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository)
        {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 物流信息
        /// </summary>
        public LogisticsCodeRecognitionItemInfoModel LogisticsCodeRecognitionItemInfo
        {
            get => _logisticsCodeRecognitionItemInfo;
            set => SetProperty(ref _logisticsCodeRecognitionItemInfo, value);
        }

        /// <summary>
        /// 声音文件位置
        /// </summary>
        public string SoundFilePath
        {
            get => _soundFilePath;
            set => SetProperty(ref _soundFilePath, value);
        }

        /// <summary>
        /// 正则列表
        /// </summary>
        public ObservableCollection<LogisticsRegexItemInfoModel> LogisticsRegexItems
        {
            get => _logisticsRegexItems;
            set => SetProperty(ref _logisticsRegexItems, value);
        }

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int? MinimumLength
        {
            get => _minimumLength;
            set => SetProperty(ref _minimumLength, value);
        }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int? MaximumLength
        {
            get => _maximumLength;
            set => SetProperty(ref _maximumLength, value);
        }

        /// <summary>
        /// 字符限制
        /// </summary>
        public CharacterType? CharacterType
        {
            get => _characterType;
            set => SetProperty(ref _characterType, value);
        }

        /// <summary>
        /// 不能包含的字符
        /// </summary>
        public string? DisallowedCharacters
        {
            get => _disallowedCharacters;
            set => SetProperty(ref _disallowedCharacters, value);
        }

        /// <summary>
        /// 必须包含的字符
        /// </summary>
        public string? RequiredCharacters
        {
            get => _requiredCharacters;
            set => SetProperty(ref _requiredCharacters, value);
        }

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public string? StartCharacter
        {
            get => _startCharacterType;
            set => SetProperty(ref _startCharacterType, value);
        }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public string? EndCharacter
        {
            get => _endCharacterType;
            set => SetProperty(ref _endCharacterType, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent
        {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        public ICommand DeleteRegexCommand
        {
            get => new DelegateCommand<LogisticsRegexItemInfoModel>(DeleteRegexDelegate);
        }

        private async void DeleteRegexDelegate(LogisticsRegexItemInfoModel obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LogisticsRegexItems.Remove(obj);
                //调整Num
                if (LogisticsRegexItems?.Any() == true)
                {
                    for (int i = 0; i < LogisticsRegexItems.Count; i++)
                    {
                        LogisticsRegexItems[i].Num = i + 1;
                    }
                }
            });
        }

        public ICommand LoadImageCommand
        {
            get => new DelegateCommand<object>(LoadImageDelegate);
        }

        private async void LoadImageDelegate(object obj)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = @"*.PNG|*.PNG|*.Icon|*.Ico|*.BMP|*.Bmp|*.JPG|*.Jpg",
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "请选择图像文件",
                RestoreDirectory = true,
            };
            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        LogisticsCodeRecognitionItemInfo.Icon = CreateBitmapImage(new Uri(openFileDialog.FileName), 30, 30);
                        LogisticsCodeRecognitionItemInfo.IconName = new FileInfo(openFileDialog.FileName).Name;
                    });
                }
            }
        }

        public ICommand LoadSoundCommand
        {
            get => new DelegateCommand<object>(LoadSoundDelegate);
        }

        private async void LoadSoundDelegate(object obj)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = $"{Languages.Language.ResourceManager.GetString("声音文件") ?? string.Empty}|*.wav;*.mp3",
                Title = Languages.Language.ResourceManager.GetString("请选择声音文件"),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    SoundFilePath = new FileInfo(openFileDialog.FileName).Name;
                    LogisticsCodeRecognitionItemInfo.SoundBytes = await File.ReadAllBytesAsync(openFileDialog.FileName);
                    LogisticsCodeRecognitionItemInfo.SoundName = new FileInfo(openFileDialog.FileName).Name;
                });
            }
        }

        public ICommand SaveRuleCommand
        {
            get => new DelegateCommand<object>(SaveRuleDelegate);
        }

        private async void SaveRuleDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var regularChars = new List<string>();
                //不能包含
                if (!string.IsNullOrWhiteSpace(DisallowedCharacters))
                {
                    var strings = DisallowedCharacters.Split(";");
                    strings.ForEach(f =>
                    {
                        regularChars.Add($"(^(?!.*{f}))");
                    });
                }
                //必须包含
                if (!string.IsNullOrWhiteSpace(RequiredCharacters))
                {
                    var strings = RequiredCharacters.Split(";");
                    strings.ForEach(f =>
                    {
                        regularChars.Add($"(?=.*{f})");
                    });
                }
                //指定开头
                if (!string.IsNullOrWhiteSpace(StartCharacter))
                {
                    var replace = StartCharacter.Replace(";", "|");
                    regularChars.Add($"(?=^({replace}).*)");
                }

                //指定结尾
                if (!string.IsNullOrWhiteSpace(EndCharacter))
                {
                    var replace = EndCharacter.Replace(";", "|");
                    regularChars.Add($"(?=.*({replace})$)");
                }
                //字符限制
                if (CharacterType is not null)
                {
                    switch (CharacterType)
                    {
                        case Domain.Dto.CharacterType.Alphanumeric:
                            regularChars.Add("(?=[0-9a-zA-Z]+$)");
                            break;

                        case Domain.Dto.CharacterType.Letter:
                            regularChars.Add("(?=\\d+$)");
                            break;

                        case Domain.Dto.CharacterType.Number:
                            regularChars.Add("(?=[a-zA-Z]+$)");
                            break;
                    }
                }
                //位数限制
                if (MinimumLength is not null && MaximumLength is not null)
                {
                    regularChars.Add($"(^.{{{MinimumLength},{MaximumLength}}}$)");
                }

                var join = string.Join(string.Empty, regularChars);
                if (!LogisticsRegexItems.Any(a => a.RegexPattern.Equals(join)))
                {
                    LogisticsRegexItems.Add(new LogisticsRegexItemInfoModel()
                    {
                        CreateTime = DateTime.Now,
                        ModifyTime = DateTime.Now,
                        Num = LogisticsRegexItems.Count + 1,
                        RegexPattern = join
                    });
                }
            });
        }

        public ICommand ClearConditionsCommand
        {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private async void ClearConditionsDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DisallowedCharacters =
                    RequiredCharacters =
                        StartCharacter =
                            EndCharacter = null;
                CharacterType = null;
                MinimumLength =
                    MaximumLength = null;
            });
        }

        public ICommand SaveCommand
        {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate()
        {
            //规则需要同步到表[使用同步:多删少增]
            try
            {
                IsOk = true;

                Pitcher.Throw.ArgumentNull.WhenNull(LogisticsCodeRecognitionItemInfo, nameof(LogisticsCodeRecognitionItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(LogisticsCodeRecognitionItemInfo.LogisticsCode, "LogisticsCode");
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(LogisticsCodeRecognitionItemInfo.LogisticsName, "LogisticsName");
            }
            catch (Exception e)
            {
                IsOk = false;
                ExceptionContent = e.Message;
            }

            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelDelegate);
        }

        private void CancelDelegate()
        {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj)
        {
        }

        public BitmapImage CreateBitmapImage(Uri uri, int width, int height)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.DecodePixelHeight = height;
                image.DecodePixelWidth = width;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}