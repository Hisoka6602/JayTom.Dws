using System.Drawing;
using JayTom.Dws.Ocr;

internal class Program {

    private static void Main(string[] args) {
        var assetsRelativePath = $"{AppDomain.CurrentDomain.BaseDirectory}";
        string assetsPath = GetAbsolutePath(assetsRelativePath);
        var modelFilePath = Path.Combine(assetsPath, "OnnxModels", "KD20231129.onnx");
        var images = Path.Combine(assetsPath, "images", "image.jpg");
        var outputFolder = Path.Combine(assetsPath, "images", "output");
        var yoloParser = new YoloParser(modelFilePath);
        var image = Image.FromFile(images);
        var o = yoloParser.Evaluate((Bitmap)image);
        Console.WriteLine("Hello, World!");
    }

    private static string GetAbsolutePath(string relativePath) {
        var dataRoot = new FileInfo(typeof(Program).Assembly.Location);
        var assemblyFolderPath = dataRoot?.Directory?.FullName;

        var fullPath = Path.Combine(assemblyFolderPath ?? string.Empty, relativePath);

        return fullPath;
    }
}