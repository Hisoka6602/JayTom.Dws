using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.ImageSettingModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class ResultOutputSettingsPageViewModel : BindableBase {

        private ObservableCollection<ItemBaseTemplateModel> _outputItems = new()
        {
            new ItemBaseTemplateModel()
            {
                Id = 0,
                Content = "{BarCode}",
                Type = 1,
                ApplicationType = ItemApplicationType.Watermark
            },
            new ItemBaseTemplateModel()
            {
                Id = 1,
                Content = "{TimestampedGuid}",
                Type = 1,
                ApplicationType = ItemApplicationType.Watermark
            },
            new ItemBaseTemplateModel()
            {
                Id = 2,
                Content = "",
                Type = 0,
                ApplicationType = ItemApplicationType.Watermark
            },
        };

        private bool _isUseTcpOutput;
        private bool _isUseHttpOutput;
        private bool _isUseSerialOutput;
        private bool _isUseAudioOutput;
        private bool _isUseLocationOutput;
        private UploadSettingsInfo _uploadSettingsInfo = new();
        private TcpSettingsInfo _tcpSettingsInfo = new();
        private HttpUploadSettingsInfo _httpUploadSettingsInfo = new();
        private LocationOutputSettingsInfo _locationOutputSettingsInfo = new();

        /// <summary>
        /// 数据模板
        /// </summary>
        public ObservableCollection<ItemBaseTemplateModel> OutputItems {
            get => _outputItems;
            set => SetProperty(ref _outputItems, value);
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.Watermark) {
                    OutputItems.Remove(model);
                    foreach (var item in OutputItems) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            OutputItems.LastOrDefault() != item) {
                            OutputItems.Remove(item);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 添加水印
        /// </summary>
        public ICommand AddOutputItemCommand {
            get => new DelegateCommand<string>(AddOutputItemDelegate);
        }

        private async void AddOutputItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                var count = OutputItems.Count;
                OutputItems.Insert(count - 1, new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.Watermark
                });
                var model = OutputItems?.LastOrDefault();
                if (model?.Type != 0) {
                    OutputItems?.Add(new ItemBaseTemplateModel() {
                        Content = string.Empty,
                        Id = OutputItems.Count,
                        ApplicationType = ItemApplicationType.Watermark
                    });
                }
            });
        }

        /// <summary>
        /// 是否使用Tcp输出
        /// </summary>
        public bool IsUseTcpOutput {
            get => _isUseTcpOutput;
            set => SetProperty(ref _isUseTcpOutput, value);
        }

        /// <summary>
        /// 是否使用Http输出
        /// </summary>
        public bool IsUseHttpOutput {
            get => _isUseHttpOutput;
            set => SetProperty(ref _isUseHttpOutput, value);
        }

        /// <summary>
        /// 是否使用串口输出
        /// </summary>
        public bool IsUseSerialOutput {
            get => _isUseSerialOutput;
            set => SetProperty(ref _isUseSerialOutput, value);
        }

        /// <summary>
        /// 是否使用音频输出
        /// </summary>
        public bool IsUseAudioOutput {
            get => _isUseAudioOutput;
            set => SetProperty(ref _isUseAudioOutput, value);
        }

        /// <summary>
        /// 是否使用位置输出
        /// </summary>
        public bool IsUseLocationOutput {
            get => _isUseLocationOutput;
            set => SetProperty(ref _isUseLocationOutput, value);
        }

        /// <summary>
        /// 上传设置
        /// </summary>
        public UploadSettingsInfo UploadSettingsInfo {
            get => _uploadSettingsInfo;
            set => SetProperty(ref _uploadSettingsInfo, value);
        }
        /// <summary>
        /// Tcp输出设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }
        /// <summary>
        /// Http输出设置
        /// </summary>
        public HttpUploadSettingsInfo HttpUploadSettingsInfo {
            get => _httpUploadSettingsInfo;
            set => SetProperty(ref _httpUploadSettingsInfo, value);
        }
        /// <summary>
        /// 位置输出
        /// </summary>
        public LocationOutputSettingsInfo LocationOutputSettingsInfo {
            get => _locationOutputSettingsInfo;
            set => SetProperty(ref _locationOutputSettingsInfo, value);
        }
    }
}