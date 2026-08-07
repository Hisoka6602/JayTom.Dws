using System.IO;
using System.Media;
using System.Reflection;
using System.Speech.Synthesis;
using System.Reflection.Metadata;
using System.Collections.Concurrent;

namespace JayTom.Dws.Plugin.Speech {

    public class Speech : ISpeech {
        private static SpeechSynthesizer? _synthesizer;
        private static readonly ConcurrentDictionary<string, byte[]> SoundDictionary = new();
        private static readonly System.Threading.Lock SynthesizerLock = new();

        public Speech() {
            _synthesizer ??= new() {
                Volume = 100,
                Rate = 0
            };
        }

        public Task Speak(string speechText, CancellationToken token = default) {
            return Task.Run(() => {
                lock (SynthesizerLock) {
                    token.ThrowIfCancellationRequested();
                    _synthesizer?.SpeakAsyncCancelAll();
                    _synthesizer?.Speak(speechText);
                }
            }, token);
        }

        public async Task PlayStream(Stream stream) {
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            await PlayByteFile(copy.ToArray());
        }

        public async Task PlayByteFile(byte[] file) {
            try {
                await Task.Run(() => {
                    using var stream = new MemoryStream(file, writable: false);
                    using var player = new SoundPlayer(stream);
                    player.PlaySync();
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"播放声音文件异常:{e}");
            }
        }

        public async Task PlayCacheByteFile(string name, byte[] file) {
            var sound = SoundDictionary.GetOrAdd(name, file);
            await PlayByteFile(sound);
        }

        public Task PlayFile(string path) {
            return Task.Run(() => {
                if (File.Exists(path)) {
                    using var player = new SoundPlayer(path);
                    player.PlaySync();
                }
            });
        }

        public Task PlaySuccess() {
            return Task.Run(() => {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.success.wav");
                if (stream == null) {
                    return;
                }
                using var player = new SoundPlayer(stream);
                player.PlaySync();
            });
        }

        public Task PlayFail() {
            return Task.Run(() => {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("JayTom.Dws.Plugin.Sound.fail.wav");
                if (stream == null) {
                    return;
                }
                using var player = new SoundPlayer(stream);
                player.PlaySync();
            });
        }
    }
}
