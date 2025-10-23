using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JayTom.Dws.Infrastructure.Channels;

namespace JayTom.Dws.Infrastructure.Examples {
    /// <summary>
    /// 优化的后台服务示例 - 使用有界通道替代 ConcurrentQueue
    /// 
    /// 使用指南：
    /// 1. 将 ConcurrentQueue<T> 替换为 Channel<T>
    /// 2. 使用 channel.Writer.WriteAsync() 替代 queue.Enqueue()
    /// 3. 使用 await foreach 替代 TryDequeue 循环
    /// 
    /// 优势：
    /// - 防止内存泄漏（有容量限制）
    /// - 更好的异步支持
    /// - 自动背压控制
    /// </summary>
    public class OptimizedBackgroundServiceExample {
        // 旧的方式（不推荐）：
        // private ConcurrentQueue<PackageInfoModel> _insertItems = new();

        // 新的方式（推荐）：
        private readonly Channel<PackageInfoModel> _insertChannel;
        private readonly Channel<CameraImageInfo> _imageChannel;
        private readonly CancellationTokenSource _cts = new();

        public OptimizedBackgroundServiceExample() {
            // 创建有界通道
            _insertChannel = BoundedChannelFactory.CreateDataChannel<PackageInfoModel>(capacity: 1000);
            _imageChannel = BoundedChannelFactory.CreateImageChannel<CameraImageInfo>(capacity: 100);

            // 启动消费者任务
            _ = Task.Run(() => ProcessInsertItemsAsync(_cts.Token));
            _ = Task.Run(() => ProcessImagesAsync(_cts.Token));
        }

        /// <summary>
        /// 添加数据到队列（生产者）
        /// </summary>
        public async Task AddPackageAsync(PackageInfoModel package, CancellationToken cancellationToken = default) {
            try {
                // 写入通道（如果满了会等待或丢弃，取决于配置）
                await _insertChannel.Writer.WriteAsync(package, cancellationToken);
            }
            catch (ChannelClosedException) {
                // 通道已关闭，记录日志
                NLog.LogManager.GetCurrentClassLogger().Warn("Insert channel is closed");
            }
        }

        /// <summary>
        /// 添加图像到队列（生产者）
        /// </summary>
        public async Task AddImageAsync(CameraImageInfo image, CancellationToken cancellationToken = default) {
            try {
                // 对于图像，使用 TryWrite 避免等待（如果满了就丢弃）
                if (!_imageChannel.Writer.TryWrite(image)) {
                    NLog.LogManager.GetCurrentClassLogger().Warn("Image channel is full, dropping image");
                }
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Failed to add image");
            }
        }

        /// <summary>
        /// 处理插入队列（消费者）
        /// </summary>
        private async Task ProcessInsertItemsAsync(CancellationToken cancellationToken) {
            try {
                // 使用 await foreach 读取通道
                await foreach (var package in _insertChannel.Reader.ReadAllAsync(cancellationToken)) {
                    try {
                        // 处理包裹数据
                        await ProcessPackageAsync(package, cancellationToken);
                    }
                    catch (Exception ex) {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, "Failed to process package");
                    }
                }
            }
            catch (OperationCanceledException) {
                // 正常取消
            }
        }

        /// <summary>
        /// 处理图像队列（消费者 - 批处理）
        /// </summary>
        private async Task ProcessImagesAsync(CancellationToken cancellationToken) {
            try {
                var batch = new System.Collections.Generic.List<CameraImageInfo>();
                const int batchSize = 10;
                var timeout = TimeSpan.FromMilliseconds(100);

                while (!cancellationToken.IsCancellationRequested) {
                    batch.Clear();

                    // 收集一批图像
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeout);

                    try {
                        while (batch.Count < batchSize && 
                               await _imageChannel.Reader.WaitToReadAsync(cts.Token)) {
                            if (_imageChannel.Reader.TryRead(out var image)) {
                                batch.Add(image);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                        // 超时，处理已收集的批次
                    }

                    // 批量处理图像
                    if (batch.Count > 0) {
                        await ProcessImageBatchAsync(batch, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) {
                // 正常取消
            }
        }

        /// <summary>
        /// 处理单个包裹
        /// </summary>
        private async Task ProcessPackageAsync(PackageInfoModel package, CancellationToken cancellationToken) {
            // 实现包裹处理逻辑
            await Task.Delay(10, cancellationToken); // 示例
        }

        /// <summary>
        /// 批量处理图像
        /// </summary>
        private async Task ProcessImageBatchAsync(
            System.Collections.Generic.List<CameraImageInfo> images, 
            CancellationToken cancellationToken) {
            // 实现批量图像处理逻辑
            foreach (var image in images) {
                // 处理图像
                await Task.Delay(5, cancellationToken); // 示例
            }
        }

        /// <summary>
        /// 停止服务并清理资源
        /// </summary>
        public async Task StopAsync() {
            // 通知生产者停止写入
            _insertChannel.Writer.Complete();
            _imageChannel.Writer.Complete();

            // 取消消费者任务
            _cts.Cancel();

            // 等待所有数据处理完成
            await _insertChannel.Reader.Completion;
            await _imageChannel.Reader.Completion;

            _cts.Dispose();
        }

        // 示例数据类
        public class PackageInfoModel {
            public int Id { get; set; }
            public string? Barcode { get; set; }
        }

        public class CameraImageInfo {
            public byte[]? ImageData { get; set; }
            public string? CameraId { get; set; }
        }
    }
}
