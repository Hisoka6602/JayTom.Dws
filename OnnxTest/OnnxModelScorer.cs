using System;
using System.Linq;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using OnnxTest.YoloParser;
using System.Threading.Tasks;
using OnnxTest.DataStructures;
using System.Collections.Generic;

namespace OnnxTest {

    /// <summary>
    /// 评分逻辑类
    /// </summary>
    public class OnnxModelScorer {
        private readonly string imagesFolder;
        private readonly string modelLocation;
        private readonly MLContext mlContext;

        private IList<YoloBoundingBox> _boundingBoxes = new List<YoloBoundingBox>();

        public OnnxModelScorer(string imagesFolder, string modelLocation, MLContext mlContext) {
            this.imagesFolder = imagesFolder;
            this.modelLocation = modelLocation;
            this.mlContext = mlContext;
            //获取图像大小
        }

        /// <summary>
        /// 图像和模型设置
        /// </summary>
        public struct ImageNetSettings {
            public const int imageHeight = 416;
            public const int imageWidth = 416;
        }

        public struct TinyYoloModelSettings {
            // for checking Tiny yolo2 Model input and  output  parameter names,
            //you can use tools like Netron,
            // which is installed by Visual Studio AI Tools

            // input tensor name
            public const string ModelInput = "image";

            // output tensor name
            public const string ModelOutput = "grid";
        }

        private ITransformer LoadModel(string modelLocation) {
            Console.WriteLine("Read model");
            Console.WriteLine($"Model location: {modelLocation}");
            Console.WriteLine($"Default parameters: image size=({ImageNetSettings.imageWidth},{ImageNetSettings.imageHeight})");
            var data = mlContext.Data.LoadFromEnumerable(new List<ImageNetData>());
            var pipeline = mlContext.Transforms.LoadImages(outputColumnName: "image", imageFolder: "", inputColumnName: nameof(ImageNetData.ImagePath))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "image", imageWidth: ImageNetSettings.imageWidth, imageHeight: ImageNetSettings.imageHeight, inputColumnName: "image"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "image"))
                .Append(mlContext.Transforms.ApplyOnnxModel(modelFile: modelLocation, outputColumnNames: new[] { TinyYoloModelSettings.ModelOutput }, inputColumnNames: new[] { TinyYoloModelSettings.ModelInput }));
            var model = pipeline.Fit(data);

            return model;
        }

        private IEnumerable<float[]> PredictDataUsingModel(IDataView testData, ITransformer model) {
            Console.WriteLine($"Images location: {imagesFolder}");
            Console.WriteLine("");
            Console.WriteLine("=====Identify the objects in the images=====");
            Console.WriteLine("");
            var scoredData = model.Transform(testData);
            var probabilities = scoredData.GetColumn<float[]>(TinyYoloModelSettings.ModelOutput);

            return probabilities;
        }

        public IEnumerable<float[]> Score(IDataView data) {
            var model = LoadModel(modelLocation);

            return PredictDataUsingModel(data, model);
        }
    }
}