using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScorchedEarthMountain.App;

internal static class BackgroundRemovalProcessor
{
    private static readonly RgbColor ExportSkyColor = new(255, 255, 255);

    public static BackgroundRemovalResult RemoveBackground(BitmapSource source, byte tolerance, RgbColor? backgroundColor = null)
    {
        BitmapSource converted = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = converted.PixelWidth;
        int height = converted.PixelHeight;
        int stride = width * 4;
        byte[] exportPixels = new byte[stride * height];
        converted.CopyPixels(exportPixels, stride, 0);
        byte[] previewPixels = new byte[exportPixels.Length];
        Array.Copy(exportPixels, previewPixels, exportPixels.Length);

        RgbColor selectedColor = backgroundColor ?? new RgbColor(exportPixels[2], exportPixels[1], exportPixels[0]);
        byte skyR = selectedColor.R;
        byte skyG = selectedColor.G;
        byte skyB = selectedColor.B;

        bool[] visited = new bool[width * height];
        Queue<(int X, int Y)> queue = new();
        queue.Enqueue((0, 0));
        int removedPixelCount = 0;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            if ((uint)x >= width || (uint)y >= height)
            {
                continue;
            }

            int index = y * width + x;
            if (visited[index])
            {
                continue;
            }

            visited[index] = true;
            int offset = y * stride + x * 4;

            if (!MatchesBackground(exportPixels, offset, skyR, skyG, skyB, tolerance))
            {
                continue;
            }

            exportPixels[offset] = ExportSkyColor.B;
            exportPixels[offset + 1] = ExportSkyColor.G;
            exportPixels[offset + 2] = ExportSkyColor.R;
            exportPixels[offset + 3] = 255;

            previewPixels[offset] = ExportSkyColor.B;
            previewPixels[offset + 1] = ExportSkyColor.G;
            previewPixels[offset + 2] = ExportSkyColor.R;
            previewPixels[offset + 3] = 0;
            removedPixelCount++;

            queue.Enqueue((x - 1, y));
            queue.Enqueue((x + 1, y));
            queue.Enqueue((x, y - 1));
            queue.Enqueue((x, y + 1));
        }

        WriteableBitmap exportImage = new(width, height, converted.DpiX, converted.DpiY, PixelFormats.Bgra32, null);
        exportImage.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), exportPixels, stride, 0);
        exportImage.Freeze();

        WriteableBitmap previewImage = new(width, height, converted.DpiX, converted.DpiY, PixelFormats.Bgra32, null);
        previewImage.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), previewPixels, stride, 0);
        previewImage.Freeze();

        return new BackgroundRemovalResult(exportImage, previewImage, removedPixelCount, ExportSkyColor);
    }

    private static bool MatchesBackground(byte[] pixels, int offset, byte skyR, byte skyG, byte skyB, byte tolerance)
    {
        int dr = Math.Abs(pixels[offset + 2] - skyR);
        int dg = Math.Abs(pixels[offset + 1] - skyG);
        int db = Math.Abs(pixels[offset] - skyB);
        return Math.Max(dr, Math.Max(dg, db)) <= tolerance;
    }
}

internal sealed record BackgroundRemovalResult(BitmapSource ExportImage, BitmapSource PreviewImage, int RemovedPixelCount, RgbColor ExportSkyColor);
