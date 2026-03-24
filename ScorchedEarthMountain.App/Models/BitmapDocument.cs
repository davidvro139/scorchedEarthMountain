using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScorchedEarthMountain.App;

internal sealed class BitmapDocument
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required RgbColor[] Palette { get; init; }
    public required byte[][] PixelsBottomUp { get; init; }

    public static BitmapDocument FromBitmapSource(BitmapSource source)
    {
        if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
        {
            throw new InvalidOperationException("Input image must have a non-zero size.");
        }

        BitmapSource converted = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = converted.PixelWidth;
        int height = converted.PixelHeight;
        int stride = width * 4;
        byte[] pixelBuffer = new byte[stride * height];
        converted.CopyPixels(pixelBuffer, stride, 0);

        List<PixelData> sourcePixels = new(width * height);
        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                int offset = rowOffset + x * 4;
                sourcePixels.Add(new PixelData(x, y, pixelBuffer[offset + 2], pixelBuffer[offset + 1], pixelBuffer[offset]));
            }
        }

        MedianCutQuantizer quantizer = new(sourcePixels, 16);
        QuantizedImage quantized = quantizer.Quantize(width, height);

        byte[][] pixelsTopDown = new byte[height][];
        for (int y = 0; y < height; y++)
        {
            pixelsTopDown[y] = new byte[width];
            Array.Copy(quantized.Indices, y * width, pixelsTopDown[y], 0, width);
        }

        Array.Reverse(pixelsTopDown);

        return new BitmapDocument
        {
            Width = width,
            Height = height,
            Palette = quantized.Palette,
            PixelsBottomUp = pixelsTopDown
        };
    }

    public static RgbColor SampleTopLeftColor(BitmapSource source)
    {
        BitmapSource converted = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        byte[] pixel = new byte[4];
        converted.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, 4, 0);
        return new RgbColor(pixel[2], pixel[1], pixel[0]);
    }

    public byte[] ToBytes()
    {
        int rowStride = ((Width + 1) / 2 + 3) & ~3;
        int imageDataSize = rowStride * Height;
        int paletteSize = Palette.Length * 4;
        int dataOffset = 14 + 40 + paletteSize;
        int fileSize = dataOffset + imageDataSize;

        using MemoryStream stream = new(fileSize);
        using BinaryWriter writer = new(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(dataOffset);
        writer.Write(40);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write((ushort)1);
        writer.Write((ushort)4);
        writer.Write(0);
        writer.Write(imageDataSize);
        writer.Write((int)Math.Ceiling(39.3701 * 72 * Width));
        writer.Write((int)Math.Ceiling(39.3701 * 72 * Height));
        writer.Write(Palette.Length);
        writer.Write(0);

        foreach (RgbColor color in Palette)
        {
            writer.Write(color.B);
            writer.Write(color.G);
            writer.Write(color.R);
            writer.Write((byte)0);
        }

        for (int y = 0; y < Height; y++)
        {
            byte[] row = PixelsBottomUp[y];
            for (int x = 0; x < Width; x += 2)
            {
                byte left = row[x];
                byte right = x + 1 < Width ? row[x + 1] : (byte)0;
                writer.Write((byte)((left << 4) | right));
            }

            int bytesWritten = (Width + 1) / 2;
            for (int i = bytesWritten; i < rowStride; i++)
            {
                writer.Write((byte)0);
            }
        }

        return stream.ToArray();
    }

}
