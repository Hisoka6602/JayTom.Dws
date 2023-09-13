using System.Media;
using System.Reflection;
using System.Speech.Synthesis;
using System.Reflection.Metadata;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Plugin.Speech {

    public class Speech : ISpeech {
        private static SpeechSynthesizer? _synthesizer;
        private static ConcurrentDictionary<string, byte[]>? _soundDictionary = new();
        private static SemaphoreSlim _playSlim = new(1);
        private static SemaphoreSlim _takeSlim = new(1);

        public Speech() {
            _synthesizer ??= new() {
                Volume = 100,
                Rate = 0
            };
        }

        public async void Speak(string speechText, CancellationToken token = default) {
            try {
                await _playSlim.WaitAsync(token);
                _synthesizer?.SpeakAsyncCancelAll();
                _synthesizer?.SpeakAsync(speechText);
            }
            finally {
                _playSlim.Release();
            }
        }

        public async void PlayStream(Stream stream) {
            try {
                await _playSlim.WaitAsync();
                new SoundPlayer(stream)?.PlaySync();
            }
            finally {
                _playSlim.Release();
            }
        }

        public async void PlayByteFile(byte[] file) {
            try {
                await _playSlim.WaitAsync();
                using (var stream = new MemoryStream(file)) {
                    using (var player = new SoundPlayer(stream)) {
                        player?.PlaySync();
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
            }
            finally {
                _playSlim.Release();
            }
        }

        public unsafe void PlaySoundFromMemory(MemoryLocation memoryLocation) {
            try {
                if (!memoryLocation.Handle.IsAllocated || memoryLocation.MemoryPtr == IntPtr.Zero) {
                    throw new Exception("内存未锁定或指针为空");
                }
                NLog.LogManager.GetCurrentClassLogger().Error($"{memoryLocation.MemoryPtr:x8}");
                using (var stream = new UnmanagedMemoryStream((byte*)memoryLocation.MemoryPtr.ToPointer(), memoryLocation.Data.Length)) {
                    using (var player = new SoundPlayer(stream)) {
                        player?.PlaySync();
                    }
                }
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
                        PlayByteFile(sound);
                    }
                    else {
                        /*var location = new MemoryLocation() {
                            Data = file,
                            Handle = GCHandle.Alloc(file, GCHandleType.Pinned),
                        };
                        location.MemoryPtr = location.Handle.AddrOfPinnedObject();*/
                        _soundDictionary?.TryAdd(name, file);
                        PlayByteFile(file);
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
            try {
                await _playSlim.WaitAsync();
                if (File.Exists(path)) {
                    await Task.Yield();
                    using var player = new SoundPlayer(path);
                    player.PlaySync(); // 同步播放
                }
            }
            finally {
                _playSlim.Release();
            }
        }

        public async void PlaySuccess() {
            try {
                await _playSlim.WaitAsync();
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.success.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.PlaySync(); // 同步播放
            }
            finally {
                _playSlim.Release();
            }
        }

        public async void PlayFail() {
            try {
                await _playSlim.WaitAsync();
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.fail.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.PlaySync(); // 同步播放
            }
            finally {
                _playSlim.Release();
            }
        }

        public class MemoryLocation {
            public GCHandle Handle { get; set; }
            public IntPtr MemoryPtr { get; set; }
            public byte[] Data { get; set; }
        }
    }
}