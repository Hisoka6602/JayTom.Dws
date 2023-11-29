using SkiaSharp;
using OpenCvSharp;
using Microsoft.ML;
using System.Drawing;
using OnnxTest.YoloParser;
using System.Drawing.Imaging;
using OnnxTest.DataStructures;
using Microsoft.ML.OnnxRuntime;
using System.Drawing.Drawing2D;
using Microsoft.ML.Transforms.Onnx;
using Image = System.Drawing.Image;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime.Tensors;
using static System.Net.Mime.MediaTypeNames;

namespace OnnxTest;

internal class Program {

    //模型标签
    private static List<string> _lables = new();

    /// <summary>
    /// 用于取出模型中的version信息
    /// </summary>
    private static string _modelVersion = string.Empty;

    /// <summary>
    /// classify = 分类，segment = 分割，detect = 检测，pose = 动作
    /// </summary>
    private static string _taskType = string.Empty;

    /// <summary>
    /// 如果是语义分割模型，如 YOLO8 的 output0 输出会变为如 [1, 8400, 116] 或者 [1, 116, 8400]，
    /// 其中有 4 个坐标框，80 个类别权重，32 个 mask 数据，所以在遍历权重时，要减掉这些循环。
    /// </summary>
    private static int _semanticSegmentationWidth = 0;

    private static int _poseWidth = 0;

    /// <summary>
    /// 缩放比例
    /// </summary>
    private static float _scaleFactor = 1;

    /// <summary>
    /// 张量宽度、高度
    /// </summary>
    private static int _tensorWidth, _tensorHeight;

    private static void Main(string[] args) {
        var assetsRelativePath = $"{AppDomain.CurrentDomain.BaseDirectory}";
        string assetsPath = GetAbsolutePath(assetsRelativePath);
        var modelFilePath = Path.Combine(assetsPath, "OnnxModels", "KD20231129.onnx");
        var images = Path.Combine(assetsPath, "images", "image.jpg");
        var outputFolder = Path.Combine(assetsPath, "images", "output");

        var mlContext = new MLContext();

        try {
            // 加载模型
            using (var session = new InferenceSession(modelFilePath)) {
                // 读取图像数据

                //模型输入名
                var inputName = session.InputNames.First();
                //模型输出名
                var outputName = session.OutputNames.First();
                //输入张量信息
                var inputDimensions = session.InputMetadata[inputName].Dimensions;
                //输出张量信息
                var outDimensions = session.OutputMetadata[outputName].Dimensions;
                //缩放比例
                var maskScaleFactor = 160f / outDimensions[2];
                //获取模型的基本信息
                var modelMetadataCustomMetadataMap = session.ModelMetadata.CustomMetadataMap;

                //如果找到标签名，则使用标签名分分割
                if (modelMetadataCustomMetadataMap.Keys.Contains("names")) {
                    _lables = SplitLabels(modelMetadataCustomMetadataMap["names"]);
                }
                if (modelMetadataCustomMetadataMap.Keys.Contains("version")) {
                    // 用于取出模型中的 version 信息
                    _modelVersion = modelMetadataCustomMetadataMap["version"];
                }

                if (modelMetadataCustomMetadataMap.Keys.Contains("task")) {
                    // classify = 分类，segment = 分割，detect = 检测，pose = 动作
                    _taskType = modelMetadataCustomMetadataMap["task"];

                    if (_taskType == "segment") {
                        // 如果是语义分割模型，如 YOLO8 的 output0 输出会变为如 [1, 8400, 116] 或者 [1, 116, 8400]，
                        // 其中有 4 个坐标框，80 个类别权重，32 个 mask 数据，所以在遍历权重时，要减掉这些循环。
                        _semanticSegmentationWidth = 32;
                    }
                    else if (_taskType == "pose") {
                        // 动作宽度
                        _poseWidth = 51;
                    }
                }
                else {
                    // 如果第一个输出张量只有2个维度,说明是分类模型
                    if (outDimensions.Length == 2) {
                        _taskType = "classify";
                    }
                    else if (outDimensions.Length == 3) {
                        // 如果输出3个维度,且只有一个output,默认为检测模型
                        if (session.OutputNames.Count == 1) {
                            _taskType = "detect";
                        }
                        else if (session.OutputNames.Count == 2) {
                            // 有的模型基本信息里没有 task 任务种类，比如 yolo5n-seg.onnx 就判断输出名有几个，
                            // 如果是2个输出名，默认也为语义分割模型
                            _semanticSegmentationWidth = 32;
                            _taskType = "segment";
                        }
                        else {
                            // throw new Exception("暂不支持的模型");
                        }
                    }
                    else {
                        throw new Exception("暂不支持的模型");
                    }
                }

                _tensorWidth = inputDimensions[3];
                _tensorHeight = inputDimensions[2];

                var image = Image.FromFile(images);

                var inferModel = InferModel((Bitmap)image, inputDimensions, inputName,
                    _modelVersion, session, _semanticSegmentationWidth, _poseWidth,
                    _scaleFactor, _tensorWidth, _tensorHeight);
                /*
                float[] imageData = LoadImageData(images);

                // 创建输入张量
                var inputMeta = session.InputMetadata;
                //var inputName = inputMeta.Keys.First();
                // var tensor = new DenseTensor<float>(imageData, inputMeta[inputName].Dimensions);
                var tensor = new DenseTensor<float>(new ReadOnlySpan<int>(new[] { 1, 3, 640, 640 }));
                // 进行推理
                var inputs = new NamedOnnxValue[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
                using (var results = session.Run(inputs)) {
                    // 获取输出
                    var outputName = session.OutputMetadata.Keys.First();
                    var outputTensor = results.First().AsTensor<float>();

                    // 处理输出，这里假设输出是包含对象坐标的张量
                    float[] outputData = outputTensor.ToArray();

                    // 在这里处理检测到的对象坐标
                    ProcessDetectionResults(outputData);
                }*/
            }

            /*
            var images = ImageNetData.ReadFromFile(imagesFolder);
            var imageNetDatas = images?.ToList();
            var imageDataView = mlContext.Data.LoadFromEnumerable(images);
            // Create instance of model scorer
            var modelScorer = new OnnxModelScorer(imagesFolder, modelFilePath, mlContext);

            // Use model to score data
            var probabilities = modelScorer.Score(imageDataView);
            var parser = new YoloOutputParser();

            var boundingBoxes =
                probabilities
                    .Select(probability => parser.ParseOutputs(probability))
                    .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));
            Console.WriteLine(boundingBoxes);*/
        }
        catch (Exception ex) {
            Console.WriteLine(ex.ToString());
        }

        Console.WriteLine("Hello, World!");
    }

