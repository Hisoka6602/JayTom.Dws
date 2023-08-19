using JayTom.Dws.Plugin.Ftp;

internal class Program {

    private static async Task Main(string[] args) {
        var fluentFtpClient = new FluentFtpClient();
        var connect = await fluentFtpClient.
            Connect("127.0.0.1", "aaa", "123");
        var (key, value) = await fluentFtpClient.UploadFile("C:\\Users\\77051\\Desktop\\cecmAJoYODQ26.jpg",
            "Users\\77051\\Desktop\\cecmAJoYODQ26.jpg");

        var fileList = fluentFtpClient.GetFileList();
        Console.WriteLine(fileList);
        Console.WriteLine(key);
        Console.WriteLine(connect);
        Console.WriteLine("Hello, World!");
    }
}