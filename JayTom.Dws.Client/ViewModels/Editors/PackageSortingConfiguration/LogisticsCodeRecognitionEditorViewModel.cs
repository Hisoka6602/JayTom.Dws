using System;
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
using JayTom.Dws.Client.Models.PackageSorting;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration {

    public class LogisticsCodeRecognitionEditorViewModel : BindableBase {
        private string _identifier = string.Empty;
        private LogisticsCodeRecognitionItemInfoModel _logisticsCodeRecognitionItemInfo = new();

        private ObservableCollection<LogisticsRegexItemInfoModel> _logisticsRegexItems = new()
        {
            new LogisticsRegexItemInfoModel()
            {
                CreateTime = DateTime.Now,
                LogisticsId = 1,
                ModifyTime = DateTime.Now,
                Num = 1,
                RegexPattern = "这些命名尽量简明扼要地描述了每个命令的功能，并遵循了常见的命名约定。请根据你自己的实际需求和上下文进行适当调整，以确保命令名称的准确性和易读性。",
                Remarks = "备注"
            }
        };

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

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 物流信息
        /// </summary>
        public LogisticsCodeRecognitionItemInfoModel LogisticsCodeRecognitionItemInfo {
            get => _logisticsCodeRecognitionItemInfo;
            set => SetProperty(ref _logisticsCodeRecognitionItemInfo, value);
        }

        /// <summary>
        /// 声音文件位置
        /// </summary>
        public string SoundFilePath {
            get => _soundFilePath;
            set => SetProperty(ref _soundFilePath, value);
        }

        /// <summary>
        /// 正则列表
        /// </summary>
        public ObservableCollection<LogisticsRegexItemInfoModel> LogisticsRegexItems {
            get => _logisticsRegexItems;
            set => SetProperty(ref _logisticsRegexItems, value);
        }

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int? MinimumLength {
            get => _minimumLength;
            set => SetProperty(ref _minimumLength, value);
        }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int? MaximumLength {
            get => _maximumLength;
            set => SetProperty(ref _maximumLength, value);
        }

        /// <summary>
        /// 字符限制
        /// </summary>
        public CharacterType? CharacterType {
            get => _characterType;
            set => SetProperty(ref _characterType, value);
        }

        /// <summary>
        /// 不能包含的字符
        /// </summary>
        public string? DisallowedCharacters {
            get => _disallowedCharacters;
            set => SetProperty(ref _disallowedCharacters, value);
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
        public string? StartCharacterType {
            get => _startCharacterType;
            set => SetProperty(ref _startCharacterType, value);
        }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public string? EndCharacterType {
            get => _endCharacterType;
            set => SetProperty(ref _endCharacterType, value);
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

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public ICommand DeleteRegexCommand {
            get => new DelegateCommand<LogisticsRegexItemInfoModel>(DeleteRegexDelegate);
        }

        private async void DeleteRegexDelegate(LogisticsRegexItemInfoModel obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LogisticsRegexItems.Remove(obj);
            });
        }

        public ICommand LoadImageCommand {
            get => new DelegateCommand<object>(LoadImageDelegate);
        }

        private async void LoadImageDelegate(object obj) {
            Console.WriteLine(1);
        }

        public ICommand LoadSoundCommand {
            get => new DelegateCommand<object>(LoadSoundDelegate);
        }

        private void LoadSoundDelegate(object obj) {
            Console.WriteLine(1);
        }

        public ICommand SaveRuleCommand {
            get => new DelegateCommand<object>(SaveRuleDelegate);
        }

        private void SaveRuleDelegate(object obj) {
            //整理规则
            //添加到列表
            Console.WriteLine(1);
        }

        public ICommand ClearConditionsCommand {
            get => new DelegateCommand<object>(ClearConditionsDelegate);
        }

        private void ClearConditionsDelegate(object obj) {
            Console.WriteLine(1);
        }

        public ICommand SaveCommand {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate() {
            //规则需要同步到表[使用同步:多删少增]
            Console.WriteLine(1);
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

        private void LoadedDelegate(object obj) {
        }

        //删除规则

        //加载图片
        //加载声音
        //保存规则
        //清空条件
        //保存
        //取消
        //页面加载
    }
}