using System;
using System.IO;
using FFmpeg.AutoGen;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Nvr.Nvr {

    public class VideoDecoder : IDisposable {
        private readonly unsafe AVCodec* _pCodec;
        private readonly unsafe AVCodecContext* _pCodecContext;
        private readonly unsafe AVPacket* _pPacket;
        private readonly unsafe AVFrame** _pFrame;
        private readonly MemoryStream _videoStream;
        private volatile bool _isDecoding;

        public event EventHandler<DecodedFrameEventArgs>? FrameDecoded;

        public unsafe VideoDecoder(string videoFilePath) {
            FFmpegBinariesHelper.RegisterFFmpegBinaries();

            // 初始化 FFmpeg
            ffmpeg.avdevice_register_all();

            // 打开视频文件流
            _videoStream = new MemoryStream(File.ReadAllBytes(videoFilePath));

            // 创建解码器上下文
            _pCodec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (_pCodec == null) {
                Console.WriteLine("Codec not found");
                return;
            }

            _pCodecContext = ffmpeg.avcodec_alloc_context3(_pCodec);

            // 打开解码器
            if (ffmpeg.avcodec_open2(_pCodecContext, _pCodec, null) < 0) {
                Console.WriteLine("Could not open codec");
                return;
            }

            // 创建 AVPacket 和 AVFrame
            _pPacket = ffmpeg.av_packet_alloc();
            _pFrame = (AVFrame**)ffmpeg.av_frame_alloc();

            _isDecoding = true;
        }

        public unsafe void StartDecoding() {
            while (_isDecoding) {
                if (ffmpeg.av_read_frame((AVFormatContext*)_pCodecContext->opaque, _pPacket) < 0) {
                    // 读取到文件结尾，重新从开头读取
                    _videoStream.Position = 0;
                    continue;
                }

                // 解码视频帧
                int response = ffmpeg.avcodec_send_packet(_pCodecContext, _pPacket);
                if (response < 0) {
                    Console.WriteLine($"Error sending packet: {response}");
                    return;
                }

                while (response >= 0) {
                    response = ffmpeg.avcodec_receive_frame(_pCodecContext, (AVFrame*)_pFrame);
                    if (response == ffmpeg.AVERROR_EOF || response == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        break;

                    // 在这里处理解码后的视频帧
                    // _pFrame 中包含解码后的视频数据

                    // 发送事件通知
                    FrameDecoded?.Invoke(this, new DecodedFrameEventArgs(ConvertFrameToMemoryStream((AVFrame*)_pFrame)));
                }

                ffmpeg.av_packet_unref(_pPacket);
            }
        }

        public void StopDecoding() {
            _isDecoding = false;
        }

        public unsafe void Dispose() {
            // 释放资源
            ffmpeg.av_packet_free((AVPacket**)_pPacket);
            ffmpeg.av_frame_free(_pFrame);
            ffmpeg.avcodec_close(_pCodecContext);
            ffmpeg.avcodec_free_context((AVCodecContext**)_pCodecContext);
        }

        private unsafe MemoryStream ConvertFrameToMemoryStream(AVFrame* frame) {
            // Create a Bitmap and copy the pixel data from the AVFrame
            using (Bitmap bitmap = new Bitmap(frame->width, frame->height, frame->linesize[0], System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(frame->data[0]))) {
                // Use a JPEG encoder to encode the Bitmap to a MemoryStream
                var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }
    }

    public unsafe class DecodedFrameEventArgs : EventArgs {
        public MemoryStream StreamFrame { get; }

        public DecodedFrameEventArgs(MemoryStream streamFrame) {
            StreamFrame = streamFrame;
        }
    }
}
