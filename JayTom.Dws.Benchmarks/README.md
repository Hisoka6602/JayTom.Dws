# Performance Benchmarks

This project contains performance benchmarks for JayTom.Dws using BenchmarkDotNet.

## Running Benchmarks

To run the benchmarks:

```bash
cd JayTom.Dws.Benchmarks
dotnet run -c Release
```

## Current Benchmarks

### UtilsBenchmarks

Benchmarks for the `JayTom.Dws.Utils.Utils` class:

- **SetPath_SinglePath**: Baseline - measures performance of adding a single path to environment variable
- **SetPath_TwoPaths**: measures performance of adding two paths
- **SetPath_ThreePaths**: measures performance of adding three paths

## Benchmark Results

Results will be generated in the `BenchmarkDotNet.Artifacts` directory after running the benchmarks.

## Adding New Benchmarks

1. Create a new class with the `[MemoryDiagnoser]` attribute
2. Add benchmark methods with the `[Benchmark]` attribute
3. Use `[GlobalSetup]` and `[GlobalCleanup]` for initialization and cleanup
4. Mark one method as `[Benchmark(Baseline = true)]` for comparison

Example:

```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MyBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize resources
    }

    [Benchmark(Baseline = true)]
    public void MyBaselineMethod()
    {
        // Baseline implementation
    }

    [Benchmark]
    public void MyOptimizedMethod()
    {
        // Optimized implementation
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up resources
    }
}
```

## Interpreting Results

- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Ratio**: Performance relative to baseline
- **Gen0/Gen1/Gen2**: Garbage collection statistics
- **Allocated**: Memory allocated per operation
