using System.Drawing.Imaging;

namespace GazeTracker.Tool
{
    public static class BitmapUtil
    {
        private static int _stride;
        private static byte[] _data;
        private static PixelFormat _format;
        private static int _width;
        private static int _height;
        private static double _dpiX;
        private static double _dpiY;

        public static void BackupBitmap(BitmapSource bitmapSource)
        {
            _width = bitmapSource.PixelWidth;
            _height = bitmapSource.PixelHeight;
            _dpiX = bitmapSource.DpiX;
            _dpiY = bitmapSource.DpiY;
            _stride = ((bitmapSource.PixelWidth * bitmapSource.Format.BitsPerPixel + 31) >> 5) << 2;

            var requiredSize = bitmapSource.PixelHeight * _stride;

            if (_data == null || _data.Length < requiredSize)
                _data = new byte[requiredSize];

            bitmapSource.CopyPixels(_data, _stride, 0);

            _format = bitmapSource.Format;
        }

        public static BitmapSource RecoverBitmap()
        {
            var bitmap = new WriteableBitmap(_width, _height, _dpiX, _dpiY, _format, null);
            bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _data, _stride, 0);

            return bitmap;
        }
    }
}
