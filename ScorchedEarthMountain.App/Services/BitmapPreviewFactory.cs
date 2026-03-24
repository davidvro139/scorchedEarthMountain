using System.IO;
using System.Windows.Media.Imaging;

namespace ScorchedEarthMountain.App;

internal static class BitmapPreviewFactory
{
    public static BitmapSource FromFile(string path)
    {
        BitmapImage image = new();
        using FileStream stream = File.OpenRead(path);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static BitmapSource FromBytes(byte[] bytes)
    {
        BitmapImage image = new();
        using MemoryStream stream = new(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
