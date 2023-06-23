namespace JayTom.Dws.Plugin.Speech {

    public interface ISpeech {

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="speechText"></param>
        /// <param name="token"></param>
        void Speak(string speechText, CancellationToken token = default);

        /// <summary>
        /// 播放流
        /// </summary>
        /// <param name="stream"></param>
        void PlayStream(Stream stream);

        /// <summary>
        /// 播放文件
        /// </summary>
        /// <param name="path"></param>
        void PlayFile(string path);

        /// <summary>
        /// 播放成功语音
        /// </summary>
        void PlaySuccess();

        /// <summary>
        /// 播放失败语音
        /// </summary>
        void PlayFail();
    }
}