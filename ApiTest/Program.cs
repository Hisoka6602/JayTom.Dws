using System.Drawing;
using JayTom.Dws.Interface;
using JayTom.Dws.Interface.geek_;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");

        /*var uploadResponse = await new GeekPlusApi(null).UploadData("SF123456",
            0.1, 0.2, 0.3, 0.4, 0.5);
        Console.WriteLine(uploadResponse);*/

        var bitmap = new Bitmap($@"C:\Users\{Environment.UserName}\Desktop\73510566875475_1698285892241.jpg");

        new GeekPlusApi(null).UploadInBackground("SF123456",
            0.1, DateTime.Now, 0.3, 0.4, 0.5, imageInfo: new UploadImageInfo() {
                CameraSerialNumber = "扫码相机",
                Image = bitmap
            }, panoramaImageInfos: new List<UploadImageInfo>()
            {
                new UploadImageInfo()
                {
                    CameraSerialNumber = "全景相机1",
                    Image = bitmap
                },
                new UploadImageInfo()
                {
                    CameraSerialNumber = "全景相机2",
                    Image = bitmap
                },
            });

        await Task.Delay(100000);
    }
}