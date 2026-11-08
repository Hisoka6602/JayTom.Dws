using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JayTom.Dws.LicenseTool;

/// <summary>为 2025 旧客户端生成并检查历史二进制授权，不依赖客户端运行时。</summary>
internal static class LegacyLicenseFile {
    private const int MaximumFileBytes = 1024 * 1024;

    public static void Generate(LicenseGenerationRequest request) {
        var claims = new LegacyClaims(
            request.CustomerName,
            request.ExpirationDate,
            request.MachineCode,
            TimeProvider.System.GetLocalNow().LocalDateTime,
            request.LicenseCode,
            request.IsAvailable,
            request.MaxBindingScannerCount,
            request.AppliedTemplateName,
            request.Remarks);
        // 历史协议携带解密私钥，每个文件独立生成密钥，绝不使用 v2 生产签名私钥。
        using var rsa = RSA.Create(2048);
        var payload = JsonSerializer.SerializeToUtf8Bytes(claims);
        if (payload.Length > MaximumFileBytes / 2) {
            throw new ArgumentException("旧版授权内容超过允许的最大长度。");
        }

        using var encryptedPayload = new MemoryStream();
        var blockSize = rsa.KeySize / 8 - 11;
        for (var offset = 0; offset < payload.Length; offset += blockSize) {
            encryptedPayload.Write(rsa.Encrypt(
                payload.AsSpan(offset, Math.Min(blockSize, payload.Length - offset)).ToArray(),
                RSAEncryptionPadding.Pkcs1));
        }

        using var aes = Aes.Create();
        using var encryptor = aes.CreateEncryptor();
        var privateKeyBytes = Encoding.UTF8.GetBytes(rsa.ToXmlString(includePrivateParameters: true));
        var encryptedPrivateKey = encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);
        CryptographicOperations.ZeroMemory(privateKeyBytes);
        using var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true)) {
            WriteBlock(writer, encryptedPrivateKey);
            WriteBlock(writer, aes.Key);
            WriteBlock(writer, aes.IV);
            WriteBlock(writer, encryptedPayload.ToArray());
        }

        if (output.Length > MaximumFileBytes) {
            throw new ArgumentException("旧版授权文件超过允许的最大长度。");
        }

        var fullPath = Path.GetFullPath(request.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try {
            File.WriteAllBytes(temporaryPath, output.ToArray());
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    public static LicenseValidationResult Validate(string filePath, string expectedMachineCode) {
        var file = new FileInfo(filePath);
        if (!file.Exists || file.Length is <= 0 or > MaximumFileBytes) {
            throw new InvalidDataException("授权文件不存在、为空或超过允许的最大长度。");
        }

        using var input = new MemoryStream(File.ReadAllBytes(file.FullName));
        using var reader = new BinaryReader(input);
        var encryptedPrivateKey = ReadBlock(reader, 16384);
        var key = ReadBlock(reader, 32);
        var iv = ReadBlock(reader, 16);
        var encryptedPayload = ReadBlock(reader, MaximumFileBytes);
        if (input.Position != input.Length) {
            throw new InvalidDataException("旧版授权文件包含多余数据。");
        }

        using var aes = Aes.Create();
        using var decryptor = aes.CreateDecryptor(key, iv);
        var privateKeyBytes = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
        using var rsa = RSA.Create();
        rsa.FromXmlString(Encoding.UTF8.GetString(privateKeyBytes));
        CryptographicOperations.ZeroMemory(privateKeyBytes);
        using var payload = new MemoryStream();
        var blockSize = rsa.KeySize / 8;
        for (var offset = 0; offset < encryptedPayload.Length; offset += blockSize) {
            payload.Write(rsa.Decrypt(
                encryptedPayload.AsSpan(offset, Math.Min(blockSize, encryptedPayload.Length - offset)).ToArray(),
                RSAEncryptionPadding.Pkcs1));
        }

        var claims = JsonSerializer.Deserialize<LegacyClaims>(payload.ToArray())
                     ?? throw new InvalidDataException("旧版授权声明为空。");
        var now = TimeProvider.System.GetLocalNow().LocalDateTime;
        if (!string.Equals(claims.MachineCode, expectedMachineCode, StringComparison.Ordinal)) {
            throw new InvalidDataException("机器码不匹配。");
        }
        if (claims.ExpirationDate <= now) {
            throw new InvalidDataException("授权已过期。");
        }
        if (claims.CreationTime - now >= TimeSpan.FromMinutes(10)) {
            throw new InvalidDataException("授权时间异常。");
        }
        if (!claims.IsAvailable) {
            throw new InvalidDataException("授权码已冻结。");
        }

        return new LicenseValidationResult(string.Empty, claims.UserName, claims.ExpirationDate, claims.IsAvailable);
    }

    private static void WriteBlock(BinaryWriter writer, byte[] bytes) {
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static byte[] ReadBlock(BinaryReader reader, int maximumLength) {
        var length = reader.ReadInt32();
        if (length <= 0 || length > maximumLength || length > reader.BaseStream.Length - reader.BaseStream.Position) {
            throw new InvalidDataException("旧版授权数据块长度无效。");
        }

        return reader.ReadBytes(length);
    }

    // 属性名称与顺序保持旧版 LicenseData 的 PascalCase JSON 契约。
    private sealed record LegacyClaims(
        string UserName,
        DateTime ExpirationDate,
        string MachineCode,
        DateTime CreationTime,
        string LicenseCode,
        bool IsAvailable,
        int MaxBindingScannerCount,
        string AppliedTemplateName,
        string Remarks);
}
