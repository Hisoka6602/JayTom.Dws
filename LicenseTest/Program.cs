using JayTom.Dws.License;

internal class Program {

    private static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        /*var decryptAuthorizationFile = LicenseManager.DecryptAuthorizationFile("U5KLHIJFYWYH192RBMOQ2WA9ULKGIN3M",
            "C:\\Users\\77051\\Desktop\\1708394970959.key", out var data);
        Console.WriteLine(data);*/
        JayTom.Dws.License.LicenseManager.GenerateKeyPair(out var publicKeyXml, out var privateKeyXml);
        //加密
        var (key, value) = JayTom.Dws.License.LicenseManager.GenerateAuthorizationFile(new LicenseData() {
            ExpirationDate = DateTime.Now.AddDays(1),
            MachineCode = LicenseManager.GenerateMachineCode(),
            LicenseCode = "YVFA4NNUID2D2S62S51NTLFPJXVTVWCH",
            UserName = "AAAAAAAAA",
            Remarks = "机器1"
        }, publicKeyXml, privateKeyXml,
            "..\\License.key");
        //写出解密密钥
        //await File.WriteAllTextAsync("..\\License.ini", privateKeyXml);
        //privateKeyXml = await File.ReadAllTextAsync("..\\License.ini");
        //解密
        var decryptAuthorizationFile = JayTom.Dws.License.LicenseManager.DecryptAuthorizationFile("..\\License.key", out var linData);

        Console.WriteLine(linData);
        Console.ReadLine();
    }
}