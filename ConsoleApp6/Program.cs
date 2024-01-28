using S7.Net;
using System.Threading;

internal class Program {
    private static CancellationTokenSource? _cancellationTokenSource;

    private static async Task Main(string[] args) {
        var ip = "127.0.0.1";
        //超时时间
        var readTimeout = 200;
        Plc plc = new Plc(CpuType.S7300, ip, 0, 2);

        await plc.OpenAsync();

        plc.ReadTimeout = readTimeout;

        //启动循环线程
        var _monitorThread = Task.Run(async () => {
            var maxByteId = 255;
            var indexByteId = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            while (!_cancellationTokenSource.IsCancellationRequested) {
                await Task.Delay(30);
                try {
                    if (indexByteId > maxByteId) {
                        indexByteId = 0;
                    }
                    if (plc is not null) {
                        var readBytesAsync =
                            await plc.ReadBytesAsync(DataType.DataBlock, 1, indexByteId, 1);
                        var firstOrDefault = readBytesAsync?.FirstOrDefault();
                        if (firstOrDefault?.Equals(0) == true) {
                            Console.WriteLine($"格口Id:[{indexByteId}]:正常,{firstOrDefault}");
                        }
                        else {
                            Console.WriteLine($"格口Id:[{indexByteId}]:锁格,{firstOrDefault}");
                        }
                    }
                }
                catch (Exception e) {
                }
                finally {
                    indexByteId++;
                }
            }
        });
        /*var readBytesAsync = await plc.ReadBytesAsync(DataType.DataBlock, 1, 0, 1);
        Console.WriteLine(readBytesAsync);
        await plc.WriteBytesAsync(DataType.DataBlock, 1, 0, new byte[] { 0x03 });
        Console.WriteLine("Hello, World!");*/
        Console.ReadLine();
    }
}