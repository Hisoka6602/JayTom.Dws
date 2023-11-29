using System;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace JayTom.Dws.Ocr {

    public class YoloHelper {
        private InferenceSession modelSession;

        /// <summary>
        /// 输入张量的宽高,用于图片缩放 (Width and height of the input tensor, used for image scaling)
        /// </summary>
        private int tensorWidth, tensorHeight;

        private string inputTensorName;
        private string outputTensorName;

        /// <summary>
        /// 用于决定构造输入张量的尺寸 (Used to determine the size of constructing the input tensor)
        /// </summary>
        private int[] inputTensorInfo;

        /// <summary>
        /// 用于判定输出格式,决定哪边是尺寸 (Used to determine the output format, deciding which side is the size)
        /// </summary>
        private int[] outputTensorInfo;

        private DenseTensor<float> inputTensor;
        private int yoloVersion;

        /// <summary>
        /// 通常分割模型的掩膜宽高为160*160,我们根据输入图片的张量宽高计算出掩膜的缩放坐标
        /// (Typically, the mask width and height of a segmentation model are 160*160.
        /// We calculate the scaling coordinates of the mask based on the width and height of the input image tensor.)
        /// </summary>
        private float maskScaleRatio = 0;

        /// <summary>
        /// 用于取出模型中的version信息 (Used to extract the version information from the model)
        /// </summary>
        private string modelVersion = string.Empty;

        /// <summary>
        /// classify=分类 segment=分割 detect=检测 pose=动作
        /// </summary>
        private string taskType = string.Empty;  // 任务类型

        /// <summary>
        /// 如果是语义分割模型,如yolo8的output0输出会变为如[1,8400,116]或者[1,116,8400],其中4个坐标框,80个类别权重,32个mask数据,
        /// 所以在遍历权重时,要减掉这些循环
        /// </summary>
        private int semanticSegmentationWidth = 0;  // 语义分割宽度

        /// <summary>
        /// 通常是17*3=51个数据,由xyv组成
        /// </summary>
        private int poseWidth = 0;  // 动作宽度

        /// <summary>
        /// 必须先调用推理后,才能得到的当前图片缩放比例
        /// </summary>
        private float scalingRatio = 1;  // 缩放比例

        /// <summary>
        /// 从模型中得到所有的标签名数组,注意,部分模型内可能没有包含标签名数据,所以传入后将不会显示,此时需要再外部自行定义传入
        /// </summary>
        public string[] labelArray { get; set; }  // 标签组

        private int taskExecutionMode = 0;  // 执行任务模式

        public int TaskMode {
            get { return taskExecutionMode; }
            set {
                if (taskType == "classify") {
                    taskExecutionMode = 0;  // 分类模式
                }
                // 检测
                else if (taskType == "detect") {
                    taskExecutionMode = 1;  // 检测模式
                }
                // 分割
                else if (taskType == "segment") {
                    if (value is 1 or 2 || value == 3) {
                        taskExecutionMode = value;
                    }
                    else {
                        // 默认分割模式下,即画框,也标注
                        taskExecutionMode = 3;
                    }
                }
                // 动作
                else if (taskType == "pose") {
                    if (value == 1 || value == 4 || value == 5) {
                        taskExecutionMode = value;
                    }
                    else {
                        taskExecutionMode = 5;
                    }
                }
                else {
                    // 如果什么都数据都没有,默认是检测模型
                    taskExecutionMode = 1;
                }
            }
        }
    }
}