using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace JayTom.Dws.Ocr.Yolo {

    public class YoloParser : IDisposable {
        private List<string> _lables = new();

        /// <summary>
        /// 用于取出模型中的version信息
        /// </summary>
        private string _modelVersion = string.Empty;

        /// <summary>
        /// classify = 分类，segment = 分割，detect = 检测，pose = 动作
        /// </summary>
        private TaskType _taskType = TaskType.Pose;

        /// <summary>
        /// 如果是语义分割模型，如 YOLO8 的 output0 输出会变为如 [1, 8400, 116] 或者 [1, 116, 8400]，
        /// 其中有 4 个坐标框，80 个类别权重，32 个 mask 数据，所以在遍历权重时，要减掉这些循环。
        /// </summary>
        private int _semanticSegmentationWidth = 0;

        private int _poseWidth = 0;

        /// <summary>
        /// mask缩放比例
        /// </summary>
        private float _maskScale = 1;

        /// <summary>
        /// 缩放比例
        /// </summary>
        private float _scale = 1;

        /// <summary>
        /// 张量宽度、高度
        /// </summary>
        private int _tensorWidth, _tensorHeight;

        /// <summary>
        /// 模型
        /// </summary>

        private InferenceSession? _session;

        /// <summary>
        /// 模型输入名
        /// </summary>
        private string _inputName = string.Empty;

        /// <summary>
        /// 模型输出名
        /// </summary>
        private string _outputName = string.Empty;

        /// <summary>
        /// 输入张量信息
        /// </summary>
        private int[]? _inputDimensions;

        /// <summary>
        /// 输出张量信息
        /// </summary>

        private int[]? _outDimensions;
        /// <summary>
        /// 缩放比例
        /// </summary>

        private float _maskScaleFactor;

        /// <summary>
        /// 获取模型的基本信息
        /// </summary>
        private Dictionary<string, string>? _modelMetadataCustomMetadataMap;

        /// <summary>
        /// 是否已加载
        /// </summary>
        public bool IsLoaded { get; set; }

        public YoloParser(string modelFilePath) {
            if (File.Exists(modelFilePath)) {
                try {
                    _session = new InferenceSession(modelFilePath);

                    //模型输入名
                    _inputName = _session.InputNames.First();
                    //模型输出名
                    _outputName = _session.OutputNames.First();
                    //输入张量信息
                    _inputDimensions = _session.InputMetadata[_inputName].Dimensions;
                    //输出张量信息
                    _outDimensions = _session.OutputMetadata[_outputName].Dimensions;
                    //缩放比例
                    _maskScaleFactor = 160f / _outDimensions[2];
                    //获取模型的基本信息
                    _modelMetadataCustomMetadataMap = _session.ModelMetadata.CustomMetadataMap;

                    //如果找到标签名，则使用标签名分分割
                    if (_modelMetadataCustomMetadataMap.Keys.Contains("names")) {
                        _lables = SplitLabels(_modelMetadataCustomMetadataMap["names"]);
                    }
                    if (_modelMetadataCustomMetadataMap.Keys.Contains("version")) {
                        // 用于取出模型中的 version 信息
                        _modelVersion = _modelMetadataCustomMetadataMap["version"];
                    }
                    if (_modelMetadataCustomMetadataMap.Keys.Contains("task")) {
                        // classify = 分类，segment = 分割，detect = 检测，pose = 动作
                        var metadata = _modelMetadataCustomMetadataMap["task"];

                        if (metadata == "segment") {
                            // 如果是语义分割模型，如 YOLO8 的 output0 输出会变为如 [1, 8400, 116] 或者 [1, 116, 8400]，
                            // 其中有 4 个坐标框，80 个类别权重，32 个 mask 数据，所以在遍历权重时，要减掉这些循环。
                            _semanticSegmentationWidth = 32;
                            _taskType = TaskType.Segment;
                        }
                        else if (metadata == "pose") {
                            // 动作宽度
                            _poseWidth = 51;
                            _taskType = TaskType.Pose;
                        }
                    }
                    else {
                        // 如果第一个输出张量只有2个维度,说明是分类模型
                        if (_outDimensions.Length == 2) {
                            _taskType = TaskType.Classify;
                        }
                        else if (_outDimensions.Length == 3) {
                            // 如果输出3个维度,且只有一个output,默认为检测模型
                            if (_session.OutputNames.Count == 1) {
                                _taskType = TaskType.Detect;
                            }
                            else if (_session.OutputNames.Count == 2) {
                                // 有的模型基本信息里没有 task 任务种类，比如 yolo5n-seg.onnx 就判断输出名有几个，
                                // 如果是2个输出名，默认也为语义分割模型
                                _semanticSegmentationWidth = 32;
                                _taskType = TaskType.Segment;
                            }
                            else {
                                // throw new Exception("暂不支持的模型");
                            }
                        }
                        else {
                            throw new Exception("暂不支持的模型");
                        }
                    }

                    _tensorWidth = _inputDimensions[3];
                    _tensorHeight = _inputDimensions[2];
                    IsLoaded = true;
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        /// <summary>
        ///   分割标签
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private List<string> SplitLabels(string input) {
            var withoutBrackets = Regex.Replace(input, "[{}]", "");
            var splitArray = withoutBrackets.Split(',');
            var resultArray = splitArray.Select(item => {
                var startIndex = item.IndexOf(':') + 3;
                var endIndex = item.Length - 1;
                return item.Substring(startIndex, endIndex - startIndex);
            }).ToList();

            return resultArray;
        }

        /// <summary>
        /// 执行推理
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="confidenceThreshold"></param>
        /// <param name="rectangleScale"></param>
        /// <param name="iouThreshold"></param>
        /// <param name="globalIoU"></param>
        /// <returns></returns>
        public List<YoloInfo>? Evaluate(Bitmap bitmap, float confidenceThreshold = 0.5f,
            float rectangleScale = 1,
            float iouThreshold = 0.3f,
            bool globalIoU = false) {
            try {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var denseTensorInfo = GetDenseTensorInfo(bitmap, _inputDimensions ?? Array.Empty<int>(), _tensorWidth, _tensorHeight);
                IReadOnlyCollection<NamedOnnxValue> container = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, denseTensorInfo) };
                Tensor<float> output0;
                var filteredDataArray = new List<YoloData>();
                var finalResultData = new List<YoloData>();
                if (_taskType == TaskType.Classify) {
                    output0 = _session?.Run(container).First().AsTensor<float>() ?? new DenseTensor<float>(0);
                    finalResultData = ConfidenceFilter_Classification(output0, confidenceThreshold);
                }
                else if (_taskType is TaskType.Detect or TaskType.Segment) {
                    output0 = _session?.Run(container)?.First()?.AsTensor<float>() ?? new DenseTensor<float>(0);

                    if (_modelVersion.StartsWith("8")) {
                        filteredDataArray = ConfidenceFilter_YOLO8Detection(output0, confidenceThreshold, _semanticSegmentationWidth, _poseWidth);
                    }

                    finalResultData = NMSFilter(filteredDataArray, iouThreshold, globalIoU);
                    /*else if (modelVersion.StartsWith("5")) {
                        filteredDataArray = ConfidenceFilter_YOLO5Detection(output0, ConfidenceThreshold);
                    }
                    else {
                        filteredDataArray = ConfidenceFilter_YOLO6Detection(output0, ConfidenceThreshold);
                    }

                    finalResultData = NMSFilter(filteredDataArray, IoUThreshold, GlobalIoU);*/
                }

                var yoloDatas = RestoreCoordinates(finalResultData, _scale);
                yoloDatas = RestoreCenterCoordinates(yoloDatas);
                stopwatch.Stop();
                return yoloDatas.Where(s => s.BasicData != null).Select(s => new YoloInfo() {
                    Label = _lables?[(int)s.BasicData![5]] ?? string.Empty,
                    Confidence = s.BasicData![4],
                    Rectangle = ExpandRegion(new Rectangle((int)s.BasicData![0],
                         (int)s.BasicData![1],
                         (int)s.BasicData![2],
                         (int)s.BasicData![3]), rectangleScale),
                    InferenceTimeInMilliseconds = (int)stopwatch.Elapsed.TotalMilliseconds
                })?.ToList();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new List<YoloInfo>();
        }

        private List<YoloData> RestoreCenterCoordinates(List<YoloData> dataList) {
            if (dataList.Count > 0 && dataList[0]?.BasicData?.Length > 2) {
                foreach (var t in dataList.Where(t => t.BasicData != null)) {
                    if (t.BasicData == null) continue;
                    t.BasicData[0] = t.BasicData[0] - t.BasicData[2] / 2;
                    t.BasicData[1] = t.BasicData[1] - t.BasicData[3] / 2;
                }
            }
            return dataList;
        }

        private List<YoloData> NMSFilter(List<YoloData> initialFilterArray, float iouThreshold, bool globalIoU) {
            // Sort the initial filter array in descending order based on confidence
            var bubbleSortConfidence = BubbleSortConfidence(initialFilterArray);

            // Initialize the result array
            var nmsFilterArray = new List<YoloData>();

            // Use LinQ to filter elements based on IoU condition
            nmsFilterArray.AddRange(bubbleSortConfidence.Where(element =>
                nmsFilterArray.All(existingElement =>
                    (globalIoU || element?.BasicData?[5] == existingElement?.BasicData?[5]) &&
                    CalculateIntersectionOverUnion(element?.BasicData ?? Array.Empty<float>(), existingElement?.BasicData ?? Array.Empty<float>()) <= iouThreshold)));

            return nmsFilterArray;
        }

        private float CalculateIntersectionOverUnion(IReadOnlyList<float> box1, IReadOnlyList<float> box2) {
            // 恢复传入参数的实际起点坐标和终点坐标，因为传进来的是中心点和宽高
            float[] rect1 = {
                box1[0] - box1[2] / 2,
                box1[1] - box1[3] / 2,
                box1[0] + box1[2] / 2,
                box1[1] + box1[3] / 2
            };

            float[] rect2 = {
                box2[0] - box2[2] / 2,
                box2[1] - box2[3] / 2,
                box2[0] + box2[2] / 2,
                box2[1] + box2[3] / 2
            };

            var intersectionArea = Enumerable.Range(0, 2)
                .Select(i => Math.Max(rect1[i], rect2[i]))
                .Zip(Enumerable.Range(2, 2).Select(i => Math.Min(rect1[i], rect2[i])), (a, b) => b - a)
                .Aggregate((a, b) => a * b);

            var area1 = (rect1[2] - rect1[0]) * (rect1[3] - rect1[1]);
            var area2 = (rect2[2] - rect2[0]) * (rect2[3] - rect2[1]);
            var unionArea = area1 + area2 - intersectionArea;

            // 交集除以并集得到 IoU，除非两个相同的框完全重叠，否则只会返回一个小于 1 的值
            return unionArea == 0 ? 0 : intersectionArea / unionArea;
        }

        private List<YoloData> ConfidenceFilter_Classification(Tensor<float> data, float confidenceThreshold) {
            // Classification model returns output data like {1, 80} representing 80 categories, and the data is the confidence of each category.
            var resultList = new List<YoloData>();

            for (var i = 0; i < data.Dimensions[1]; i++) {
                if (!(data[0, i] >= confidenceThreshold)) continue;
                var temp = new YoloData {
                    BasicData = new float[] { data[0, i], i }
                };
                resultList.Add(temp);
            }

            return BubbleSortConfidence(resultList);
        }

        private DenseTensor<float> GetDenseTensorInfo(Bitmap bitmap, int[] tensorInfo,
       int tensorWidth, int tensorHeight) {
            var imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var stride = imageData.Stride;  // 每一行的字节数
            var scan0 = imageData.Scan0;  // 第一个像素的内存地址

            // 用基本的 float 类型模拟与 DenseTensor 维度相同的数组，并进行写入，如 {3, 640, 640}
            var temporaryData = new float[tensorInfo[1], tensorInfo[2], tensorInfo[3]];
            float scaledImageWidth = imageData.Width;
            float scaledImageHeight = imageData.Height;

            // 科学的缩放方式，而不是裁剪和拉伸
            // 图片宽和高只要有一个比张量的宽高大，就说明要缩放
            if (scaledImageWidth > tensorWidth || scaledImageHeight > tensorHeight) {
                // 图片更大要缩放
                _scale = tensorWidth / scaledImageWidth < tensorHeight / scaledImageHeight ? tensorWidth / scaledImageWidth : tensorHeight / scaledImageHeight;
                scaledImageWidth *= _scale;
                scaledImageHeight *= _scale;
            }

            // 用于定位取原始图像中的 x 坐标或 y 坐标位置
            int xPosition, yPosition;
            var coefficient = 1 / _scale;

            for (var y = 0; y < (int)scaledImageHeight; y++)  // 高
            {
                for (var x = 0; x < (int)scaledImageWidth; x++) {
                    xPosition = (int)(x * coefficient);
                    yPosition = (int)(y * coefficient);

                    var pixel = nint.Add(scan0, yPosition * stride + xPosition * 3);
                    temporaryData[2, y, x] = Marshal.ReadByte(pixel) / 255f;  // B 通道，归一化

                    pixel = nint.Add(pixel, 1);
                    temporaryData[1, y, x] = Marshal.ReadByte(pixel) / 255f;  // G

                    pixel = nint.Add(pixel, 1);
                    temporaryData[0, y, x] = Marshal.ReadByte(pixel) / 255f;  // R
                }
            }

            bitmap.UnlockBits(imageData);
            var flattenedTemporaryData = new float[tensorInfo[1] * tensorInfo[2] * tensorInfo[3]];

            // 将多维数组的所有数据复制到展开的数组中，Length * 4 是每个 float 数据占用 4 个字节
            Buffer.BlockCopy(temporaryData, 0, flattenedTemporaryData, 0, flattenedTemporaryData.Length * 4);

            // 然后将展开的数组创建成一个 DenseTensor 并返回
            return new DenseTensor<float>(flattenedTemporaryData, tensorInfo);
        }

        private List<YoloData> ConfidenceFilter_YOLO8Detection(Tensor<float> data,
       float confidenceThreshold, int semanticSegmentationWidth, int poseWidth) {
            var isSizeMiddle = data.Dimensions[1] < data.Dimensions[2];

            if (isSizeMiddle) {
                var resultList = new ConcurrentBag<YoloData>();

                Parallel.For(0, data.Dimensions[2], i => {
                    var tempConfidence = 0f;
                    var index = -1;

                    for (var j = 0; j < data.Dimensions[1] - 4 - semanticSegmentationWidth - poseWidth; j++) {
                        if (!(data[0, j + 4, i] >= confidenceThreshold)) continue;
                        if (!(tempConfidence < data[0, j + 4, i])) continue;
                        tempConfidence = data[0, j + 4, i];
                        index = j;
                    }

                    if (index == -1) return;
                    var temp = new YoloData {
                        BasicData = new float[] { data[0, 0, i], data[0, 1, i], data[0, 2, i], data[0, 3, i], tempConfidence, index }
                    };
                    resultList.Add(temp);
                });

                return resultList.ToList();
            }
            else {
                var resultList = new List<YoloData>();
                var outputSize = data.Dimensions[2];
                var tempConfidence = 0f;
                var index = -1;
                var data2 = data.ToArray();

                for (var i = 0; i < data2.Length; i += outputSize) {
                    tempConfidence = 0f;
                    index = -1;

                    for (var j = 0; j < outputSize - 4 - semanticSegmentationWidth - poseWidth; j++) {
                        if (!(data2[i + 4 + j] > confidenceThreshold)) continue;
                        if (!(tempConfidence < data2[i + 4 + j])) continue;
                        tempConfidence = data2[i + 4 + j];
                        index = j;
                    }

                    if (index == -1) continue;
                    var temp = new YoloData {
                        BasicData = new float[] { data2[i], data2[i + 1], data2[i + 2], data2[i + 3], tempConfidence, index }
                    };
                    resultList.Add(temp);
                }

                return resultList;
            }
        }

        private List<YoloData> BubbleSortConfidence(List<YoloData> dataArray) {
            var n = dataArray.Count;
            if (n <= 0) return dataArray;
            var confidenceIndex = dataArray[0]?.BasicData?.Length == 2 ? 0 : 4;

            for (var i = 0; i < n - 1; i++) {
                for (var j = 0; j < n - i - 1; j++) {
                    if (dataArray[j]?.BasicData?[confidenceIndex] < dataArray[j + 1]?.BasicData?[confidenceIndex]) {
                        (dataArray[j], dataArray[j + 1]) = (dataArray[j + 1], dataArray[j]);
                    }
                }
            }
            return dataArray;
        }

        private List<YoloData> RestoreCoordinates(List<YoloData> dataList, float scaleFactor) {
            if (dataList.Count > 0) {
                // Check if it is not data returned from a classification model before processing
                if (dataList[0]?.BasicData?.Length > 2) {
                    foreach (var t in dataList.Where(t => t?.BasicData is not null && t.BasicData.Length > 3)) {
                        t.BasicData![0] = t.BasicData![0] / scaleFactor;
                        t.BasicData![1] = t.BasicData![1] / scaleFactor;
                        t.BasicData![2] = t.BasicData![2] / scaleFactor;
                        t.BasicData![3] = t.BasicData![3] / scaleFactor;
                    }
                }

                // If there are poses, restore the scale for them as well
                if (dataList[0].Poses != null) {
                    foreach (var t1 in dataList.SelectMany(t => t.Poses ?? Array.Empty<Pose>())) {
                        t1.X /= scaleFactor;
                        t1.Y /= scaleFactor;
                    }
                }
            }

            return dataList;
        }

        private Rectangle ExpandRegion(Rectangle originalRegion, float rectangleScale = 1) {
            var newWidth = (int)(originalRegion.Width * rectangleScale);
            var newHeight = (int)(originalRegion.Height * rectangleScale);

            var newX = originalRegion.X - (newWidth - originalRegion.Width) / 2;
            var newY = originalRegion.Y - (newHeight - originalRegion.Height) / 2;

            var newRegion = new Rectangle(newX, newY, newWidth, newHeight);

            return newRegion;
        }

        public class YoloData {

            /// <summary>
            /// Used to store basic data: x, y, w, h, confidence, label.
            /// </summary>
            public float[]? BasicData { get; set; }

            /// <summary>
            /// Default to use a pose model with 17 keypoints.
            /// </summary>
            public Pose[]? Poses { get; set; }

            public Mat? MaskData { get; set; }
        }

        public class YoloInfo {

            /// <summary>
            /// 区域
            /// </summary>
            public Rectangle? Rectangle { get; set; }

            /// <summary>
            /// 置信度
            /// </summary>
            public float? Confidence { get; set; }

            /// <summary>
            /// 标签
            /// </summary>
            public string Label { get; set; } = string.Empty;

            /// <summary>
            /// 推理耗时
            /// </summary>
            public int InferenceTimeInMilliseconds { get; set; }
        }

        public class Pose {

            /// <summary>
            /// x-coordinate of the pose point.
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// y-coordinate of the pose point.
            /// </summary>
            public float Y { get; set; }

            /// <summary>
            /// Confidence of the pose point.
            /// When this value is low, it is likely to be outside the bounding box, generally using 0.5 as the threshold.
            /// </summary>
            public float V { get; set; }
        }

        public enum TaskType {

            /// <summary>
            /// 无
            /// </summary>
            None,

            /// <summary>
            /// 分类
            /// </summary>
            Classify,

            /// <summary>
            /// 分割
            /// </summary>
            Segment,

            /// <summary>
            /// 检测
            /// </summary>
            Detect,

            /// <summary>
            /// 动作
            /// </summary>
            Pose
        }

        public void Dispose() {
            _session?.Dispose();
        }
    }
}