using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证关键业务队列在突发流量下不会阻塞生产者或丢弃数据。</summary>
public sealed class LosslessWorkQueueTests
{
    /// <summary>确认超过旧容量阈值时所有工作仍能按顺序出队。</summary>
    [Fact]
    public void BurstBeyondLegacyCapacity_RemainsLossless()
    {
        var queue = new LosslessWorkQueue<int>();
        const int itemCount = 12_000;
        for (var value = 0; value < itemCount; value++)
        {
            Assert.True(queue.TryEnqueue(value));
        }

        for (var expected = 0; expected < itemCount; expected++)
        {
            Assert.True(queue.TryDequeue(out var actual));
            Assert.Equal(expected, actual);
        }
        Assert.True(queue.IsEmpty);
    }
}
