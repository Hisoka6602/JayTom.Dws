using System.Media;
using System.Reflection;
using System.Speech.Synthesis;
using System.Collections.Concurrent;

namespace JayTom.Dws.Plugin.Speech {

    public class Speech : ISpeech {
        private static SpeechSynthesizer? _synthesizer;
        private static ConcurrentDictionary<string, byte[]>? _soundDictionary = new();
        private SemaphoreSlim _playSlim = new(1);
        private SemaphoreSlim _takeSlim = new(1);

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
                new System.Media.SoundPlayer(stream)?.PlaySync();
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

                await Task.Delay(100);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
                // ignored
            }
            finally {
                _playSlim.Release();
            }
        }

        public async void PlayCacheByteFile(string name, byte[] file) {
            try {
                await _takeSlim.WaitAsync();
                if (_soundDictionary != null) {
                    _soundDictionary.TryGetValue(name, out var sound);
                    if (sound is not null) {
                        PlayByteFile(sound);
                    }
                    else {
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
    }
}