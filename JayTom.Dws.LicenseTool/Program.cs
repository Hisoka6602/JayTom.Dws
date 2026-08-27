// DWS-COHESIVE-CONTRACTS: 单文件命令行工具的解析、签发和验证命令共同组成入口。
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

    if (arguments.GenerateKeyPair) {
        LicenseKeyGenerator.Generate(arguments);
        return 0;
    }

    if (arguments.ValidateFile) {
        return LicenseValidator.Validate(arguments);
    }

    var request = LicenseGenerationRequest.FromArguments(arguments);
    LicenseGenerator.Generate(request);
    Console.WriteLine($"授权文件已生成：{request.OutputPath}");
    Console.WriteLine($"签名密钥标识：{request.KeyId}");
    return 0;
}
catch (Exception exception) when (
    exception is ArgumentException or InvalidOperationException or IOException or
    UnauthorizedAccessException or System.Security.Cryptography.CryptographicException) {
    Console.Error.WriteLine(exception.Message);
    return 1;
}

internal sealed partial record LicenseGenerationRequest(
    string LicenseCode,
    string MachineCode,
    string CustomerName,
    DateTime ExpirationDate,
    int MaxBindingScannerCount,
    string AppliedTemplateName,
    string Remarks,
    bool IsAvailable,
    string OutputPath,
    string PrivateKey,
    string KeyId) {
    public static LicenseGenerationRequest FromArguments(ToolArguments arguments) {
        var licenseCode = arguments.GetRequired("--license-code").Trim();
        var machineCode = arguments.GetRequired("--machine-code").Trim().ToUpperInvariant();
        if (!MachineCodeRegex().IsMatch(machineCode)) {
            throw new ArgumentException("机器码必须是 64 位十六进制字符串。");
        }

        var customerName = arguments.GetRequired("--customer-name").Trim();
        var expirationDate = ParseExpirationDate(
            arguments.GetRequired("--expiration-date").Trim());
        if (expirationDate <= TimeProvider.System.GetLocalNow().DateTime) {
            throw new ArgumentException("到期时间必须晚于当前时间。");
        }

        var maxBindingScannerCount = arguments.GetInt32(
            "--max-binding-scanner-count",
            1);
        if (maxBindingScannerCount <= 0) {
            throw new ArgumentException("允许绑定的扫码器数量必须大于 0。");
        }

        var privateKeyPath = Path.GetFullPath(
            arguments.GetRequired("--private-key").Trim());
        if (!File.Exists(privateKeyPath)) {
            throw new ArgumentException($"找不到私钥文件：{privateKeyPath}");
        }

        var privateKey = File.ReadAllText(privateKeyPath);
        var keyId = arguments.GetOptional(
            "--key-id",
            LicenseManager.ComputeKeyId(privateKey)).Trim();
        var outputPath = Path.GetFullPath(arguments.GetRequired("--output").Trim());
        return new LicenseGenerationRequest(
            licenseCode,
            machineCode,
            customerName,
            expirationDate,
            maxBindingScannerCount,
            arguments.GetOptional("--applied-template-name", "DWS").Trim(),
            arguments.GetOptional("--remarks", string.Empty),
            arguments.GetBoolean("--is-available", true),
            outputPath,
            privateKey,
            keyId);
    }

    private static DateTime ParseExpirationDate(string value) {
        string[] supportedFormats = [
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy/MM/dd",
            "yyyy/MM/dd HH:mm:ss"
        ];
        if (!DateTime.TryParseExact(
                value,
                supportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var expirationDate)) {
            throw new ArgumentException(
                "到期时间格式错误，请使用 yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss。");
        }

        var hasTimePart = value.Contains(':', StringComparison.Ordinal) ||
                          value.Contains('T', StringComparison.OrdinalIgnoreCase);
        var local = hasTimePart
            ? expirationDate
            : expirationDate.Date.AddDays(1).AddTicks(-1);
        return DateTime.SpecifyKind(local, DateTimeKind.Local);
    }

    [GeneratedRegex("^[A-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex MachineCodeRegex();
}

internal static class LicenseGenerator {
    public static void Generate(LicenseGenerationRequest request) {
        var data = new LicenseData {
            LicenseCode = request.LicenseCode,
            MachineCode = request.MachineCode,
            UserName = request.CustomerName,
            ExpirationDate = request.ExpirationDate,
            CreationTime = TimeProvider.System.GetLocalNow().DateTime,
            MaxBindingScannerCount = request.MaxBindingScannerCount,
            AppliedTemplateName = request.AppliedTemplateName,
            Remarks = request.Remarks,
            IsAvailable = request.IsAvailable
        };
        var result = LicenseManager.GenerateSignedAuthorizationFile(
            data,
            request.PrivateKey,
            request.KeyId,
            request.OutputPath);
        if (!result.IsSuccess) {
            throw new InvalidOperationException(
                $"授权文件生成失败 [{result.ErrorCode}]：{result.Message}");
        }
    }
}

internal static class LicenseKeyGenerator {
    public static void Generate(ToolArguments arguments) {
        var privateKeyPath = Path.GetFullPath(
            arguments.GetRequired("--private-key-output"));
        var publicKeyPath = Path.GetFullPath(
            arguments.GetRequired("--public-key-output"));
        EnsureNewFile(privateKeyPath);
        EnsureNewFile(publicKeyPath);
        Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(publicKeyPath)!);
        LicenseManager.GenerateKeyPair(out var publicKey, out var privateKey);
        File.WriteAllText(privateKeyPath, privateKey);
        File.WriteAllText(publicKeyPath, publicKey);
        Console.WriteLine($"离线私钥已生成：{privateKeyPath}");
        Console.WriteLine($"部署公钥已生成：{publicKeyPath}");
        Console.WriteLine($"密钥标识：{LicenseManager.ComputeKeyId(publicKey)}");
    }

    private static void EnsureNewFile(string path) {
        if (File.Exists(path)) {
            throw new InvalidOperationException(
                $"为避免覆盖密钥，目标文件必须不存在：{path}");
        }
    }
}

internal static class LicenseValidator {
    public static int Validate(ToolArguments arguments) {
        var filePath = Path.GetFullPath(arguments.GetRequired("--validate-file"));
        LicenseOperationResult result;
        LicenseData? data;
        var publicKeyPath = arguments.GetOptional("--public-key", string.Empty);
        if (string.IsNullOrWhiteSpace(publicKeyPath)) {
            var compatible = LicenseManager.DecryptAuthorizationFile(filePath, out data);
            result = compatible.Key
                ? LicenseOperationResult.Success(compatible.Value)
                : LicenseOperationResult.Failure(
                    LicenseErrorCode.InvalidPayload,
                    compatible.Value);
        }
        else {
            var publicKey = File.ReadAllText(Path.GetFullPath(publicKeyPath));
            result = LicenseManager.ValidateAuthorizationFile(
                filePath,
                publicKey,
                expectedMachineCode: arguments.GetOptional(
                    "--machine-code",
                    LicenseManager.GenerateMachineCode()),
                TimeProvider.System,
                out data);
        }

        Console.WriteLine($"[{result.ErrorCode}] {result.Message}");
        if (data is not null) {
            Console.WriteLine($"客户名称：{data.UserName}");
            Console.WriteLine($"到期时间：{data.ExpirationDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"授权状态：{data.IsAvailable}");
        }

        return result.IsSuccess ? 0 : 1;
    }
}

internal sealed class ToolArguments {
    private readonly Dictionary<string, string> _values;

    private ToolArguments(Dictionary<string, string> values) {
        _values = values;
    }

    public bool ShowHelp { get; private init; }

    public bool PrintMachineCode { get; private init; }

    public bool GenerateKeyPair { get; private init; }

    public bool ValidateFile => _values.ContainsKey("--validate-file");

    public static ToolArguments Parse(string[] args) {
        var flags = new HashSet<string>(
            args.Where(static item => item.StartsWith("--", StringComparison.Ordinal)),
            StringComparer.OrdinalIgnoreCase);
        if (flags.Contains("--help") || args.Any(static item => item is "-h" or "/?")) {
            return new ToolArguments(new Dictionary<string, string>()) {
                ShowHelp = true
            };
        }

        if (flags.Contains("--print-machine-code")) {
            return new ToolArguments(new Dictionary<string, string>()) {
                PrintMachineCode = true
            };
        }

        var generateKeyPair = flags.Contains("--generate-key-pair");
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++) {
            var name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal)) {
                throw new ArgumentException($"无法识别的参数：{name}");
            }

            if (name is "--generate-key-pair") {
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--")) {
                throw new ArgumentException($"参数缺少取值：{name}");
            }

            values[name] = args[++index];
        }

        return new ToolArguments(values) { GenerateKeyPair = generateKeyPair };
    }

    public string GetRequired(string name) {
        if (!_values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"缺少必填参数：{name}");
        }

        return value;
    }

    public string GetOptional(string name, string defaultValue) {
        return _values.TryGetValue(name, out var value) ? value : defaultValue;
    }

    public int GetInt32(string name, int defaultValue) {
        return !_values.TryGetValue(name, out var value)
            ? defaultValue
            : int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : throw new ArgumentException($"参数必须是整数：{name}");
    }

    public bool GetBoolean(string name, bool defaultValue) {
        return !_values.TryGetValue(name, out var value)
            ? defaultValue
            : bool.TryParse(value, out var result)
                ? result
                : throw new ArgumentException($"参数必须是 true 或 false：{name}");
    }

    public static void WriteHelp() {
        Console.WriteLine("生成密钥：JayTom.Dws.LicenseTool --generate-key-pair --private-key-output <离线私钥.pem> --public-key-output <部署公钥.pem>");
        Console.WriteLine("签发授权：JayTom.Dws.LicenseTool --private-key <离线私钥.pem> --license-code <授权码> --machine-code <机器码> --customer-name <客户名称> --expiration-date <到期时间> --output <授权文件>");
        Console.WriteLine("校验授权：JayTom.Dws.LicenseTool --validate-file <授权文件> --public-key <部署公钥.pem> [--machine-code <机器码>]");
        Console.WriteLine("查询机器码：JayTom.Dws.LicenseTool --print-machine-code");
    }
}
