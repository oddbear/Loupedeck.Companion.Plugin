using SkiaSharp;

namespace Loupedeck.CompanionPlugin.Extensions
{
    public static class BitmapExtensions
    {
        public static BitmapImage DrawDisconnected()
        {
            using (var bitmapBuilder = new BitmapBuilder(80, 80))
            {
                var path = "Loupedeck.CompanionPlugin.Resources.Companion.disconnected-80.png";
                var background = EmbeddedResources.ReadImage(path);
                bitmapBuilder.Clear(BitmapColor.Black);
                bitmapBuilder.SetBackgroundImage(background);
                return bitmapBuilder.ToImage();
            }
        }

        public static BitmapImage BitmapToBitmapImage(this SKBitmap bitmap)
        {
            var data = bitmap.Encode(SKEncodedImageFormat.Png, 80);

            return BitmapImage.FromArray(data.ToArray());
        }

        public static void DrawBuffer(this SKBitmap bitmap, byte[] buffer)
        {
            var height = 72;
            var width = 72;
            var bytes = 3; //r, g, b

            if (buffer.Length != height * width * bytes)
                throw new ArgumentException($"Buffer is wrong size, should be {bytes} but  was {buffer.Length}");
            

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pos = y * height * 3 + x * 3;

                    var r = buffer[pos];
                    var g = buffer[pos + 1];
                    var b = buffer[pos + 2];

                    bitmap.SetPixel(x, y, new SKColor(r, g, b));
                }
            }
        }
    }
}
