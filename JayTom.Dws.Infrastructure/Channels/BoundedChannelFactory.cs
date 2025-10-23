using System;
using System.Threading.Channels;

namespace JayTom.Dws.Infrastructure.Channels {
    /// <summary>
    /// 有界通道工厂 - 创建具有容量限制的通道，防止内存泄漏
    /// 用于替代无界的 ConcurrentQueue
    /// </summary>
    public static class BoundedChannelFactory {
        /// <summary>
        /// 创建有界通道 - 当满时等待
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="capacity">容量</param>
        /// <param name="singleReader">是否只有一个读取者</param>
        /// <param name="singleWriter">是否只有一个写入者</param>
        /// <returns>有界通道</returns>
        public static Channel<T> CreateWaitChannel<T>(
            int capacity = 1000,
            bool singleReader = true,
            bool singleWriter = false) {
            
            return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = singleReader,
                SingleWriter = singleWriter
            });
        }

        /// <summary>
        /// 创建有界通道 - 当满时丢弃最旧的
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="capacity">容量</param>
        /// <param name="singleReader">是否只有一个读取者</param>
        /// <param name="singleWriter">是否只有一个写入者</param>
        /// <returns>有界通道</returns>
        public static Channel<T> CreateDropOldestChannel<T>(
            int capacity = 1000,
            bool singleReader = true,
            bool singleWriter = false) {
            
            return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = singleReader,
                SingleWriter = singleWriter
            });
        }

        /// <summary>
        /// 创建有界通道 - 当满时丢弃最新的
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="capacity">容量</param>
        /// <param name="singleReader">是否只有一个读取者</param>
        /// <param name="singleWriter">是否只有一个写入者</param>
        /// <returns>有界通道</returns>
        public static Channel<T> CreateDropWriteChannel<T>(
            int capacity = 1000,
            bool singleReader = true,
            bool singleWriter = false) {
            
            return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = singleReader,
                SingleWriter = singleWriter
            });
        }

        /// <summary>
        /// 创建无界通道（仅用于必须无界的场景）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="singleReader">是否只有一个读取者</param>
        /// <param name="singleWriter">是否只有一个写入者</param>
        /// <returns>无界通道</returns>
        public static Channel<T> CreateUnboundedChannel<T>(
            bool singleReader = true,
            bool singleWriter = false) {
            
            return Channel.CreateUnbounded<T>(new UnboundedChannelOptions {
                SingleReader = singleReader,
                SingleWriter = singleWriter
            });
        }

        /// <summary>
        /// 图像处理通道 - 容量较小，防止内存溢出
        /// </summary>
        public static Channel<T> CreateImageChannel<T>(int capacity = 100) {
            return CreateDropOldestChannel<T>(capacity, singleReader: true, singleWriter: false);
        }

        /// <summary>
        /// 数据处理通道 - 容量中等，使用等待模式
        /// </summary>
        public static Channel<T> CreateDataChannel<T>(int capacity = 1000) {
            return CreateWaitChannel<T>(capacity, singleReader: true, singleWriter: false);
        }

        /// <summary>
        /// 日志通道 - 容量较大，丢弃旧日志
        /// </summary>
        public static Channel<T> CreateLogChannel<T>(int capacity = 10000) {
            return CreateDropOldestChannel<T>(capacity, singleReader: true, singleWriter: false);
        }
    }
}
