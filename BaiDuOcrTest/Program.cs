using JayTom.Dws.Ocr;
using System.Drawing;
using JayTom.Dws.Ocr.ExpressBill;

internal class Program {

    private static async Task Main(string[] args) {
        var expressBill = new ExpressBill();
        var (key, value) = await expressBill.Initialize();
        var bitmap = Image.FromFile("1.jpg");
        var ocrResult = expressBill.ParseOcrResult((Bitmap)bitmap);
        /*var baiDuOcr = new BaiDuOcr();
        baiDuOcr.ValidateAuthorization();*/
        Console.WriteLine(ocrResult);
    }
}