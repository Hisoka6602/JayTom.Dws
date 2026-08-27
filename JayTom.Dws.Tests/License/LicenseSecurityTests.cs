using System.Text.Json.Nodes;
using JayTom.Dws.License;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.License;

/// <summary>验证 v2 授权文件的签名、机器绑定、大小限制和密钥边界。</summary>
public sealed class LicenseSecurityTests {
    private static readonly DateTimeOffset Now = new(
        2026,
        8,
        14,
        8,
        0,
        0,
        TimeSpan.Zero);

    /// <summary>合法签发内容应能由对应公钥校验并恢复声明。</summary>
    [Fact]
    public void Signed_license_round_trips_with_public_key_only() {
        using var directory = TemporaryDirectory.Create("dws-license-tests");
        LicenseManager.GenerateKeyPair(out var publicKey, out var privateKey);
        var path = Path.Combine(directory.Path, "license.dws");
        var machineCode = new string('A', 64);

        var signed = LicenseManager.GenerateSignedAuthorizationFile(
            CreateLicense(machineCode),
            privateKey,
            LicenseManager.ComputeKeyId(publicKey),
            path);
        var validated = LicenseManager.ValidateAuthorizationFile(
            path,
            publicKey,
            machineCode,
            new FixedTimeProvider(Now),
            out var data);

        Assert.True(signed.IsSuccess);
        Assert.True(validated.IsSuccess);
        Assert.Equal("license-001", data?.LicenseCode);
    }

    /// <summary>签名载荷被篡改后必须返回稳定的签名错误。</summary>
    [Fact]
    public void Tampered_payload_is_rejected() {
        using var directory = TemporaryDirectory.Create("dws-license-tests");
        LicenseManager.GenerateKeyPair(out var publicKey, out var privateKey);
        var path = Path.Combine(directory.Path, "license.dws");
        var machineCode = new string('B', 64);
        LicenseManager.GenerateSignedAuthorizationFile(
            CreateLicense(machineCode),
            privateKey,
            LicenseManager.ComputeKeyId(publicKey),
            path);

        var envelope = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var payload = envelope["payload"]!.GetValue<string>();
        envelope["payload"] = (payload[0] == 'A' ? 'B' : 'A') + payload[1..];
        File.WriteAllText(path, envelope.ToJsonString());

        var result = LicenseManager.ValidateAuthorizationFile(
            path,
            publicKey,
            machineCode,
            new FixedTimeProvider(Now),
            out _);

        Assert.False(result.IsSuccess);
        Assert.Equal(LicenseErrorCode.InvalidSignature, result.ErrorCode);
    }

    /// <summary>错公钥和超长文件应在解析声明前被拒绝。</summary>
    [Fact]
    public void Wrong_key_and_oversized_file_are_rejected_early() {
        using var directory = TemporaryDirectory.Create("dws-license-tests");
        LicenseManager.GenerateKeyPair(out var publicKey, out var privateKey);
        LicenseManager.GenerateKeyPair(out var wrongPublicKey, out _);
        var path = Path.Combine(directory.Path, "license.dws");
        var machineCode = new string('C', 64);
        LicenseManager.GenerateSignedAuthorizationFile(
            CreateLicense(machineCode),
            privateKey,
            LicenseManager.ComputeKeyId(publicKey),
            path);

        var wrongKeyResult = LicenseManager.ValidateAuthorizationFile(
            path,
            wrongPublicKey,
            machineCode,
            new FixedTimeProvider(Now),
            out _);
        File.WriteAllBytes(path, new byte[1024 * 1024 + 1]);
        var oversizedResult = LicenseManager.ValidateAuthorizationFile(
            path,
            publicKey,
            machineCode,
            new FixedTimeProvider(Now),
            out _);

        Assert.Equal(LicenseErrorCode.UntrustedKey, wrongKeyResult.ErrorCode);
        Assert.Equal(LicenseErrorCode.FileTooLarge, oversizedResult.ErrorCode);
    }

    /// <summary>密钥标识不能包含目录分隔符。</summary>
    [Fact]
    public void Signing_key_identifier_cannot_escape_trust_directory() {
        using var directory = TemporaryDirectory.Create("dws-license-tests");
        LicenseManager.GenerateKeyPair(out _, out var privateKey);

        var result = LicenseManager.GenerateSignedAuthorizationFile(
            CreateLicense(new string('D', 64)),
            privateKey,
            "../../outside",
            Path.Combine(directory.Path, "license.dws"));

        Assert.Equal(LicenseErrorCode.InvalidKey, result.ErrorCode);
    }

    private static LicenseData CreateLicense(string machineCode) => new() {
        LicenseCode = "license-001",
        UserName = "test-user",
        MachineCode = machineCode,
        CreationTime = Now.LocalDateTime,
        ExpirationDate = Now.AddDays(30).LocalDateTime,
        IsAvailable = true,
        MaxBindingScannerCount = 2,
        AppliedTemplateName = "test",
        Remarks = "security regression"
    };
}
