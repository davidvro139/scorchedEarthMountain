namespace ScorchedEarthMountain.App;

internal readonly record struct RgbColor(byte R, byte G, byte B);

internal readonly record struct PixelData(int X, int Y, byte R, byte G, byte B);

internal sealed record QuantizedImage(RgbColor[] Palette, byte[] Indices);
