using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace RPA._90_Common
{
    static class Common
    {
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                memory.Flush();

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public async static Task<SoftwareBitmap> ToSoftwareBitmap(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                memory.Flush();

                var a = await ConvertToRandomAccessStream(memory);
                Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(a);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                return softwareBitmap;
            }
        }

        private async static Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            dw.WriteBytes(memoryStream.ToArray());
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }
    }
}
