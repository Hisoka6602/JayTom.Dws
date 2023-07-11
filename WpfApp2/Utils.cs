using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WpfApp2 {

    public static class Utils {

        public static IntPtr StringToByteArray(string s) {
            var p = new IntPtr();
            var b = new byte[s.Length + 1];
            int i;
            for (i = 0; i < s.Length; i++)
                b[i] = (byte)s.ToCharArray()[i];
            b[s.Length] = 0;
            p = Marshal.AllocCoTaskMem(s.Length + 1);
            Marshal.Copy(b, 0, p, s.Length + 1);
            return p;
        }

        public static Image? ByteToImage(byte[] myByte) {
            try {
                var ms = new MemoryStream(myByte);
                var image = Image.FromStream(ms);
                return image;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            return null;
        }

        public static GraphicsPath GetRoundedRectangle(RectangleF rect, int cornerRadius) {
            var roundedRect = new GraphicsPath();
            roundedRect.AddArc(rect.X, rect.Y, cornerRadius, cornerRadius, 180, 90);
            roundedRect.AddArc(rect.Right - cornerRadius, rect.Y, cornerRadius, cornerRadius, 270, 90);
            roundedRect.AddArc(rect.Right - cornerRadius, rect.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            roundedRect.CloseFigure();

            return roundedRect;
        }
    }
}