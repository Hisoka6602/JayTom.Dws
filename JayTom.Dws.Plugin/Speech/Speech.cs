using System.IO;
using System.Media;
using System.Reflection;
using System.Speech.Synthesis;
using System.Reflection.Metadata;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Plugin.Speech {

    public class Speech : ISpeech {
        private static SpeechSynthesizer? _synthesizer;
        private static ConcurrentDictionary<string, MemoryLocation>? _soundDictionary = new();
        private static SemaphoreSlim _takeSlim = new(1);

        public Speech() {
            _synthesizer ??= new() {
                Volume = 100,
                Rate = 0
            };
        }

        public async void Speak(string speechText, CancellationToken token = default) {
            await Task.Factory.StartNew(() => {
                _synthesizer?.SpeakAsyncCancelAll();
                _synthesizer?.SpeakAsync(speechText);
            }, token);
        }

        public async void PlayStream(Stream stream) {
            await Task.Factory.StartNew(() => {
                new SoundPlayer(stream)?.Play();
            });
        }

        public async void PlayByteFile(byte[] file) {
            try {
                await Task.Factory.StartNew(() => {
                    using (var stream = new MemoryStream(file)) {
                        using (var player = new SoundPlayer(stream)) {
                            player?.Play();
                        }
                    }
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
            }
        }

        public unsafe void PlaySoundFromMemory(MemoryLocation memoryLocation) {
            try {
                Task.Factory.StartNew(() => {
                    if (!memoryLocation.Handle.IsAllocated || memoryLocation.MemoryPtr == IntPtr.Zero) {
                        throw new Exception("内存未锁定或指针为空");
                    }
                    using (var stream = new UnmanagedMemoryStream((byte*)memoryLocation.MemoryPtr.ToPointer(), memoryLocation.Data.Length)) {
                        using (var player = new SoundPlayer(stream)) {
                            player?.Play();
                        }
                    }
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
            }
        }

        public async Task PlayCacheByteFile(string name, byte[] file) {
            try {
                await _takeSlim.WaitAsync();
                if (_soundDictionary != null) {
                    _soundDictionary.TryGetValue(name, out var sound);
                    if (sound is not null) {
                        PlaySoundFromMemory(sound);
                    }
                    else {
                        var location = new MemoryLocation() {
                            Data = file,
                            Handle = GCHandle.Alloc(file, GCHandleType.Pinned),
                        };
                        location.MemoryPtr = location.Handle.AddrOfPinnedObject();
                        _soundDictionary?.TryAdd(name, location);
                        PlaySoundFromMemory(location);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
            }
            finally {
                _takeSlim.Release();
            }
        }

        public async void PlayFile(string path) {
            await Task.Factory.StartNew(() => {
                if (File.Exists(path)) {
                    using var player = new SoundPlayer(path);
                    player.Play(); // 同步播放
                }
            });
        }

        public async void PlaySuccess() {
            await Task.Factory.StartNew(async () => {
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.success.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.Play(); // 同步播放
            });
        }

        public async void PlayFail() {
            await Task.Factory.StartNew(async () => {
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.fail.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.Play(); // 同步播放
            });
        }

        public class MemoryLocation {
            public GCHandle Handle { get; set; }
            public IntPtr MemoryPtr { get; set; }
            public byte[] Data { get; set; }
        }
    }
}