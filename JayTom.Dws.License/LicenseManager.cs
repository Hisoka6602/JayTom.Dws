using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JayTom.Dws.License;

/// <summary>
/// 提供 DWS v2 授权文件的签发、校验和机器绑定能力。
/// </summary>
public static class LicenseManager {
    private const int CurrentFormatVersion = 2;
    private const int MaximumLicenseFileBytes = 1024 * 1024;
    private const string SignatureAlgorithm = "PS256";
    private const string DefaultPublicKeyFileName = "license-public.pem";
    private const string TrustDirectoryName = "license-trust";
    private const string RevocationFileName = "revoked-keys.txt";
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    /// <summary>
    /// 生成 PEM 编码的 RSA 公私钥对；私钥必须保留在离线签发环境中。
    /// </summary>
    public static void GenerateKeyPair(out string publicKeyXml, out string privateKeyXml) {
        using var rsa = RSA.Create(3072);
        publicKeyXml = rsa.ExportSubjectPublicKeyInfoPem();
        privateKeyXml = rsa.ExportPkcs8PrivateKeyPem();
    }

    /// <summary>
    /// 使用 RSA-PSS/SHA-256 对任意数据签名。
    /// </summary>
    /// <param name="data">待签名文本。</param>
    /// <param name="privateKeyXml">PEM 或旧 XML 编码私钥。</param>
    /// <returns>签名字节。</returns>
    public static byte[] GenerateAuthorizationFile(string data, string privateKeyXml) {
        ArgumentNullException.ThrowIfNull(data);
        using var rsa = ImportRsa(privateKeyXml, requirePrivateKey: true);
        return rsa.SignData(
            Encoding.UTF8.GetBytes(data),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
    }

    /// <summary>
    /// 旧 API 无法从数字签名恢复原文，因此始终拒绝调用。
    /// </summary>
    [Obsolete("数字签名不能解密为原文。请使用 ValidateAuthorizationFile 校验签名。")]
    public static string DecryptAuthorizationByte(byte[] encryptedData, string publicKeyXml) {
        throw new NotSupportedException(
            "DWS v2 使用不可逆数字签名；请保留原文并调用签名校验 API。");
    }

    /// <summary>
    /// 拒绝只提供公钥的旧加密写法，防止继续生成可伪造的自带密钥文件。
    /// </summary>
    [Obsolete("DWS v2 必须使用离线私钥签名；请调用包含 privateKeyXml 和 keyId 的重载。")]
    public static void GenerateAuthorizationFile(
        LicenseData data,
        string publicKeyXml,
        string filePath) {
        throw new NotSupportedException(
            "不能使用公钥签发授权。请在离线签发工具中提供私钥和密钥标识。");
    }

    /// <summary>
    /// 使用离线私钥生成 v2 授权文件。
    /// </summary>
    /// <param name="data">授权声明。</param>
    /// <param name="privateKey">PEM 或旧 XML 编码私钥。</param>
    /// <param name="keyId">信任根中的稳定密钥标识。</param>
    /// <param name="filePath">输出路径。</param>
    /// <returns>签发结果。</returns>
    public static LicenseOperationResult GenerateSignedAuthorizationFile(
        LicenseData data,
        string privateKey,
        string keyId,
        string filePath) {
        ArgumentNullException.ThrowIfNull(data);
        if (!IsValidKeyId(keyId)) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.InvalidKey,
                "密钥标识不能为空。");
        }

