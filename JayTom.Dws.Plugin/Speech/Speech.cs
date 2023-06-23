using System.Media;
using System.Reflection;
using System.Speech.Synthesis;

namespace JayTom.Dws.Plugin.Speech {

    public class Speech : ISpeech {
        private static SpeechSynthesizer? _synthesizer;

        public Speech() {
            _synthesizer ??= new() {
                Volume = 100,
                Rate = 0
            };
        }

        public void Speak(string speechText, CancellationToken token = default) {
            _synthesizer?.SpeakAsyncCancelAll();
            _synthesizer?.SpeakAsync(speechText);
        }

        public async void PlayStream(Stream stream) {
            try {
                await Task.Yield();
                new System.Media.SoundPlayer(stream)?.PlaySync();
            }
            catch (Exception) {
            }
        }

        public async void PlayFile(string path) {
            try {
                if (File.Exists(path)) {
                    await Task.Yield();
                    using var player = new SoundPlayer(path);
                    player.PlaySync(); // 同步播放
                }
            }
            catch (Exception) {
            }
        }

        public async void PlaySuccess() {
            try {
                await Task.Yield();
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("ShengJiu.Weigh.Plugin.Sound.success.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.PlaySync(); // 同步播放
            }
            catch (Exception) {
            }
        }

        public async void PlayFail() {
            try {
                await Task.Yield();
                var assembly = Assembly.GetExecutingAssembly();

                // 获取嵌入资源的流
                await using var stream = assembly.GetManifestResourceStream("ShengJiu.Weigh.Plugin.Sound.fail.wav");
                // 如果流为空，则嵌入资源不存在或无法读取该资源
                if (stream == null) {
                    return;
                }

                // 使用SoundPlayer播放wav文件流
                using var player = new SoundPlayer(stream);
                player.PlaySync(); // 同步播放
            }
            catch (Exception) {
            }
        }
    }
}