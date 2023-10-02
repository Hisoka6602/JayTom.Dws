using JayTom.Dws.Ocr;
using JayTom.Dws.Ocr.BaiduOcr;

internal class Program {

    private static void Main(string[] args) {
        var baiDuOcr = new OcrgveEngine();
        baiDuOcr.Test();
        /*var baiDuOcr = new BaiDuOcr();
        baiDuOcr.ValidateAuthorization();*/
        Console.WriteLine("Hello, World!");
    }
}