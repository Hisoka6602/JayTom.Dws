using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Nvr.Nvr {

    public static class FFmpegBinariesHelper {
        private const string FFmpegPath = "C:\\Users\\77051\\Desktop\\ffmpeg-6.1-essentials_build\\ffmpeg-6.1-essentials_build\\bin\\ffmpeg.exe";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static void RegisterFFmpegBinaries() {
            //var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FFmpegPath);

            if (SetDllDirectory(FFmpegPath)) {
                return;
            }

            throw new System.ComponentModel.Win32Exception();
        }
    }
}