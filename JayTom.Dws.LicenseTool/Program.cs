using System.Globalization;
using System.Text.RegularExpressions;
using JayTom.Dws.License;

try {
    var arguments = ToolArguments.Parse(args);
    if (arguments.ShowHelp) {
        ToolArguments.WriteHelp();
        return 0;
    }
    if (arguments.PrintMachineCode) {
        Console.WriteLine(LicenseManager.GenerateMachineCode());
        return 0;
    }
    if (arguments.ValidateFile) {
        var validationResult = LicenseManager.DecryptAuthorizationFile(
            arguments.GetRequired("--validate-file"),
            out var licenseData);
        Console.WriteLine(validationResult.Value);
        if (licenseData is not null) {
            Console.WriteLine($"客户名称：{licenseData.UserName}");
            Console.WriteLine($"到期时间：{licenseData.ExpirationDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"授权状态：{licenseData.IsAvailable}");
        }

        return validationResult.Key ? 0 : 1;
    }

    var request = LicenseGenerationRequest.FromArguments(arguments);
    LicenseGenerator.Generate(request);
    Console.WriteLine($"授权文件已生成：{request.OutputPath}");
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException) {
    Console.Error.WriteLine(exception.Message);
    return 1;
}

/// <summary>
/// 授权生成请求。
/// </summary>
internal sealed partial record LicenseGenerationRequest(
    string LicenseCode,
    string MachineCode,
    string CustomerName,
    DateTime ExpirationDate,
    int MaxBindingScannerCount,
    string AppliedTemplateName,
    string Remarks,
    bool IsAvailable,
    string OutputPath) {
    /// <summary>
    /// 从命令行参数创建授权生成请求。
    /// </summary>
    /// <param name="arguments">命令行参数。</param>
    /// <returns>授权生成请求。</returns>
    public static LicenseGenerationRequest FromArguments(ToolArguments arguments) {
        var licenseCode = arguments.GetRequired("--license-code").Trim();
        var machineCode = arguments.GetRequired("--machine-code").Trim().ToUpperInvariant();
        if (!MachineCodeRegex().IsMatch(machineCode)) {
            throw new ArgumentException("机器码必须是 32 位十六进制字符串。");
        }

        var customerName = arguments.GetRequired("--customer-name").Trim();
        var expirationDateText = arguments.GetRequired("--expiration-date").Trim();
        var expirationDate = ParseExpirationDate(expirationDateText);
        if (expirationDate.CompareTo(DateTime.Now) <= 0) {
            throw new ArgumentException("到期时间必须晚于当前时间。");
        }

        var maxBindingScannerCount = arguments.GetInt32("--max-binding-scanner-count", 1);
        if (maxBindingScannerCount <= 0) {
            throw new ArgumentException("允许绑定的扫码器数量必须大于 0。");
        }

        var appliedTemplateName = arguments.GetOptional("--applied-template-name", "DWS").Trim();
        var remarks = arguments.GetOptional("--remarks", string.Empty);
        var isAvailable = arguments.GetBoolean("--is-available", true);
        var outputPath = Path.GetFullPath(arguments.GetRequired("--output").Trim());
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory)) {
            throw new ArgumentException("授权文件输出目录不能为空。");
        }

        return new LicenseGenerationRequest(
            licenseCode,
            machineCode,
            customerName,
            expirationDate,
            maxBindingScannerCount,
            appliedTemplateName,
            remarks,
            isAvailable,
            outputPath);
    }

    /// <summary>
    /// 解析授权到期时间。
    /// </summary>
    /// <param name="value">到期时间文本。</param>
    /// <returns>授权到期时间。</returns>
    private static DateTime ParseExpirationDate(string value) {
        var supportedFormats = new[] {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy/MM/dd",
            "yyyy/MM/dd HH:mm:ss"
        };

        if (!DateTime.TryParseExact(
                value,
                supportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var expirationDate)) {
            throw new ArgumentException("到期时间格式错误，请使用 yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss。");
        }

        var hasTimePart = value.Contains(':', StringComparison.Ordinal) ||
                          value.Contains('T', StringComparison.OrdinalIgnoreCase);
        return hasTimePart ? expirationDate : expirationDate.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// 获取机器码格式校验表达式。
    /// </summary>
    /// <returns>机器码格式校验表达式。</returns>
    [GeneratedRegex("^[A-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex MachineCodeRegex();
}

/// <summary>
/// DWS 授权文件生成器。
/// </summary>
internal static class LicenseGenerator {
    /// <summary>
    /// 生成授权文件。
    /// </summary>
    /// <param name="request">授权生成请求。</param>
    public static void Generate(LicenseGenerationRequest request) {
        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);

        LicenseManager.GenerateKeyPair(out var publicKeyXml, out var privateKeyXml);
        var licenseData = new LicenseData {
            LicenseCode = request.LicenseCode,
            MachineCode = request.MachineCode,
            UserName = request.CustomerName,
            ExpirationDate = request.ExpirationDate,
            CreationTime = DateTime.Now,
            MaxBindingScannerCount = request.MaxBindingScannerCount,
            AppliedTemplateName = request.AppliedTemplateName,
            Remarks = request.Remarks,
            IsAvailable = request.IsAvailable
        };

        var result = LicenseManager.GenerateAuthorizationFile(
            licenseData,
            publicKeyXml,
            privateKeyXml,
            request.OutputPath);

        if (!result.Key) {
            throw new InvalidOperationException($"授权文件生成失败：{result.Value}");
        }

        var licenseFile = new FileInfo(request.OutputPath);
        if (!licenseFile.Exists || licenseFile.Length <= 0) {
            throw new InvalidOperationException("授权文件生成失败：输出文件不存在或内容为空。");
        }
    }
}

/// <summary>
/// 命令行参数集合。
/// </summary>
internal sealed class ToolArguments {
    /// <summary>
    /// 命令行参数字典。
    /// </summary>
    private readonly Dictionary<string, string> _values;

    /// <summary>
    /// 初始化命令行参数集合。
    /// </summary>
    /// <param name="values">命令行参数字典。</param>
    private ToolArguments(Dictionary<string, string> values) {
        _values = values;
    }

    /// <summary>
    /// 是否显示帮助。
    /// </summary>
    public bool ShowHelp { get; private init; }

    /// <summary>
    /// 是否输出当前机器码。
    /// </summary>
    public bool PrintMachineCode { get; private init; }

    /// <summary>
    /// 是否校验授权文件。
    /// </summary>
    public bool ValidateFile => _values.ContainsKey("--validate-file");

    /// <summary>
    /// 解析命令行参数。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>命令行参数集合。</returns>
    public static ToolArguments Parse(string[] args) {
        if (args.Any(static item => item is "-h" or "--help" or "/?")) {
            return new ToolArguments(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)) {
                ShowHelp = true
            };
        }
        if (args.Any(static item => item is "--print-machine-code")) {
            return new ToolArguments(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)) {
                PrintMachineCode = true
            };
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++) {
            var name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal)) {
                throw new ArgumentException($"无法识别的参数：{name}");
            }

            if (index + 1 >= args.Length) {
                throw new ArgumentException($"参数缺少取值：{name}");
            }

            values[name] = args[++index];
        }

        return new ToolArguments(values);
    }

    /// <summary>
    /// 获取必填参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <returns>参数值。</returns>
    public string GetRequired(string name) {
        if (!_values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"缺少必填参数：{name}");
        }

        return value;
    }

    /// <summary>
    /// 获取可选参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>参数值。</returns>
    public string GetOptional(string name, string defaultValue) {
        return _values.TryGetValue(name, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 获取整数参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>整数参数。</returns>
    public int GetInt32(string name, int defaultValue) {
        if (!_values.TryGetValue(name, out var value)) {
            return defaultValue;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ArgumentException($"参数必须是整数：{name}");
    }

    /// <summary>
    /// 获取布尔参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>布尔参数。</returns>
    public bool GetBoolean(string name, bool defaultValue) {
        if (!_values.TryGetValue(name, out var value)) {
            return defaultValue;
        }

        return bool.TryParse(value, out var result)
            ? result
            : throw new ArgumentException($"参数必须是 true 或 false：{name}");
    }

    /// <summary>
    /// 输出帮助信息。
    /// </summary>
    public static void WriteHelp() {
        Console.WriteLine("用法：JayTom.Dws.LicenseTool --license-code <授权码> --machine-code <机器码> --customer-name <客户名称> --expiration-date <到期时间> --output <输出文件>");
        Console.WriteLine("调试：JayTom.Dws.LicenseTool --print-machine-code");
        Console.WriteLine("校验：JayTom.Dws.LicenseTool --validate-file <授权文件>");
    }
}
