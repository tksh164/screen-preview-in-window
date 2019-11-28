using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace spiw
{
    internal static class ScreenCapture
    {
        private static class NativeMethods
        {
            [DllImport("gdi32.dll", SetLastError = false)]
            public static extern bool DeleteObject(IntPtr hObject);
        }

        public static BitmapSource GetScreenImage(int posLeft, int posTop, int width, int height)
        {
            using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format16bppRgb565))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(posLeft, posTop, 0, 0, new System.Drawing.Size { Width = width, Height = height }, CopyPixelOperation.SourceCopy);

                var bitmapHandle = bitmap.GetHbitmap();
                try
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                finally
                {
                    NativeMethods.DeleteObject(bitmapHandle);
                }
            }
        }
    }
}
