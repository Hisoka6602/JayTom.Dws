using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace JayTom.Dws.Benchmarks;

/// <summary>
/// Entry point for running benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<UtilsBenchmarks>();
    }
}

/// <summary>
/// Performance benchmarks for JayTom.Dws.Utils
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class UtilsBenchmarks
{
    private const string TestPath1 = @"C:\TestPath1";
    private const string TestPath2 = @"C:\TestPath2";
    private const string TestPath3 = @"C:\TestPath3";
    private string? _originalPath;

    [GlobalSetup]
    public void Setup()
    {
        _originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_originalPath != null)
        {
            Environment.SetEnvironmentVariable("PATH", _originalPath, EnvironmentVariableTarget.Process);
        }
    }

    [Benchmark(Baseline = true)]
    public void SetPath_SinglePath()
    {
        JayTom.Dws.Utils.Utils.SetPath(TestPath1);
    }

    [Benchmark]
    public void SetPath_TwoPaths()
    {
        JayTom.Dws.Utils.Utils.SetPath(TestPath1, TestPath2);
    }

    [Benchmark]
    public void SetPath_ThreePaths()
    {
        JayTom.Dws.Utils.Utils.SetPath(TestPath1, TestPath2, TestPath3);
    }
}