    private static string GetAbsolutePath(string relativePath) {
        var dataRoot = new FileInfo(typeof(Program).Assembly.Location);
        var assemblyFolderPath = dataRoot?.Directory?.FullName;

        var fullPath = Path.Combine(assemblyFolderPath ?? string.Empty, relativePath);

        return fullPath;
    }

    private static float[] LoadImageData(string imagePath) {
        // 使用 System.Drawing 加载图像数据
        using (var bitmap = new Bitmap(imagePath)) {
            // 计算缩放比例以适应模型的输入大小，同时保持原始纵横比
            float scale = Math.Min(1.0f, Math.Min(224.0f / bitmap.Width, 224.0f / bitmap.Height));

            // 调整图像大小
            var resizedBitmap = ResizeImage(bitmap, (int)(bitmap.Width * scale), (int)(bitmap.Height * scale));

            // 将图像数据转换为浮点数数组
            float[] imageData = new float[resizedBitmap.Width * resizedBitmap.Height * 3]; // 3 channels (RGB) per pixel

            for (int y = 0; y < resizedBitmap.Height; y++) {
                for (int x = 0; x < resizedBitmap.Width; x++) {
                    var color = resizedBitmap.GetPixel(x, y);
                    imageData[(y * resizedBitmap.Width + x) * 3] = color.R / 255.0f;
                    imageData[(y * resizedBitmap.Width + x) * 3 + 1] = color.G / 255.0f;
                    imageData[(y * resizedBitmap.Width + x) * 3 + 2] = color.B / 255.0f;
                }
            }

            return imageData;
        }
    }