        try {
            ValidateOutputPath(filePath);
            var claims = LicenseClaims.From(data);
            var payload = JsonSerializer.SerializeToUtf8Bytes(claims, JsonOptions);
            using var rsa = ImportRsa(privateKey, requirePrivateKey: true);
            var signature = rsa.SignData(
                payload,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);
            var envelope = new LicenseEnvelope(
                CurrentFormatVersion,
                SignatureAlgorithm,
                keyId.Trim(),
                Base64UrlEncode(payload),
                Base64UrlEncode(signature));
            var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
            if (bytes.Length > MaximumLicenseFileBytes) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.FileTooLarge,
                    "授权文件超过允许的最大长度。");
            }

            WriteAtomically(filePath, bytes);
            return LicenseOperationResult.Success("授权文件已安全签发。");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            CryptographicException or JsonException or ArgumentException) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.SigningFailed,
                $"授权文件签发失败：{exception.Message}");
        }
    }

    /// <summary>
    /// 兼容旧调用签名；publicKeyXml 不写入授权文件，也不参与信任判定。
    /// </summary>
    public static KeyValuePair<bool, string> GenerateAuthorizationFile(
        LicenseData data,
        string publicKeyXml,
        string privateKeyXml,
        string filePath) {
        var keyId = ComputeKeyId(publicKeyXml);
        var result = GenerateSignedAuthorizationFile(
            data,
            privateKeyXml,
            keyId,
            filePath);
        return new KeyValuePair<bool, string>(result.IsSuccess, result.Message);
    }

    /// <summary>
    /// 兼容旧方法名并转发到 v2 签发实现。
    /// </summary>
    [Obsolete("请使用 GenerateSignedAuthorizationFile。")]
    public static void GenerateAuthorizationFile1(
        LicenseData data,
        string publicKeyXml,
        string privateKeyXml,
        string filePath) {
        var result = GenerateAuthorizationFile(
            data,
            publicKeyXml,
            privateKeyXml,
            filePath);
        if (!result.Key) {
            throw new InvalidOperationException(result.Value);
        }
    }

    /// <summary>
    /// 使用显式信任公钥校验 v2 授权文件。
    /// </summary>
    /// <param name="filePath">授权文件路径。</param>
    /// <param name="trustedPublicKey">PEM 或旧 XML 编码的受信任公钥。</param>
    /// <param name="expectedMachineCode">期望的机器码；为空时读取本机机器码。</param>
    /// <param name="timeProvider">可测试、可替换的时间源。</param>
    /// <param name="data">校验成功后的授权声明。</param>
    /// <returns>结构化校验结果。</returns>
    public static LicenseOperationResult ValidateAuthorizationFile(
        string filePath,
        string trustedPublicKey,
        string? expectedMachineCode,
        TimeProvider? timeProvider,
        out LicenseData? data) {
        data = null;
        var envelopeResult = ReadEnvelope(filePath, out var envelope);
        if (!envelopeResult.IsSuccess || envelope is null) {
            return envelopeResult;
        }

        try {
            if (envelope.FormatVersion != CurrentFormatVersion ||
                !string.Equals(
                    envelope.Algorithm,
                    SignatureAlgorithm,
                    StringComparison.Ordinal)) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.UnsupportedFormat,
                    "授权文件版本或签名算法不受支持。");
            }

            var payload = Base64UrlDecode(envelope.Payload);
            var signature = Base64UrlDecode(envelope.Signature);
            using var rsa = ImportRsa(trustedPublicKey, requirePrivateKey: false);
            var actualKeyId = ComputeKeyId(rsa.ExportSubjectPublicKeyInfoPem());
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(envelope.KeyId),
                    Encoding.UTF8.GetBytes(actualKeyId))) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.UntrustedKey,
                    "授权文件的密钥标识与信任根不匹配。");
            }

            if (!rsa.VerifyData(
                    payload,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss)) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.InvalidSignature,
                    "授权文件签名无效或内容已被篡改。");
            }

            var claims = JsonSerializer.Deserialize<LicenseClaims>(payload, JsonOptions);
            if (claims is null) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.InvalidPayload,
                    "授权声明为空。");
            }

            var now = (timeProvider ?? TimeProvider.System).GetLocalNow();
            var timeResult = ValidateTimes(claims, now);
            if (!timeResult.IsSuccess) {
                return timeResult;
            }

            var machineCode = expectedMachineCode ?? GenerateMachineCode();
            if (!FixedTimeTextEquals(claims.MachineCode, machineCode)) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.MachineMismatch,
                    "机器码不匹配。");
            }

            if (!claims.IsAvailable) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.RevokedLicense,
                    "授权已被冻结。");
            }

            data = claims.ToLicenseData();
            return LicenseOperationResult.Success("授权正常。");
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException or FormatException or
            ArgumentException) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.InvalidPayload,
                $"授权文件解析失败：{exception.Message}");
        }
    }

    /// <summary>
    /// 从应用信任目录或环境变量解析公钥并校验授权文件。
    /// </summary>
    public static KeyValuePair<bool, string> DecryptAuthorizationFile(
        string filePath,
        out LicenseData? data) {
        data = null;
        var envelopeResult = ReadEnvelope(filePath, out var envelope);
        if (!envelopeResult.IsSuccess || envelope is null) {
            return new KeyValuePair<bool, string>(false, envelopeResult.Message);
        }

        var trustResult = TryResolveTrustedPublicKey(envelope.KeyId, out var publicKey);
        if (!trustResult.IsSuccess || publicKey is null) {
            return new KeyValuePair<bool, string>(false, trustResult.Message);
        }

        var result = ValidateAuthorizationFile(
            filePath,
            publicKey,
            expectedMachineCode: null,
            TimeProvider.System,
            out data);
        return new KeyValuePair<bool, string>(result.IsSuccess, result.Message);
    }

    /// <summary>
    /// 使用调用方明确提供的受信任公钥校验授权文件。
    /// </summary>
    public static KeyValuePair<bool, string> DecryptAuthorizationFile(
        string privateKeyXml,
        string filePath,
        out LicenseData? data) {
        var result = ValidateAuthorizationFile(
            filePath,
            privateKeyXml,
            expectedMachineCode: null,
            TimeProvider.System,
            out data);
        return new KeyValuePair<bool, string>(result.IsSuccess, result.Message);
    }

    /// <summary>
    /// 兼容旧布尔返回 API，并使用外置信任根执行校验。
    /// </summary>
    [Obsolete("请使用 DecryptAuthorizationFile。")]
    public static bool DecryptAuthorizationFile1(
        string filePath,
        out LicenseData? data) {
        return DecryptAuthorizationFile(filePath, out data).Key;
    }

    /// <summary>
    /// 生成隐私安全、产品域隔离的 SHA-256 机器指纹。
    /// </summary>
    public static string GenerateMachineCode() {
        var identifiers = new List<string>();
        TryAppendWmiValues(
            identifiers,
            "SELECT ProcessorId FROM Win32_Processor",
            "ProcessorId");
        TryAppendNonUsbDiskSerials(identifiers);
        identifiers.Add(Environment.MachineName.Trim());
        identifiers.Add(Environment.OSVersion.Version.Major.ToString());

        var normalized = string.Join(
            '|',
            identifiers
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim().ToUpperInvariant())
                .OrderBy(static value => value, StringComparer.Ordinal));
        var fingerprint = SHA256.HashData(
            Encoding.UTF8.GetBytes($"JayTom.Dws/MachineBinding/v2|{normalized}"));
        // DWS-HEX-COMPACT: 机器指纹是协议字段，必须保持无分隔符格式。
        return Convert.ToHexString(fingerprint);
    }

    /// <summary>
    /// 计算公钥的稳定 SHA-256 标识，用于轮换和吊销。
    /// </summary>
    public static string ComputeKeyId(string publicKey) {
        using var rsa = ImportRsa(publicKey, requirePrivateKey: false);
        var hash = SHA256.HashData(rsa.ExportSubjectPublicKeyInfo());
        return "dws-" + Base64UrlEncode(hash.AsSpan(0, 12));
    }

    private static LicenseOperationResult ReadEnvelope(
        string filePath,
        out LicenseEnvelope? envelope) {
        envelope = null;
        try {
            var file = new FileInfo(filePath);
            if (!file.Exists) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.FileNotFound,
                    "授权文件不存在。");
            }

            if (file.Length is <= 0 or > MaximumLicenseFileBytes) {
                return LicenseOperationResult.Failure(
                    LicenseErrorCode.FileTooLarge,
                    "授权文件为空或超过允许的最大长度。");
            }

            var bytes = File.ReadAllBytes(file.FullName);
            envelope = JsonSerializer.Deserialize<LicenseEnvelope>(bytes, JsonOptions);
            return envelope is null || !IsValidKeyId(envelope.KeyId)
                ? LicenseOperationResult.Failure(
                    LicenseErrorCode.InvalidPayload,
                    "授权文件信封不完整。")
                : LicenseOperationResult.Success("授权文件信封有效。");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.InvalidPayload,
                $"无法读取授权文件：{exception.Message}");
        }
    }

    private static LicenseOperationResult TryResolveTrustedPublicKey(
        string keyId,
        out string? publicKey) {
        publicKey = null;
        var revoked = Environment.GetEnvironmentVariable("DWS_LICENSE_REVOKED_KEY_IDS")
            ?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        var trustDirectory = Path.Combine(AppContext.BaseDirectory, TrustDirectoryName);
        var revocationPath = Path.Combine(trustDirectory, RevocationFileName);
        if (File.Exists(revocationPath)) {
            revoked = [.. revoked, .. File.ReadAllLines(revocationPath)
                .Select(static line => line.Trim())
                .Where(static line => line.Length > 0 && !line.StartsWith('#'))];
        }

        if (revoked.Contains(keyId, StringComparer.Ordinal)) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.RevokedKey,
                $"签名密钥 {keyId} 已被吊销。");
        }

        var configured = Environment.GetEnvironmentVariable("DWS_LICENSE_PUBLIC_KEY");
        if (!string.IsNullOrWhiteSpace(configured)) {
            publicKey = File.Exists(configured) ? File.ReadAllText(configured) : configured;
            return LicenseOperationResult.Success("已加载环境信任根。");
        }

        var candidates = new[] {
            Path.Combine(trustDirectory, $"{keyId}.pem"),
            Path.Combine(AppContext.BaseDirectory, DefaultPublicKeyFileName)
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.TrustRootMissing,
                "未配置授权信任公钥。请部署 license-trust/<keyId>.pem，或设置 DWS_LICENSE_PUBLIC_KEY。");
        }

        publicKey = File.ReadAllText(path);
        return LicenseOperationResult.Success("已加载应用信任根。");
    }

    private static LicenseOperationResult ValidateTimes(
        LicenseClaims claims,
        DateTimeOffset now) {
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(claims.IssuedAtUnixSeconds);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(claims.ExpiresAtUnixSeconds);
        if (expiresAt <= issuedAt) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.InvalidPayload,
                "授权有效期无效。");
        }

        if (issuedAt - now > TimeSpan.FromMinutes(10)) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.ClockRollback,
                "本机时间早于授权签发时间，疑似发生时钟回拨。");
        }

        if (expiresAt <= now) {
            return LicenseOperationResult.Failure(
                LicenseErrorCode.Expired,
                "授权已过期。");
        }

        return LicenseOperationResult.Success("授权时间有效。");
    }

    private static RSA ImportRsa(string key, bool requirePrivateKey) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new CryptographicException("RSA 密钥为空。");
        }

        var rsa = RSA.Create();
        try {
            if (key.Contains("<RSAKeyValue>", StringComparison.Ordinal)) {
                rsa.FromXmlString(key);
            }
            else {
                rsa.ImportFromPem(key);
            }

            if (requirePrivateKey && rsa.ExportParameters(true).D is null) {
                throw new CryptographicException("签发操作需要 RSA 私钥。");
            }

            return rsa;
        }
        catch {
            rsa.Dispose();
            throw;
        }
    }

    private static bool FixedTimeTextEquals(string left, string right) {
        var leftBytes = Encoding.UTF8.GetBytes(left.Trim().ToUpperInvariant());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim().ToUpperInvariant());
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static void TryAppendWmiValues(
        ICollection<string> values,
        string query,
        string property) {
        try {
            using var searcher = new ManagementObjectSearcher(query);
            using var collection = searcher.Get();
            foreach (var item in collection.OfType<ManagementObject>()) {
                if (item[property]?.ToString() is { Length: > 0 } value) {
                    values.Add(value);
                }
            }
        }
        catch (ManagementException) {
            // WMI 在裁剪环境中可能不可用；其余稳定标识仍可形成机器指纹。
        }
    }

    private static void TryAppendNonUsbDiskSerials(ICollection<string> values) {
        try {
            using var searcher = new ManagementObjectSearcher(
                "SELECT InterfaceType, SerialNumber FROM Win32_DiskDrive");
            using var collection = searcher.Get();
            foreach (var item in collection.OfType<ManagementObject>()) {
                var interfaceType = item["InterfaceType"]?.ToString();
                var serial = item["SerialNumber"]?.ToString();
                if (!string.Equals(interfaceType, "USB", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(serial)) {
                    values.Add(serial);
                }
            }
        }
        catch (ManagementException) {
            // 同上：不输出硬件标识，也不吞没授权文件错误。
        }
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> value) {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool IsValidKeyId(string keyId) {
        return !string.IsNullOrWhiteSpace(keyId) &&
               keyId.Length <= 128 &&
               keyId.All(static character =>
                   char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
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

    private static void ValidateOutputPath(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentException("授权文件输出路径不能为空。", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (string.IsNullOrWhiteSpace(directory)) {
            throw new ArgumentException("授权文件输出目录不能为空。", nameof(filePath));
        }

        Directory.CreateDirectory(directory);
    }

    private static void WriteAtomically(string filePath, byte[] bytes) {
        var fullPath = Path.GetFullPath(filePath);
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
        [property: JsonPropertyOrder(8)] string Remarks) {
        public static LicenseClaims From(LicenseData data) {
            var creation = ToLocalOffset(data.CreationTime);
            var expiration = ToLocalOffset(data.ExpirationDate);
            return new LicenseClaims(
                data.LicenseCode,
                data.MachineCode,
                data.UserName,
                creation.ToUnixTimeSeconds(),
                expiration.ToUnixTimeSeconds(),
                data.IsAvailable,
                data.MaxBindingScannerCount,
                data.AppliedTemplateName,
                data.Remarks);
        }

        public LicenseData ToLicenseData() {
            return new LicenseData {
                LicenseCode = LicenseCode,
                MachineCode = MachineCode,
                UserName = UserName,
                CreationTime = DateTimeOffset
                    .FromUnixTimeSeconds(IssuedAtUnixSeconds)
                    .LocalDateTime,
                ExpirationDate = DateTimeOffset
                    .FromUnixTimeSeconds(ExpiresAtUnixSeconds)
                    .LocalDateTime,
                IsAvailable = IsAvailable,
                MaxBindingScannerCount = MaxBindingScannerCount,
                AppliedTemplateName = AppliedTemplateName,
                Remarks = Remarks
            };
        }

        private static DateTimeOffset ToLocalOffset(DateTime value) {
            var local = value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Local)
                : value.ToLocalTime();
            return new DateTimeOffset(local);
        }
    }
}
