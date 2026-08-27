using System.Security.Cryptography;
using System.Text.Json;
using JayTom.Dws.Application.Deployment;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>验证 win-x64 原生资产清单、完整性和重复文件策略。</summary>
public sealed class DeploymentAssetTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证受支持厂商入口文件的长度与 SHA-256 均和清单一致。</summary>
    [Fact]
    public async Task Native_dependency_manifest_matches_tracked_assets()
    {
        await using FileStream manifestStream = File.OpenRead(Path.Combine(
            RepositoryRoot,
            "eng",
            "native-assets.win-x64.json"));
        NativeDependencyManifest manifest = (await JsonSerializer
            .DeserializeAsync<NativeDependencyManifest>(manifestStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }))!;
        string cameraRoot = Path.Combine(RepositoryRoot, "JayTom.Dws.Camera");

        Assert.Equal("win-x64", manifest.Rid);
        Assert.Equal(10, manifest.Assets.Count);
        foreach (NativeDependencyAsset asset in manifest.Assets)
        {
            string sourcePath = Path.Combine(cameraRoot, asset.SourceRelativePath);
            var sourceInfo = new FileInfo(sourcePath);
            Assert.True(sourceInfo.Exists, asset.Name);
            Assert.Equal(asset.Length, sourceInfo.Length);
            await using FileStream sourceStream = File.OpenRead(sourcePath);
            byte[] hash = await SHA256.HashDataAsync(sourceStream);
            // DWS-HEX-COMPACT: SHA-256 清单按协议约定使用无分隔小写十六进制。
            Assert.Equal(asset.Sha256, Convert.ToHexStringLower(hash));
        }
    }

    /// <summary>验证被篡改的依赖在启动前被拒绝。</summary>
    [Fact]
    public async Task Native_dependency_validator_rejects_tampered_files()
    {
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"dws-native-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            string assetPath = Path.Combine(temporaryDirectory, "sample.dll");
            await File.WriteAllBytesAsync(assetPath, [1, 2, 3, 4]);
            string manifestPath = Path.Combine(temporaryDirectory, "manifest.json");
            // DWS-HEX-COMPACT: 测试清单按协议约定使用无分隔小写十六进制。
            string originalHash = Convert.ToHexStringLower(SHA256.HashData([1, 2, 3, 4]));
            var manifest = new NativeDependencyManifest
            {
                Rid = "win-x64",
                Assets = [new NativeDependencyAsset
                {
                    Name = "sample",
                    RelativePath = "sample.dll",
                    SourceRelativePath = "sample.dll",
                    Version = "1.0.0",
                    Length = 4,
                    Sha256 = originalHash
                }]
            };
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest));

            var validator = new NativeDependencyValidator();
            var initialResult = await validator.ValidateAsync(temporaryDirectory, manifestPath);
            await File.WriteAllBytesAsync(assetPath, [4, 3, 2, 1]);
            var result = await validator
                .ValidateAsync(temporaryDirectory, manifestPath);

            Assert.True(initialResult.IsSuccess);
            Assert.False(result.IsSuccess);
            Assert.Equal("native.hash.mismatch", result.ErrorCode);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }

    /// <summary>验证源码中的原生二进制不存在字节级重复副本。</summary>
    [Fact]
    public void Tracked_native_assets_must_not_contain_byte_identical_duplicates()
    {
        string cameraRoot = Path.Combine(RepositoryRoot, "JayTom.Dws.Camera");
        string[] nativePaths = Directory
            .EnumerateFiles(cameraRoot, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".dll" or ".exe")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToArray();
        // DWS-HEX-COMPACT: SHA-256 去重比较使用无分隔小写十六进制。
        var duplicateGroups = nativePaths
            .GroupBy(path => new FileInfo(path).Length)
            .Where(group => group.Skip(1).Any())
            .SelectMany(group => group)
            .Select(path => new
            {
                Path = path,
                Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))
            })
            .GroupBy(item => item.Hash, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .Select(group => string.Join(", ", group.Select(item =>
                Path.GetRelativePath(cameraRoot, item.Path))))
            .ToArray();

        Assert.Empty(duplicateGroups);
    }

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("无法定位仓库根目录。");
    }
}
