using System.Text.Json;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>使用可审计证据锁定第二批架构实施结果。</summary>
public sealed class StageTwoImplementationRegisterTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>要求台账包含恰好两百个连续编号且完整验证的条目。</summary>
    [Fact]
    public void Machine_register_closes_exactly_two_hundred_verified_items()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "ArchitectureStage2Register.json")));
        JsonElement[] items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(200, items.Length);
        Assert.Equal(200, items.Select(item => item.GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(200, items.Select(item => item.GetProperty("title").GetString()).Distinct().Count());

        for (int index = 0; index < items.Length; index++)
        {
            JsonElement item = items[index];
            Assert.Equal($"S2-{index + 1:000}", item.GetProperty("id").GetString());
            Assert.Equal("Verified", item.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("category").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("title").GetString()));

            JsonElement[] evidence = item.GetProperty("evidence").EnumerateArray().ToArray();
            Assert.NotEmpty(evidence);
            foreach (JsonElement evidencePath in evidence)
            {
                Assert.True(
                    File.Exists(Resolve(evidencePath.GetString()!)),
                    $"Missing evidence for {item.GetProperty("id").GetString()}: {evidencePath.GetString()}");
            }

            string verification = item.GetProperty("verification").GetString()!;
            string verificationPath = Resolve(verification);
            Assert.True(File.Exists(verificationPath), $"Missing verification: {verification}");
            Assert.Contains("[Fact]", File.ReadAllText(verificationPath), StringComparison.Ordinal);
        }
    }

    /// <summary>要求可读台账公开机器台账中的每一个编号。</summary>
    [Fact]
    public void Readable_register_covers_every_stage_two_identifier()
    {
        string readable = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "architecture",
            "implementation-register-stage2-200.md"));

        for (int index = 1; index <= 200; index++)
        {
            Assert.Contains($"S2-{index:000}", readable, StringComparison.Ordinal);
        }
    }

    private static string Resolve(string relativePath) =>
        Path.Combine(
            RepositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
