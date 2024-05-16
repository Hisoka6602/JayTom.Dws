using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

internal class Program {
    private static readonly SemaphoreSlim _takePackageSlim = new(1);
    private static ConcurrentDictionary<long, int> _packageSubmissionPushItems = new();
    private static MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    private static async Task Main(string[] args) {
        var outInts = new List<int>();
        Console.WriteLine("Hello, World!");
        var list = Enumerable.Range(0, 100).ToList();
        for (int i = 0; i < 10; i++) {
            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (num) => {
                try {
                    await _takePackageSlim.WaitAsync();
                    await Task.Delay(5);
                    var unixTimeMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (!cache.TryGetValue(unixTimeMilliseconds, out _)) {
                        cache.Set(num, true, TimeSpan.FromMilliseconds(30));
                        var tryAdd = _packageSubmissionPushItems.TryAdd(unixTimeMilliseconds, num);
                        if (!tryAdd) {
                            Console.WriteLine($"添加失败:{num}");
                        }
                    }
                }
                finally {
                    _takePackageSlim.Release();
                }
            });

            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (num) => {
                try {
                    _takePackageSlim.WaitAsync().GetAwaiter().GetResult();
                    var (key, value) = _packageSubmissionPushItems.FirstOrDefault(f => f.Value.Equals(num));
                    if (key > 0 && value % 10 > 0) {
                        var tryRemove = _packageSubmissionPushItems.TryRemove(key, out var outValue);
                        if (!tryRemove) {
                            Console.WriteLine($"移除失败:{outValue}");
                        }
                    }
                    Test(num, outInts);
                }
                finally {
                    _takePackageSlim.Release();
                }
            });
        }

        Console.WriteLine("完成");
        await Task.Delay(9000);
        Console.WriteLine(JsonConvert.SerializeObject(_packageSubmissionPushItems.OrderByDescending(o => o.Value)));
        Console.ReadLine();
    }

    private static async void Test(int i, List<int> dataInts) {
        await Task.Delay(5000);
        try {
            /*var (key, value) = _packageSubmissionPushItems.FirstOrDefault(f => f.Value.Equals(i));
            if (key > 0) {
                var tryRemove = _packageSubmissionPushItems.TryRemove(key, out var outValue);
                if (!tryRemove) {
                    Console.WriteLine($"移除失败:{outValue}");
                }

                /*await Task.Delay(10);
                var tryAdd = _packageSubmissionPushItems.TryAdd(DateTimeOffset.Now.ToUnixTimeMilliseconds(), 0 - outValue);
                if (!tryAdd) {
                    Console.WriteLine($"添加失败:{0 - outValue}");
                }#1#
            }*/

            dataInts.Add(i);
        }
        finally {
        }

        Console.WriteLine(i);
    }
}