using System.Drawing;
using JayTom.Dws.Ocr;
using JayTom.Dws.Ocr.Yolo;

internal class Program {

    private static void Main(string[] args) {
        var assetsRelativePath = $"{AppDomain.CurrentDomain.BaseDirectory}";
        string assetsPath = GetAbsolutePath(assetsRelativePath);
        var modelFilePath = Path.Combine(assetsPath, "OnnxModels", "OCR202311292106.onnx");
        var images = Path.Combine(assetsPath, "images", "image.jpg");
        var image2 = Path.Combine(assetsPath, "images", "2.jpg");
        var outputFolder = Path.Combine(assetsPath, "images", "output");
        var yoloParser = new YoloParser(modelFilePath);
        var image = Image.FromFile(images);
        var file = Image.FromFile(image2);
        var o = yoloParser.Evaluate((Bitmap)image, 0.5F, 1.2F);
        var yoloInfos = yoloParser.Evaluate((Bitmap)file);
        var onImage = DrawRectangleOnImage(image, o?.FirstOrDefault()?.Rectangle ?? new Rectangle(0, 0, 0, 0), Color.Coral, 10);
        onImage?.Save($"{System.AppDomain.CurrentDomain.BaseDirectory}1.jpg");
        Console.WriteLine("Hello, World!");
    }

    public static Bitmap DrawRectangleOnImage(Image image, Rectangle drawArea, Color color, int thickness) {
        var markedImage = new Bitmap(image);
        using (var graphics = Graphics.FromImage(markedImage)) {
            using (var pen = new Pen(color, thickness)) {
                graphics.DrawRectangle(pen, drawArea);
            }
        }
        return markedImage;
    }

    private static string GetAbsolutePath(string relativePath) {
        var dataRoot = new FileInfo(typeof(Program).Assembly.Location);
        var assemblyFolderPath = dataRoot?.Directory?.FullName;

        var fullPath = Path.Combine(assemblyFolderPath ?? string.Empty, relativePath);

        return fullPath;
    }
}