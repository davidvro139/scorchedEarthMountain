using System.IO;

namespace ScorchedEarthMountain.App;

internal sealed class MountainDocument
{
    public required string FileName { get; init; }
    public required ushort Width { get; init; }
    public required ushort MinimumBytesPerRow { get; init; }
    public required ushort Height { get; init; }
    public required ushort PaletteAndImageSize { get; init; }
    public required ushort SkyPaletteIndex { get; init; }
    public required RgbColor[] Palette { get; init; }
    public required List<byte[]> Pixels { get; init; }

    public static MountainDocument FromBitmap(BitmapDocument bitmapDocument, string fileName)
    {
        byte skyPaletteValue = bitmapDocument.PixelsBottomUp[^1][0];
        byte[][] rotatedLeft = MatrixHelpers.RotateLeft(bitmapDocument.PixelsBottomUp);
        byte[][] mirrored = MatrixHelpers.Mirror(rotatedLeft);
        List<byte[]> unpadded = MatrixHelpers.Unpad(mirrored, skyPaletteValue);

        return new MountainDocument
        {
            FileName = fileName,
            Width = checked((ushort)bitmapDocument.Width),
            Height = checked((ushort)bitmapDocument.Height),
            MinimumBytesPerRow = (ushort)unpadded.Min(column => column.Length),
            PaletteAndImageSize = 0,
            SkyPaletteIndex = skyPaletteValue,
            Palette = bitmapDocument.Palette,
            Pixels = unpadded
        };
    }

    public static MountainDocument FromBytes(byte[] bytes, string fileName)
    {
        using MemoryStream stream = new(bytes, writable: false);
        using BinaryReader reader = new(stream);

        byte[] signature = reader.ReadBytes(4);
        if (!signature.SequenceEqual(new byte[] { (byte)'M', (byte)'T', 0xBE, 0xEF }))
        {
            throw new InvalidOperationException("Input file is not a valid MTN file.");
        }

        ushort version = ReadUInt16BigEndian(reader);
        if (version != 1)
        {
            throw new InvalidOperationException($"Unsupported MTN version: {version}");
        }

        ushort width = reader.ReadUInt16();
        ushort minimumBytesPerRow = reader.ReadUInt16();
        ushort height = reader.ReadUInt16();
        ushort colorCount = reader.ReadUInt16();
        ushort paletteAndImageSize = reader.ReadUInt16();
        ushort skyPaletteIndex = reader.ReadUInt16();

        reader.ReadBytes(6);

        if (colorCount != 16)
        {
            throw new InvalidOperationException($"Unsupported palette size: {colorCount}");
        }

        RgbColor[] palette = new RgbColor[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            palette[i] = new RgbColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        List<byte[]> pixels = new(width);
        for (int column = 0; column < width; column++)
        {
            ushort count = reader.ReadUInt16();
            int packedLength = (count + 1) / 2;
            byte[] packed = reader.ReadBytes(packedLength);
            if (packed.Length != packedLength)
            {
                throw new InvalidOperationException("Unexpected end of MTN pixel data.");
            }

            byte[] items = new byte[count];
            for (int i = 0; i < count; i++)
            {
                byte pair = packed[i / 2];
                items[i] = (byte)((i % 2 == 0) ? (pair >> 4) & 0x0F : pair & 0x0F);
            }

            pixels.Add(items);
        }

        return new MountainDocument
        {
            FileName = fileName,
            Width = width,
            MinimumBytesPerRow = minimumBytesPerRow,
            Height = height,
            PaletteAndImageSize = paletteAndImageSize,
            SkyPaletteIndex = skyPaletteIndex,
            Palette = palette,
            Pixels = pixels
        };
    }

    public BitmapDocument ToBitmapDocument()
    {
        byte[][] padded = MatrixHelpers.PadColumns(Pixels, Height, (byte)SkyPaletteIndex);
        byte[][] mirrored = MatrixHelpers.Mirror(padded);
        byte[][] rotated = MatrixHelpers.RotateRight(mirrored);

        return new BitmapDocument
        {
            Width = Width,
            Height = Height,
            Palette = Palette,
            PixelsBottomUp = rotated
        };
    }

    public byte[] ToBytes()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)0xBE);
        writer.Write((byte)0xEF);
        WriteUInt16BigEndian(writer, 1);
        writer.Write(Width);
        writer.Write(MinimumBytesPerRow);
        writer.Write(Height);
        writer.Write((ushort)Palette.Length);
        writer.Write(PaletteAndImageSize);
        writer.Write(SkyPaletteIndex);
        writer.Write(new byte[6]);

        foreach (RgbColor color in Palette)
        {
            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);
        }

        foreach (byte[] column in Pixels)
        {
            writer.Write(checked((ushort)column.Length));
            for (int i = 0; i < column.Length; i += 2)
            {
                byte first = column[i];
                byte second = i + 1 < column.Length ? column[i + 1] : (byte)0;
                writer.Write((byte)((first << 4) | second));
            }
        }

        return stream.ToArray();
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[2];
        int read = reader.Read(buffer);
        if (read != 2)
        {
            throw new InvalidOperationException("Unexpected end of file.");
        }

        return (ushort)((buffer[0] << 8) | buffer[1]);
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }
}