    private static void ProcessDetectionResults(float[] results) {
        // 在这里处理检测到的对象坐标
        // 输出坐标或其他相关信息
        Console.WriteLine("Detection Results:");
        for (int i = 0; i < results.Length; i += 4) {
            float x = results[i];
            float y = results[i + 1];
            float width = results[i + 2];
            float height = results[i + 3];

            Console.WriteLine($"Object {i / 4 + 1}: X={x}, Y={y}, Width={width}, Height={height}");
        }
    }

    private static Bitmap ResizeImage(Image image, int width, int height) {
        /*var resizedImage = new Bitmap(width, height);
        using (var g = Graphics.FromImage(resizedImage)) {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, 0, 0, width, height);
        }
        return resizedImage;*/
        return new Bitmap(0, 0);
    }

    private static DenseTensor<float> GetDenseTensorInfo(Bitmap bitmap, int[] tensorInfo,
        int tensorWidth, int tensorHeight) {
        float scale = 1;
        var imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        int stride = imageData.Stride;  // 每一行的字节数
        IntPtr scan0 = imageData.Scan0;  // 第一个像素的内存地址

        // 用基本的 float 类型模拟与 DenseTensor 维度相同的数组，并进行写入，如 {3, 640, 640}
        float[,,] temporaryData = new float[tensorInfo[1], tensorInfo[2], tensorInfo[3]];
        float scaledImageWidth = imageData.Width;
        float scaledImageHeight = imageData.Height;

        // 科学的缩放方式，而不是裁剪和拉伸
        // 图片宽和高只要有一个比张量的宽高大，就说明要缩放
        if (scaledImageWidth > tensorWidth || scaledImageHeight > tensorHeight) {
            // 图片更大要缩放
            scale = (tensorWidth / scaledImageWidth) < (tensorHeight / scaledImageHeight) ? (tensorWidth / scaledImageWidth) : (tensorHeight / scaledImageHeight);
            scaledImageWidth *= scale;
            scaledImageHeight *= scale;
        }

        // 用于定位取原始图像中的 x 坐标或 y 坐标位置
        int xPosition, yPosition;
        var coefficient = 1 / scale;

        for (var y = 0; y < (int)scaledImageHeight; y++)  // 高
        {
            for (var x = 0; x < (int)scaledImageWidth; x++) {
                xPosition = (int)(x * coefficient);
                yPosition = (int)(y * coefficient);

                var pixel = IntPtr.Add(scan0, yPosition * stride + xPosition * 3);
                temporaryData[2, y, x] = Marshal.ReadByte(pixel) / 255f;  // B 通道，归一化

                pixel = IntPtr.Add(pixel, 1);
                temporaryData[1, y, x] = Marshal.ReadByte(pixel) / 255f;  // G

                pixel = IntPtr.Add(pixel, 1);
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

    private static List<string> SplitLabels(string input) {
        var withoutBrackets = Regex.Replace(input, "[{}]", "");
        var splitArray = withoutBrackets.Split(',');
        var resultArray = splitArray.Select(item => {
            var startIndex = item.IndexOf(':') + 3;
            var endIndex = item.Length - 1;
            return item.Substring(startIndex, endIndex - startIndex);
        }).ToList();

        return resultArray;
    }

    public static List<YoloData> InferModel(Bitmap imageData, int[] inputDimensions,
        string modleInputName,
        string modelVersion,
        InferenceSession session,
        int semanticSegmentationWidth, int poseWidth,
        float scaleFactor,
        int tensorWidth, int tensorHeight,
        float confidenceThreshold = 0.5f,
        float iouThreshold = 0.3f,
        bool globalIoU = false, int preprocessingMode = 1) {
        var inputTensor = new DenseTensor<float>(inputDimensions);

        var denseTensorInfo = GetDenseTensorInfo(imageData, inputDimensions, tensorWidth, tensorHeight);

        IReadOnlyCollection<NamedOnnxValue> container = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(modleInputName, denseTensorInfo) };
        Tensor<float> output0;
        Tensor<float>? output1;
        List<YoloData> filteredDataArray = new List<YoloData>();
        var finalResultData = new List<YoloData>();

        if (_taskType.Equals("classify")) {
            output0 = session.Run(container).First().AsTensor<float>();
            finalResultData = ConfidenceFilter_Classification(output0, confidenceThreshold);
        }
        else if (_taskType.Equals("detect") || _taskType.Equals("segment")) {
            output0 = session.Run(container).First().AsTensor<float>();

            if (modelVersion.StartsWith("8")) {
                filteredDataArray = ConfidenceFilter_YOLO8Detection(output0, confidenceThreshold, semanticSegmentationWidth, poseWidth);
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
        else if (_taskType.Equals("detect") || _taskType.Equals("pose")) {
            // Both segmentation and detection + segmentation execute this step
            /*var returnedData = ModelSession.Run(Container);
            output0 = returnedData.First().AsTensor<float>();
            output1 = returnedData.ElementAtOrDefault(1)?.AsTensor<float>();

            if (yoloVersion == 8) {
                filteredDataArray = ConfidenceFilter_YOLO8Segmentation(output0, ConfidenceThreshold);
            }
            else {
                filteredDataArray = ConfidenceFilter_YOLO5Segmentation(output0, ConfidenceThreshold);
            }

            finalResultData = NMSFilter(filteredDataArray, IoUThreshold, GlobalIoU);
            RestoreMask(ref finalResultData, output1);*/
        }
        /*else if (_taskType == 4 || _taskType == 5) {
            output0 = ModelSession.Run(Container).First().AsTensor<float>();
            filteredDataArray = ConfidenceFilter_Pose(output0, ConfidenceThreshold);

            finalResultData = NMSFilter(filteredDataArray, IoUThreshold, GlobalIoU);
        }*/

        RestoreCoordinates(ref finalResultData, scaleFactor);
        return finalResultData;
    }

    private static List<YoloData> ConfidenceFilter_Classification(Tensor<float> data, float confidenceThreshold) {
        // Classification model returns output data like {1, 80} representing 80 categories, and the data is the confidence of each category.
        var resultList = new List<YoloData>();

        for (var i = 0; i < data.Dimensions[1]; i++) {
            if (!(data[0, i] >= confidenceThreshold)) continue;
            var temp = new YoloData {
                BasicData = new float[] { data[0, i], i }
            };
            resultList.Add(temp);
        }

        BubbleSortConfidence(ref resultList);
        return resultList;
    }

    private static void BubbleSortConfidence(ref List<YoloData> dataArray) {
        var n = dataArray.Count;
        if (n > 0) {
            var confidenceIndex = dataArray[0].BasicData.Length == 2 ? 0 : 4;

            for (var i = 0; i < n - 1; i++) {
                for (var j = 0; j < n - i - 1; j++) {
                    if (dataArray[j].BasicData[confidenceIndex] < dataArray[j + 1].BasicData[confidenceIndex]) {
                        (dataArray[j], dataArray[j + 1]) = (dataArray[j + 1], dataArray[j]);
                    }
                }
            }
        }
    }

    /// <summary>
    ///  NMS 过滤方法。
    /// </summary>
    /// <param name="initialFilterArray">初始过滤数组</param>
    /// <param name="iouThreshold">IoU 阈值</param>
    /// <param name="globalIoU">全局 IoU 模式</param>
    /// <returns>经过 NMS 过滤后的数组</returns>
    private static List<YoloData> NMSFilter(List<YoloData> initialFilterArray, float iouThreshold, bool globalIoU) {
        // Sort the initial filter array in descending order based on confidence
        BubbleSortConfidence(ref initialFilterArray);

        // Initialize the result array
        var nmsFilterArray = new List<YoloData>();

        // Use LinQ to filter elements based on IoU condition
        nmsFilterArray.AddRange(initialFilterArray.Where(element =>
            nmsFilterArray.All(existingElement =>
                (globalIoU || element.BasicData[5] == existingElement.BasicData[5]) &&
                CalculateIntersectionOverUnion(element.BasicData, existingElement.BasicData) <= iouThreshold)));

        return nmsFilterArray;
    }

    /// <summary>
    /// 计算两个边界框的交并比（IoU），并使用更优化的算法。
    /// </summary>
    private static float CalculateIntersectionOverUnion(float[] box1, float[] box2) {
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

    private void RestoreMask(ref List<YoloData> data, Tensor<float> output1, float scale, float maskScale) {
        // Convert {1,32,160,160} tensor to a matrix of size 32*25600
        Mat ot1 = new Mat(32, 25600, MatType.CV_32F, output1.ToArray());

        foreach (var item in data) {
            // Matrix multiplication
            Mat originalMask = item.MaskData * ot1;

            Parallel.For(0, originalMask.Cols, col => {
                originalMask.At<float>(0, col) = Sigmoid(originalMask.At<float>(0, col));
            });
            // Reshape to 160*160
            Mat reshapedMask = originalMask.Reshape(1, 160);

            // Calculate cropping coordinates
            int maskX1 = Math.Abs((int)((item.BasicData[0] - item.BasicData[2] / 2) * maskScale));
            int maskY1 = Math.Abs((int)((item.BasicData[1] - item.BasicData[3] / 2) * maskScale));
            int maskX2 = (int)(item.BasicData[2] * maskScale);
            int maskY2 = (int)(item.BasicData[3] * maskScale);

            // Ensure cropping does not exceed 160
            if (maskX2 + maskX1 > 160) maskX2 = 160 - maskX1;
            if (maskY1 + maskY2 > 160) maskY2 = 160 - maskY1;

            Rect region = new Rect(maskX1, maskY1, maskX2, maskY2);

            // Crop the mask within 160*160
            Mat croppedMask = new Mat(reshapedMask, region);

            // Resize the cropped mask to the original image size
            Mat restoredOriginalMask = new Mat();
            int enlargedWidth = (int)(croppedMask.Width / maskScale / scale);
            int enlargedHeight = (int)(croppedMask.Height / maskScale / scale);
            Cv2.Resize(croppedMask, restoredOriginalMask, new OpenCvSharp.Size(enlargedWidth, enlargedHeight));

            // Binarize the image: values below 0.5 become 0, values above 0.5 become 1
            Cv2.Threshold(restoredOriginalMask, restoredOriginalMask, 0.5, 1, ThresholdTypes.Binary);

            item.MaskData = restoredOriginalMask;
        }
    }

    private float Sigmoid(float value) {
        return 1 / (1 + (float)Math.Exp(-value));
    }

    private static List<YoloData> ConfidenceFilter_YOLO8Detection(Tensor<float> data,
        float confidenceThreshold, int semanticSegmentationWidth, int poseWidth) {
        var isSizeMiddle = data.Dimensions[1] < data.Dimensions[2];

        if (isSizeMiddle) {
            var resultList = new ConcurrentBag<YoloData>();

            Parallel.For(0, data.Dimensions[2], i => {
                var tempConfidence = 0f;
                var index = -1;

                for (var j = 0; j < data.Dimensions[1] - 4 - semanticSegmentationWidth - poseWidth; j++) {
                    if (data[0, j + 4, i] >= confidenceThreshold) {
                        if (tempConfidence < data[0, j + 4, i]) {
                            tempConfidence = data[0, j + 4, i];
                            index = j;
                        }
                    }
                }

                if (index != -1) {
                    var temp = new YoloData {
                        BasicData = new float[] { data[0, 0, i], data[0, 1, i], data[0, 2, i], data[0, 3, i], tempConfidence, index }
                    };
                    resultList.Add(temp);
                }
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

    private static void RestoreCoordinates(ref List<YoloData> dataList, float scaleFactor) {
        if (dataList.Count > 0) {
            // Check if it is not data returned from a classification model before processing
            if (dataList[0].BasicData.Length > 2) {
                foreach (var t in dataList) {
                    t.BasicData[0] = t.BasicData[0] / scaleFactor;
                    t.BasicData[1] = t.BasicData[1] / scaleFactor;
                    t.BasicData[2] = t.BasicData[2] / scaleFactor;
                    t.BasicData[3] = t.BasicData[3] / scaleFactor;
                }
            }

            // If there are poses, restore the scale for them as well
            if (dataList[0].Poses != null) {
                foreach (var t1 in dataList.SelectMany(t => t.Poses)) {
                    t1.X = t1.X / scaleFactor;
                    t1.Y = t1.Y / scaleFactor;
                }
            }
        }
    }

    public class YoloData {

        /// <summary>
        /// Used to store basic data: x, y, w, h, confidence, label.
        /// </summary>
        public float[] BasicData { get; set; }

        /// <summary>
        /// Default to use a pose model with 17 keypoints.
        /// </summary>
        public Pose[] Poses { get; set; }

        public Mat MaskData { get; set; }
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
}