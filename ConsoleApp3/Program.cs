using System.Text.RegularExpressions;
using JayTom.Dws.Plugin.WeighingScale;

internal class Program {

    private static void Main(string[] args) {
        /*string pattern = @"^\+\s*([\d.]+)\s*kg$";
        string input = "+  0.000 kg";

        Regex regex = new Regex(pattern);
        Match match = regex.Match(input);

        if (match.Success) {
            string result = match.Groups[1].Value;
            Console.WriteLine(result); // 输出: 0.000
        }
        return;*/

        string input1 = "88 02 00 00 00 04 01 16 ";
        float weight1 = ExtractWeightFromHex(input1);
        Console.WriteLine("Weight 1: " + weight1); // 输出：4.268

        string input2 = "88 02 02 07 01 01 06 16";
        float weight2 = ExtractWeightFromHex(input2);
        Console.WriteLine("Weight 2: " + weight2); // 输出：27.116
        return;
        var weighingScale = new WeighingScale();
        weighingScale.CurrentWeight += delegate (object? sender, float f) {
            Console.WriteLine($"实时重量:{f:F3}");
        };
        weighingScale.StabledWeight += delegate (object? sender, float f) {
            Console.WriteLine($"稳定重量:{f:F3}");
        };
        weighingScale.Excepted += delegate (object? sender, Exception exception) {
            Console.WriteLine($"{exception}");
        };
        /*weighingScale.Received += delegate (object? sender, string s) {
            Console.WriteLine($"收到的内容:{s}");
        };*/
        weighingScale.SetWeightCalculationParameters(new WeightCalculationParameters() {
            AccessMode = WeightAccessMode.Readonly,
            CharacterLength = 8,
            IdentifierPosition = 0,
            IntegerStartPosition = 5,
            IntegerEndPosition = 7,
            DecimalStartPosition = 1,
            DecimalEndPosition = 3,
            MinWeight = -50,
            IsReversed = true,
            Identifier = "=",
        });
        var connect = weighingScale.Connect(new WeighingScale.ConnectInfo() {
            PortName = "COM3",
            Parity = 0,
            BaudRate = 9600,
            DataBits = 8,
            StopBits = 1,
        });
        Console.WriteLine(connect);
        Console.WriteLine("Hello, World!");
        Console.ReadLine();
    }

    public static float ExtractWeightFromHex(string input) {
        // 移除所有空格
        try {
            var hexString = input.Replace(" ", "");
            if (hexString.Length == 16) {
                string weightSubstring = hexString.Substring(4, 10).Replace("0", string.Empty);
                int.TryParse(weightSubstring, out var weightInt);
                return weightInt / 1000f;
            }
        }
        catch {
        }

        return 0;
    }
}