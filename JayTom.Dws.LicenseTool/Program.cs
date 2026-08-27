using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

try {
    var arguments = ToolArguments.Parse(args);
    if (arguments.ShowHelp) {
        ToolArguments.WriteHelp();
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
    V2LicenseIssuer.Generate(request);
    Console.WriteLine($"授权文件已生成：{request.OutputPath}");
    Console.WriteLine($"签名密钥标识：{request.KeyId}");
    return 0;
}
catch (Exception exception) {
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

        var maxBindingScannerCount = arguments.GetInt32("--max-binding-scanner-count", 1);
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
            V2LicenseIssuer.ComputeKeyId(privateKey)).Trim();
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

internal static class V2LicenseIssuer {
    private const int CurrentFormatVersion = 2;
    private const string SignatureAlgorithm = "PS256";
    private const int MaximumLicenseFileBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    public static void Generate(LicenseGenerationRequest request) {
        var now = TimeProvider.System.GetLocalNow();
        var claims = new LicenseClaims(
            request.LicenseCode,
            request.MachineCode,
            request.CustomerName,
            now.ToUnixTimeSeconds(),
            new DateTimeOffset(request.ExpirationDate).ToUnixTimeSeconds(),
            request.IsAvailable,
            request.MaxBindingScannerCount,
            request.AppliedTemplateName,
            request.Remarks);
        var payload = JsonSerializer.SerializeToUtf8Bytes(claims, JsonOptions);
        using var rsa = ImportRsa(request.PrivateKey, requirePrivateKey: true);
        var signature = rsa.SignData(
            payload,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
        var envelope = new LicenseEnvelope(
            CurrentFormatVersion,
            SignatureAlgorithm,
            request.KeyId,
            Base64UrlEncode(payload),
            Base64UrlEncode(signature));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        if (bytes.Length > MaximumLicenseFileBytes) {
            throw new InvalidOperationException("授权文件超过允许的最大长度。");
        }

        WriteAtomically(request.OutputPath, bytes);
    }

    public static LicenseValidationResult Validate(
        string filePath,
        string publicKey,
        string expectedMachineCode) {
        var file = new FileInfo(filePath);
        if (!file.Exists || file.Length is <= 0 or > MaximumLicenseFileBytes) {
            throw new InvalidDataException("授权文件不存在、为空或超过允许的最大长度。");
        }

        var envelope = JsonSerializer.Deserialize<LicenseEnvelope>(
            File.ReadAllBytes(file.FullName),
            JsonOptions) ?? throw new InvalidDataException("授权文件信封为空。");
        if (envelope.FormatVersion != CurrentFormatVersion ||
            !string.Equals(envelope.Algorithm, SignatureAlgorithm, StringComparison.Ordinal)) {
            throw new InvalidDataException("授权文件版本或签名算法不受支持。");
        }

        using var rsa = ImportRsa(publicKey, requirePrivateKey: false);
        var actualKeyId = ComputeKeyId(rsa.ExportSubjectPublicKeyInfoPem());
        if (!FixedTimeTextEquals(envelope.KeyId, actualKeyId)) {
            throw new CryptographicException("授权文件的密钥标识与公钥不匹配。");
        }

        var payload = Base64UrlDecode(envelope.Payload);
        var signature = Base64UrlDecode(envelope.Signature);
        if (!rsa.VerifyData(
                payload,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss)) {
            throw new CryptographicException("授权文件签名无效或内容已被篡改。");
        }

        var claims = JsonSerializer.Deserialize<LicenseClaims>(payload, JsonOptions)
                     ?? throw new InvalidDataException("授权声明为空。");
        if (!FixedTimeTextEquals(claims.MachineCode, expectedMachineCode)) {
            throw new InvalidDataException("机器码不匹配。");
        }

        var now = TimeProvider.System.GetUtcNow();
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(claims.IssuedAtUnixSeconds);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(claims.ExpiresAtUnixSeconds);
        if (expiresAt <= issuedAt || expiresAt <= now) {
            throw new InvalidDataException("授权有效期无效或已经过期。");
        }
        if (issuedAt - now > TimeSpan.FromMinutes(10)) {
            throw new InvalidDataException("授权签发时间晚于当前时间。");
        }
        if (!claims.IsAvailable) {
            throw new InvalidDataException("授权已被冻结。");
        }

        return new LicenseValidationResult(
            envelope.KeyId,
            claims.UserName,
            expiresAt.LocalDateTime,
            claims.IsAvailable);
    }

    public static void GenerateKeyPair(out string publicKey, out string privateKey) {
        using var rsa = RSA.Create(3072);
        publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        privateKey = rsa.ExportPkcs8PrivateKeyPem();
    }

    public static string ComputeKeyId(string key) {
        using var rsa = ImportRsa(key, requirePrivateKey: false);
        var hash = SHA256.HashData(rsa.ExportSubjectPublicKeyInfo());
        return "dws-" + Base64UrlEncode(hash.AsSpan(0, 12));
    }

    private static RSA ImportRsa(string key, bool requirePrivateKey) {
        if (string.IsNullOrWhiteSpace(key) || !key.Contains("-----BEGIN", StringComparison.Ordinal)) {
            throw new CryptographicException("授权签名密钥必须是 PEM 格式。");
        }

        var rsa = RSA.Create();
        try {
            rsa.ImportFromPem(key);
            if (requirePrivateKey) {
                _ = rsa.ExportParameters(includePrivateParameters: true);
            }
            return rsa;
        }
        catch {
            rsa.Dispose();
            throw;
        }
    }

    private static bool FixedTimeTextEquals(string left, string right) {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left.Trim().ToUpperInvariant()),
            Encoding.UTF8.GetBytes(right.Trim().ToUpperInvariant()));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> value) {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value) {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
        return Convert.FromBase64String(normalized);
    }

    private static void WriteAtomically(string filePath, byte[] bytes) {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory)) {
            throw new ArgumentException("授权文件输出目录不能为空。", nameof(filePath));
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record LicenseEnvelope(
        int FormatVersion,
        string Algorithm,
        string KeyId,
        string Payload,
        string Signature);

    private sealed record LicenseClaims(
        [property: JsonPropertyOrder(0)] string LicenseCode,
        [property: JsonPropertyOrder(1)] string MachineCode,
        [property: JsonPropertyOrder(2)] string UserName,
        [property: JsonPropertyOrder(3)] long IssuedAtUnixSeconds,
        [property: JsonPropertyOrder(4)] long ExpiresAtUnixSeconds,
        [property: JsonPropertyOrder(5)] bool IsAvailable,
        [property: JsonPropertyOrder(6)] int MaxBindingScannerCount,
        [property: JsonPropertyOrder(7)] string AppliedTemplateName,
        [property: JsonPropertyOrder(8)] string Remarks);
}

