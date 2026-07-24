namespace JayTom.Dws.Plugin.Speech {

    public interface ISpeech {

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="speechText"></param>
        /// <param name="token"></param>
        Task Speak(string speechText, CancellationToken token = default);

        /// <summary>
        /// 播放流
        /// </summary>
        /// <param name="stream"></param>
        Task PlayStream(Stream stream);

        /// <summary>
        /// 播放字节文件
        /// </summary>
        /// <param name="file"></param>
        Task PlayByteFile(byte[] file);

        /// <summary>
        /// 播放缓存字节文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="file"></param>
        Task PlayCacheByteFile(string name, byte[] file);

        /// <summary>
        /// 播放文件
        /// </summary>
        /// <param name="path"></param>
        Task PlayFile(string path);

        /// <summary>
        /// 播放成功语音
        /// </summary>
        Task PlaySuccess();

        /// <summary>
        /// 播放失败语音
        /// </summary>
        Task PlayFail();
    }
}
