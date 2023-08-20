using System.Text;
using JayTom.Dws.Plugin.Ftp;
using JayTom.Dws.Plugin.Tcp;

internal class Program {

    private static async Task Main(string[] args) {
        /*byte[] bytes = { 0xE6, 0xB5, 0x8B, 0xE8, 0xAF, 0x95, 0xE6, 0xB6, 0x88, 0xE6, 0x81, 0xAF };
        string str = Encoding.Default.GetString(bytes);*/
        var tcpCommunicationClient = new TcpCommunicationClient();
        tcpCommunicationClient.SetParameter(new TcpConnectParam {
            Address = "127.0.0.1",
            Port = 60000
        });
        var b = await tcpCommunicationClient.Connect();
        await Task.Delay(1000);
        await tcpCommunicationClient.SendMessage("BarCode:SF797784454");
        await Task.Delay(5000);
        await tcpCommunicationClient.SendMessage("Weight:3235-");
        /*var fluentFtpClient = new FluentFtpClient();
       var connect = await fluentFtpClient.
           Connect("127.0.0.1", "aaa", "123");
       var (key, value) = await fluentFtpClient.UploadFile("C:\\Users\\77051\\Desktop\\cecmAJoYODQ26.jpg",
           "Users\\77051\\Desktop\\cecmAJoYODQ26.jpg");

       var fileList = fluentFtpClient.GetFileList();
       Console.WriteLine(fileList);
       Console.WriteLine(key);
       Console.WriteLine(connect);*/
        Console.WriteLine("Hello, World!");
    }
}