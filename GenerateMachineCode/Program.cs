using System.Text;
using System.Management;
using System.Security.Cryptography;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine(GenerateMachineCode());
        Console.ReadLine();
    }

    public static string GenerateMachineCode() {
        var cpuSerialNumber = string.Empty;
        var hardDiskId = string.Empty;
        var machineName = string.Empty;
        var versionString = string.Empty;
        var machineCode = string.Empty;
        try {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var collection = searcher.Get();
            foreach (var o in collection) {
                var obj = (ManagementObject)o;
                cpuSerialNumber += obj?["ProcessorId"].ToString();
            }
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            collection = searcher.Get();
            foreach (var o in collection) {
                var obj = (ManagementObject)o;
                hardDiskId += obj?["SerialNumber"].ToString();
            }

            machineName = Environment.MachineName;
            versionString = Environment.OSVersion.VersionString;

            machineCode = $"{cpuSerialNumber}{hardDiskId}{machineName}{versionString}";

            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes($"{machineCode}Hisoka"));
                var strResult = BitConverter.ToString(result);
                machineCode = strResult.Replace("-", "");
            }
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
        }
        return machineCode;
    }
}