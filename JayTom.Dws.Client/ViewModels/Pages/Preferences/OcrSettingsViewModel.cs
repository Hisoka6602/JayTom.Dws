using JayTom.Dws.Application.Configuration;
using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using JayTom.Dws.Ocr;
using System.Drawing;
using Newtonsoft.Json;
using Microsoft.Win32;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Pen = System.Drawing.Pen;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class OcrSettingsViewModel : SettingsPageTemplateViewModel
    {
        private readonly IOcr _ocr;
        private readonly IDeviceService _deviceService;
        private OcrSettingsInfoModel _ocrSettingsInfo = new();
        private ObservableCollection<string> _modelFiles = new();
        private string _selectModelFile = string.Empty;
        private string _loadImagePath = string.Empty;
        private ImageSource? _originalImage;
        private ImageSource? _imageSource;
        private Bitmap? _cropImage = null;

        public OcrSettingsViewModel(ISettingsStore settingsStore, IOcr ocr,
            IDeviceService deviceService, JayTom.Dws.Application.Messaging.IEventBus eventBus) : base(settingsStore, eventBus)
        {
            _ocr = ocr;
            _deviceService = deviceService;
        }

        public OcrSettingsInfoModel OcrSettingsInfo
        {
            get => _ocrSettingsInfo;
            set => SetProperty(ref _ocrSettingsInfo, value);
        }

        /// <summary>
        /// 算法Items
        /// </summary>
        public ObservableCollection<string> ModelFiles
        {
            get => _modelFiles;
            set => SetProperty(ref _modelFiles, value);
        }

        /// <summary>
        /// 选择的算法
        /// </summary>
        public string SelectModelFile
        {
            get => _selectModelFile;
            set => SetProperty(ref _selectModelFile, value);
        }

        /// <summary>
        /// 测试图片路径
        /// </summary>
        public string LoadImagePath
        {
            get => _loadImagePath;
            set => SetProperty(ref _loadImagePath, value);
        }

        /// <summary>
        /// 原图
        /// </summary>
        public ImageSource? OriginalImage
        {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value);
        }

        /// <summary>
        /// 预览图片
        /// </summary>
        public ImageSource? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        /// <summary>
        /// 浏览目录
        /// </summary>
        public ICommand OpenFolderCommand
        {
            get => new DelegateCommand<object>(OpenFolderDelegate);
        }

        private void OpenFolderDelegate(object obj)
        {
            var folderBrowserDialog = new FolderBrowserDialog()
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                OcrSettingsInfo.CropImagePath = folderBrowserDialog.SelectedPath;
            }
        }

        public ICommand LoadImageCommand => new DelegateCommand<object>(LoadImageDelegate);

        private async void LoadImageDelegate(object obj)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Title = Languages.Language.ResourceManager.GetString("请选择需要打开的图片") ?? string.Empty,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Filter =
                    $"{Languages.Language.ResourceManager.GetString("位图文件") ?? string.Empty} ( *.jpg)| *.jpg",
                DefaultExt = ".jpg",
                RestoreDirectory = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                await UiThread.Dispatcher.InvokeAsync(() =>
                {
                    OriginalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    ImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                    LoadImagePath = openFileDialog.FileName;
                    RefreshDelegate(obj);
                });
            }
        }

        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

        private async void RefreshDelegate(object obj)
        {
            //判断图片不为空
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                _cropImage = null;
                if (OriginalImage is not null)
                {
                    var fullName = Directory.GetFiles($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels")
                        ?.Select(s => new FileInfo(s))?.FirstOrDefault(f => f.Name.Equals(SelectModelFile))?.FullName ?? string.Empty;
                    var image = OriginalImage.ConvertImageSourceToImage();

                    if (image is not null)
                    {
                        var ocrTemporarilyResult = _ocr.ParseOcrTemporarilyResult(
                            OcrBitmapAdapter.Encode((Bitmap)image),
                            fullName, OcrSettingsInfo.ConfidenceThreshold, OcrSettingsInfo.RectangleScale);
                        var inPixels = CalculateLineWidthMultiplier(image.Width, image.Height) * 10;
                        Bitmap? rectangleOnImage = null;
                        if (ocrTemporarilyResult is not null)
                        {
                            _cropImage?.Dispose();
                            _cropImage = ocrTemporarilyResult.CropImage is null
                                ? null
                                : OcrBitmapAdapter.Decode(ocrTemporarilyResult.CropImage);
                            if (ocrTemporarilyResult.CropRectangle is not null)
                            {
                                //先画出区域
                                rectangleOnImage = DrawRectangleOnImage(image, ocrTemporarilyResult.CropRectangle
                                                                               ?? new Rectangle(0, 0, 0, 0),
                                   Color.Crimson, (int)inPixels);
                            }

                            if (ocrTemporarilyResult.IsSuccess && rectangleOnImage is not null)
                            {
                                var drawIndicator = DrawIndicator(rectangleOnImage, rectangleOnImage.Size,
                                    ocrTemporarilyResult, (int)inPixels);
                                ImageSource = drawIndicator.ConvertBitmapToBitmapSource();
                            }
                        }
                        else
                        {
                            ImageSource = OriginalImage;
                        }
                    }
                }
            });
        }

        public ICommand SaveCropImageCommand => new DelegateCommand<object>(SaveCropImageDelegate);

        private async void SaveCropImageDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                if (_cropImage is null)
                {
                    base.MessageQueue.Enqueue("未获取到截图!");
                    return;
                }
                else
                {
                    var saveFileDialog = new SaveFileDialog()
                    {
                        Title = "保存图片",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        Filter =
                            $"{Languages.Language.ResourceManager.GetString("位图文件") ?? string.Empty} ( *.jpg)| *.jpg",
                        DefaultExt = ".jpg",
                        RestoreDirectory = true,
                    };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        _cropImage.Save(saveFileDialog.FileName);
                    }
                }
            });
        }

        //画出矩形区域
        public Bitmap DrawRectangleOnImage(Image image, Rectangle drawArea, Color color, int thickness)
        {
            var markedImage = new Bitmap(image);
            using var graphics = Graphics.FromImage(markedImage);
            using var pen = new Pen(color, (int)(thickness / 5));
            graphics.DrawRectangle(pen, drawArea);

            return markedImage;
        }

        public Bitmap DrawIndicator(Bitmap thumbnail, Size originalSize,
           OcrResult result, int thickness)
        {
            var sortedAreas = new List<List<decimal>>()
            {
                result.BarcodeArea ?? new List<decimal>(),
                result.RecipientAddressArea ?? new List<decimal>(),
                result.ThreeSegmentArea ?? new List<decimal>(),
                result.SenderAddressArea ?? new List<decimal>()
            };

            var yOffset = 30; // 初始偏移量
            sortedAreas.Sort((a, b) => a[1].CompareTo(b[1])); // 根据Y轴值进行排序
            using var g = Graphics.FromImage(thumbnail);
            foreach (var area in sortedAreas.Where(area => !(area[1] <= 0) && !string.IsNullOrEmpty(GetTextForArea(result, area))))
            {
                // 绘制指示器和文本
                DrawIndicatorForArea(g, thumbnail, originalSize, area, GetTextForArea(result, area), GetColorForArea(result, area), yOffset, thickness);

                yOffset += 40 * (thickness / 6); // 每个指示器之间的间隔为40
            }
            return thumbnail;
        }

        private Color GetColorForArea(OcrResult result, List<decimal> area)
        {
            if (area == result.BarcodeArea)
            {
                return Color.LawnGreen;
            }
            else if (area == result.RecipientAddressArea)
            {
                return Color.Orange;
            }
            else if (area == result.ThreeSegmentArea)
            {
                return Color.DodgerBlue;
            }
            else if (area == result.SenderAddressArea)
            {
                return Color.OrangeRed;
            }

            return Color.Black; // 默认颜色为黑色
        }

        private string GetTextForArea(OcrResult result, List<decimal> area)
        {
            if (area == result.BarcodeArea)
            {
                return result.BarCode;
            }
            else if (area == result.RecipientAddressArea)
            {
                return result.RecipientAddress;
            }
            else if (area == result.ThreeSegmentArea)
            {
                return result.ThreeSegmentCode;
            }
            else if (area == result.SenderAddressArea)
            {
                return result.SenderAddress;
            }

            return string.Empty;
        }

        private void DrawIndicatorForArea(Graphics g, Image thumbnail, Size originalSize, List<decimal> areaPoints, string text, Color color, int yOffset, int lineWidth)
        {
            try
            {
                var imageWidth = originalSize.Width > 0 ? originalSize.Width : 1;
                var imageHeight = originalSize.Height > 0 ? originalSize.Height : 1;

                var convertPoints = ConvertPoint(areaPoints);
                var points = new Point[4];
                for (var i = 0; i < convertPoints.Count; i++)
                {
                    points[i].X = (int)(convertPoints[i].X * ((decimal)thumbnail.Size.Width / imageWidth));
                    points[i].Y = (int)(convertPoints[i].Y * ((decimal)thumbnail.Size.Height / imageHeight));
                }

                g.DrawPolygon(new Pen(color, (int)(lineWidth / 6)), points);

                var font = new Font("Arial", (int)(lineWidth * 1.2), FontStyle.Bold);
                var brush = new SolidBrush(color);

                // 截断文本
                if (text.Length >= 20)
                {
                    text = text[..18] + "...";
                }

                //g.DrawString(text, font, brush, 3, yOffset);
                var textWidth = (int)g.MeasureString(text, font).Width;
                var textHeight = (int)g.MeasureString(text, font).Height;

                var lineY = textHeight + yOffset + 3;

                // 判断points[0]坐标在缩略图的左边还是右边
                var isLeftSide = (points[0].X) < thumbnail.Size.Width / 2;

                // 根据判断结果调整绘制位置
                if (isLeftSide) // 如果在左边，靠右绘制
                {
                    var rightMargin = 210;
                    g.DrawString(text, font, brush, thumbnail.Width - textWidth - rightMargin, yOffset);
                    g.DrawLine(new Pen(color, (int)(lineWidth / 6)), thumbnail.Width - rightMargin, lineY, thumbnail.Width - textWidth - rightMargin, lineY);
                    g.DrawLine(new Pen(color, (int)(lineWidth / 6)), thumbnail.Width - textWidth - rightMargin, lineY, points[0].X, points[0].Y);
                }
                else // 如果在右边，靠左绘制
                {
                    g.DrawString(text, font, brush, 3, yOffset);
                    g.DrawLine(new Pen(color, (int)(lineWidth / 6)), 3, lineY, textWidth + 3, lineY);
                    g.DrawLine(new Pen(color, (int)(lineWidth / 6)), textWidth + 3, lineY, points[0].X, points[0].Y);
                }
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        private List<Point> ConvertPoint(List<decimal>? coord)
        {
            var points = new List<Point>();
            if (coord?.Count == 8)
            {
                points = [.. Enumerable.Range(0, coord.Count / 2).Select(i => new Point((int)coord[i * 2], (int)coord[i * 2 + 1]))];

                return SortPointsInCounterClockwiseOrder(points);
            }

            return points;
        }

        private List<Point> SortPointsInCounterClockwiseOrder(List<Point> points)
        {
            // 计算多边形的中心点
            var center = new Point(points.Sum(p => p.X) / points.Count, points.Sum(p => p.Y) / points.Count);

            // 根据相对于中心点的极角排序点
            points.Sort((p1, p2) =>
            {
                var angle1 = Math.Atan2(p1.Y - center.Y, p1.X - center.X);
                var angle2 = Math.Atan2(p2.Y - center.Y, p2.X - center.X);
                return angle1.CompareTo(angle2);
            });

            return points;
        }

        public decimal CalculateLineWidthMultiplier(int imageWidth, int imageHeight, int desiredLineWidthInPixels = 1)
        {
            var widthMultiplier = (decimal)imageWidth / 800; // 计算宽度倍数
            var heightMultiplier = (decimal)imageHeight / 600; // 计算高度倍数

            // 取宽度和高度倍数的最大值作为结果
            var lineWidthMultiplier = Math.Max(widthMultiplier, heightMultiplier);
            return lineWidthMultiplier;
        }

        public override string Identifier => "OcrSettingsDialogHost";
        public override string SettingsName => "OcrSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            if (_deviceService.RunningStatus)
            {
                IsSavingInProgress = false;
                base.MessageQueue.Enqueue($"设备工作中,无法设置");
                return false;
            }
            //即时设置Ocr文件
            var dictionary = new Dictionary<string, object>()
            {
                {"three_segment_code", OcrSettingsInfo.IsThreeSegmentCode},
                {"recipient_name", OcrSettingsInfo.IsShowReceiverInfo},
                {"recipient_phone", OcrSettingsInfo.IsShowReceiverInfo},
                {"recipient_addr", OcrSettingsInfo.IsShowReceiverInfo},
                {"sender_name", OcrSettingsInfo.IsShowSenderInfo},
                {"sender_phone", OcrSettingsInfo.IsShowSenderInfo},
                {"sender_addr", OcrSettingsInfo.IsShowSenderInfo},
            };
            await _ocr.SetOcrParameters(dictionary);
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new OcrSettingsDto()
                {
                    IsThreeSegmentCode = OcrSettingsInfo.IsThreeSegmentCode,
                    IsShowReceiverInfo = OcrSettingsInfo.IsShowReceiverInfo,
                    IsShowRecognitionTime = OcrSettingsInfo.IsShowRecognitionTime,
                    IsShowSenderInfo = OcrSettingsInfo.IsShowSenderInfo,
                    IsUseOcr = OcrSettingsInfo.IsUseOcr,
                    RecognitionTimeout = OcrSettingsInfo.RecognitionTimeout,
                    CropImagePath = OcrSettingsInfo.CropImagePath,
                    ConfidenceThreshold = OcrSettingsInfo.ConfidenceThreshold,
                    IsSaveCropImage = OcrSettingsInfo.IsSaveCropImage,
                    ModelFilePath = SelectModelFile,
                    RectangleScale = OcrSettingsInfo.RectangleScale,
                    IsSecondConfirmationEnabled = OcrSettingsInfo.IsSecondConfirmationEnabled,
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj)
        {
            ModelFiles.Clear();
            var modelNames = Directory.GetFiles($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels")
                ?.Select(s => new FileInfo(s))?.Where(w => w.Extension.Contains("onnx"))
                ?.Select(s1 => s1.Name)?.ToList();
            ModelFiles.AddRange(modelNames);
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var ocrSettingsDto = await _settingsStore.GetAsync<OcrSettingsDto>(SettingsName);

                if (ocrSettingsDto is not null)
                {
                    OcrSettingsInfo = new OcrSettingsInfoModel()
                    {
                        IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                        IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime,
                        IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                        IsUseOcr = ocrSettingsDto.IsUseOcr,
                        IsThreeSegmentCode = ocrSettingsDto.IsThreeSegmentCode,
                        RecognitionTimeout = ocrSettingsDto.RecognitionTimeout,
                        ConfidenceThreshold = ocrSettingsDto.ConfidenceThreshold,
                        CropImagePath = ocrSettingsDto.CropImagePath,
                        IsSaveCropImage = ocrSettingsDto.IsSaveCropImage,
                        ModelFilePath = ocrSettingsDto.ModelFilePath,
                        RectangleScale = ocrSettingsDto.RectangleScale,
                        IsSecondConfirmationEnabled = ocrSettingsDto.IsSecondConfirmationEnabled
                    };
                    SelectModelFile = ocrSettingsDto.ModelFilePath;
                }
                //读取算法文件夹文件
                if (!Directory.Exists($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels"))
                {
                    Directory.CreateDirectory($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels");
                }
            });
        }
    }
}