internal sealed record LicenseValidationResult(
    string KeyId,
    string CustomerName,
    DateTime ExpirationDate,
    bool IsAvailable);

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
        V2LicenseIssuer.GenerateKeyPair(out var publicKey, out var privateKey);
        File.WriteAllText(privateKeyPath, privateKey);
        File.WriteAllText(publicKeyPath, publicKey);
        Console.WriteLine($"离线私钥已生成：{privateKeyPath}");
        Console.WriteLine($"部署公钥已生成：{publicKeyPath}");
        Console.WriteLine($"密钥标识：{V2LicenseIssuer.ComputeKeyId(publicKey)}");
    }

    private static void EnsureNewFile(string path) {
        if (File.Exists(path)) {
            throw new InvalidOperationException($"为避免覆盖密钥，目标文件必须不存在：{path}");
        }
    }
}

internal static class LicenseValidator {
    public static int Validate(ToolArguments arguments) {
        var filePath = Path.GetFullPath(arguments.GetRequired("--validate-file"));
        var publicKeyPath = Path.GetFullPath(arguments.GetRequired("--public-key"));
        var machineCode = arguments.GetRequired("--machine-code").Trim().ToUpperInvariant();
        var result = V2LicenseIssuer.Validate(
            filePath,
            File.ReadAllText(publicKeyPath),
            machineCode);
        Console.WriteLine("[None] 授权正常。");
        Console.WriteLine($"客户名称：{result.CustomerName}");
        Console.WriteLine($"到期时间：{result.ExpirationDate:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"授权状态：{result.IsAvailable}");
        return 0;
    }
}

internal sealed class ToolArguments {
    private readonly Dictionary<string, string> _values;

    private ToolArguments(Dictionary<string, string> values) {
        _values = values;
    }

    public bool ShowHelp { get; private init; }
    public bool GenerateKeyPair { get; private init; }
    public bool ValidateFile => _values.ContainsKey("--validate-file");

    public static ToolArguments Parse(string[] args) {
        var flags = new HashSet<string>(
            args.Where(static item => item.StartsWith("--", StringComparison.Ordinal)),
            StringComparer.OrdinalIgnoreCase);
        if (flags.Contains("--help") || args.Any(static item => item is "-h" or "/?")) {
            return new ToolArguments(new Dictionary<string, string>()) { ShowHelp = true };
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
            : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
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
        Console.WriteLine("签发授权：JayTom.Dws.LicenseTool --private-key <离线私钥.pem> --license-code <授权码> --machine-code <64位机器码> --customer-name <客户名称> --expiration-date <到期时间> --output <授权文件>");
        Console.WriteLine("校验授权：JayTom.Dws.LicenseTool --validate-file <授权文件> --public-key <部署公钥.pem> --machine-code <64位机器码>");
    }
}
