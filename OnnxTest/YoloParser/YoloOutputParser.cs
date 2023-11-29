using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OnnxTest.YoloParser {

    /// <summary>
    /// 分析器(输出)
    /// </summary>
    public class YoloOutputParser {
        public const int ROW_COUNT = 13;
        public const int COL_COUNT = 13;
        public const int CHANNEL_COUNT = 125;
        public const int BOXES_PER_CELL = 5;
        public const int BOX_INFO_FEATURE_COUNT = 5;
        public const int CLASS_COUNT = 20;
        public const float CELL_WIDTH = 32;
        public const float CELL_HEIGHT = 32;

        private int channelStride = ROW_COUNT * COL_COUNT;

        public class CellDimensions : DimensionsBase { }

        private float[] _anchors = new float[]
        {
            1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
        };

        private string[] _labels = new string[]
        {
            "ExpressBill"
        };

        private static Color[] _classColors = new Color[]
        {
            Color.Khaki,
            Color.Fuchsia,
            Color.Silver,
            Color.RoyalBlue,
            Color.Green,
            Color.DarkOrange,
            Color.Purple,
            Color.Gold,
            Color.Red,
            Color.Aquamarine,
            Color.Lime,
            Color.AliceBlue,
            Color.Sienna,
            Color.Orchid,
            Color.Tan,
            Color.LightPink,
            Color.Yellow,
            Color.HotPink,
            Color.OliveDrab,
            Color.SandyBrown,
            Color.DarkTurquoise
        };

        /// <summary>
        /// 应用 sigmoid 函数，该函数输出介于 0 和 1 之间的数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float Sigmoid(float value) {
            var k = (float)Math.Exp(value);
            return k / (1.0f + k);
        }

        /// <summary>
        /// 将输入向量规范化为概率分布
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private float[] Softmax(float[] values) {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp)).ToArray();
        }

        /// <summary>
        /// 将一维模型输出中的元素映射到 125 x 13 x 13 张量中的对应位置
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private int GetOffset(int x, int y, int channel) {
            // YOLO outputs a tensor that has a shape of 125x13x13, which
            // WinML flattens into a 1D array.  To access a specific channel
            // for a given (x,y) cell position, we need to calculate an offset
            // into the array
            return (channel * this.channelStride) + (y * COL_COUNT) + x;
        }

        /// <summary>
        /// 使用模型输出中的 GetOffset 方法提取边界框维度
        /// </summary>
        /// <param name="modelOutput"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private BoundingBoxDimensions ExtractBoundingBoxDimensions(float[] modelOutput, int x, int y, int channel) {
            return new BoundingBoxDimensions {
                X = modelOutput[GetOffset(x, y, channel)],
                Y = modelOutput[GetOffset(x, y, channel + 1)],
                Width = modelOutput[GetOffset(x, y, channel + 2)],
                Height = modelOutput[GetOffset(x, y, channel + 3)]
            };
        }

        /// <summary>
        /// 提取置信度值，该值表示模型是否检测到对象，并使用 Sigmoid 函数将其转换为百分比
        /// </summary>
        /// <param name="modelOutput"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private float GetConfidence(float[] modelOutput, int x, int y, int channel) {
            return Sigmoid(modelOutput[GetOffset(x, y, channel + 4)]);
        }

        /// <summary>
        /// 使用边界框维度并将它们映射到图像中的相应单元格。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="box"></param>
        /// <param name="boxDimensions"></param>
        /// <returns></returns>
        private CellDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions) {
            return new CellDimensions {
                X = ((float)x + Sigmoid(boxDimensions.X)) * CELL_WIDTH,
                Y = ((float)y + Sigmoid(boxDimensions.Y)) * CELL_HEIGHT,
                Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH * _anchors[box * 2],
                Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT * _anchors[box * 2 + 1],
            };
        }

        /// <summary>
        /// 使用 GetOffset 方法从模型输出中提取边界框的类预测，并使用 Softmax 方法将它们转换为概率分布。
        /// </summary>
        /// <param name="modelOutput"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="channel"></param>
        /// <returns></returns>

        public float[] ExtractClasses(float[] modelOutput, int x, int y, int channel) {
            float[] predictedClasses = new float[CLASS_COUNT];
            int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;
            for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++) {
                predictedClasses[predictedClass] = modelOutput[GetOffset(x, y, predictedClass + predictedClassOffset)];
            }
            return Softmax(predictedClasses);
        }

        /// <summary>
        /// 从具有最高概率的预测类列表中选择类
        /// </summary>
        /// <param name="predictedClasses"></param>
        /// <returns></returns>
        private ValueTuple<int, float> GetTopResult(float[] predictedClasses) {
            return predictedClasses
                .Select((predictedClass, index) => (Index: index, Value: predictedClass)).MaxBy(result => result.Value);
        }

        /// <summary>
        /// 筛选具有较低概率的重叠边界框
        /// </summary>
        /// <param name="boundingBoxA"></param>
        /// <param name="boundingBoxB"></param>
        /// <returns></returns>
        private float IntersectionOverUnion(RectangleF boundingBoxA, RectangleF boundingBoxB) {
            var areaA = boundingBoxA.Width * boundingBoxA.Height;

            if (areaA <= 0)
                return 0;

            var areaB = boundingBoxB.Width * boundingBoxB.Height;

            if (areaB <= 0)
                return 0;

            var minX = Math.Max(boundingBoxA.Left, boundingBoxB.Left);
            var minY = Math.Max(boundingBoxA.Top, boundingBoxB.Top);
            var maxX = Math.Min(boundingBoxA.Right, boundingBoxB.Right);
            var maxY = Math.Min(boundingBoxA.Bottom, boundingBoxB.Bottom);

            var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }

        /// <summary>
        /// 模型输出
        /// </summary>
        /// <param name="yoloModelOutputs"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public IList<YoloBoundingBox> ParseOutputs(float[] yoloModelOutputs, float threshold = .3F) {
            var boxes = new List<YoloBoundingBox>();
            for (var row = 0; row < ROW_COUNT; row++) {
                for (int column = 0; column < COL_COUNT; column++) {
                    for (int box = 0; box < BOXES_PER_CELL; box++) {
                        var channel = (box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT));
                        var boundingBoxDimensions = ExtractBoundingBoxDimensions(yoloModelOutputs, row, column, channel);
                        var confidence = GetConfidence(yoloModelOutputs, row, column, channel);
                        var mappedBoundingBox = MapBoundingBoxToCell(row, column, box, boundingBoxDimensions);
                        if (confidence < threshold)
                            continue;
                        var predictedClasses = ExtractClasses(yoloModelOutputs, row, column, channel);
                        var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                        var topScore = topResultScore * confidence;
                        if (topScore < threshold)
                            continue;
                        boxes.Add(new YoloBoundingBox() {
                            Dimensions = new BoundingBoxDimensions {
                                X = (mappedBoundingBox.X - mappedBoundingBox.Width / 2),
                                Y = (mappedBoundingBox.Y - mappedBoundingBox.Height / 2),
                                Width = mappedBoundingBox.Width,
                                Height = mappedBoundingBox.Height,
                            },
                            Confidence = topScore,
                            Label = _labels[topResultIndex],
                            BoxColor = _classColors[topResultIndex]
                        });
                    }
                }
            }
            return boxes;
        }

        public IList<YoloBoundingBox> FilterBoundingBoxes(IList<YoloBoundingBox> boxes, int limit, float threshold) {
            var activeCount = boxes.Count;
            var isActiveBoxes = new bool[boxes.Count];

            for (var i = 0; i < isActiveBoxes.Length; i++)
                isActiveBoxes[i] = true;

            var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                .OrderByDescending(b => b.Box.Confidence)
                .ToList();
            var results = new List<YoloBoundingBox>();
            for (var i = 0; i < boxes.Count; i++) {
                if (isActiveBoxes[i]) {
                    var boxA = sortedBoxes[i].Box;
                    results.Add(boxA);

                    if (results.Count >= limit)
                        break;
                    for (var j = i + 1; j < boxes.Count; j++) {
                        if (isActiveBoxes[j]) {
                            var boxB = sortedBoxes[j].Box;

                            if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold) {
                                isActiveBoxes[j] = false;
                                activeCount--;

                                if (activeCount <= 0)
                                    break;
                            }
                        }
                    }
                }
            }
            return results;
        }
    }
}