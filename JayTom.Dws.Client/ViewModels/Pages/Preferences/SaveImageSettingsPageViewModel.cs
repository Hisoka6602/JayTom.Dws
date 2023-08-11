using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using Prism.Commands;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Brush = System.Drawing.Brush;
using JayTom.Dws.PluginInterface.Utils;
using Color = System.Windows.Media.Color;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class SaveImageSettingsPageViewModel : BindableBase {
        private bool _isUseWatermark;
        private string _watermarkText = "测试水印";
        private System.Windows.Media.Color _watermarkColor = Color.FromRgb(System.Drawing.Color.DodgerBlue.R, System.Drawing.Color.DodgerBlue.G, System.Drawing.Color.DodgerBlue.B);
        private int _watermarkFontSize = 10;
        private WatermarkPosition _watermarkPosition = WatermarkPosition.TopLeft;
        private ImageSource? _originalImage = new BitmapImage(new Uri("../../../Image/14.jpg", UriKind.Relative));
        private ImageSource? _imageSource;
        private bool _isSliderMoving;

        public SaveImageSettingsPageViewModel() {
            _imageSource = _originalImage;
        }

        public bool IsSliderMoving {
            get => _isSliderMoving;
            set => SetProperty(ref _isSliderMoving, value);
        }

        public ICommand SliderValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand ColorPickerValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand CheckBoxValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand WatermarkPositionCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        /// <summary>
        /// 是否使用水印
        /// </summary>
        public bool IsUseWatermark {
            get => _isUseWatermark;
            set => SetProperty(ref _isUseWatermark, value);
        }

        /// <summary>
        /// 水印内容
        /// </summary>
        public string WatermarkText {
            get => _watermarkText;
            set => SetProperty(ref _watermarkText, value);
        }

        /// <summary>
        /// 原图
        /// </summary>
        public ImageSource? OriginalImage {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value);
        }

        /// <summary>
        /// 水印颜色
        /// </summary>
        public System.Windows.Media.Color WatermarkColor {
            get => _watermarkColor;
            set => SetProperty(ref _watermarkColor, value);
        }

        /// <summary>
        /// 水印字体大小
        /// </summary>
        public int WatermarkFontSize {
            get => _watermarkFontSize;
            set => SetProperty(ref _watermarkFontSize, value);
        }

        /// <summary>
        /// 水印位置
        /// </summary>
        public WatermarkPosition WatermarkPosition {
            get => _watermarkPosition;
            set => SetProperty(ref _watermarkPosition, value);
        }

        /// <summary>
        /// 预览图片
        /// </summary>
        public ImageSource? ImageSource {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        /// <summary>
        /// 设置水印
        /// </summary>
        private void SetWatermarkToImage() {
            Task.Run(async () => {
                //信号锁
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    if (OriginalImage is not null && IsUseWatermark && !string.IsNullOrEmpty(WatermarkText)) {
                        var image = OriginalImage.ConvertImageSourceToImage();
                        if (image is not null) {
                            using var graphics = Graphics.FromImage(image);
                            using var watermarkFont = new Font("微软雅黑", WatermarkFontSize, FontStyle.Bold);
                            using var watermarkBrush = new SolidBrush(System.Drawing.Color.FromArgb(WatermarkColor.A,
                                WatermarkColor.R, WatermarkColor.G, WatermarkColor.B));

                            float x = 0, y = 0;
                            switch (WatermarkPosition) {
                                case WatermarkPosition.TopLeft:
                                    x = 10;
                                    y = 10;
                                    break;

                                case WatermarkPosition.TopRight:
                                    x = image.Width - graphics.MeasureString(WatermarkText, watermarkFont).Width - 10;
                                    y = 10;
                                    break;

                                case WatermarkPosition.BottomLeft:
                                    x = 10;
                                    y = image.Height - watermarkFont.Height - 10;
                                    break;

                                case WatermarkPosition.BottomRight:
                                    x = image.Width - graphics.MeasureString(WatermarkText, watermarkFont).Width - 10;
                                    y = image.Height - watermarkFont.Height - 10;
                                    break;

                                default:
                                    x = 10;
                                    y = 10;
                                    break;
                            }

                            graphics.DrawString(WatermarkText, watermarkFont, watermarkBrush, x, y);
                            ImageSource = image.ConvertBitmapToBitmapSource();
                        }
                    }
                });
            });
        }
    }

    public enum WatermarkPosition {
        TopLeft = 0,
        BottomLeft = 1,
        TopRight = 2,
        BottomRight = 3
    }
}