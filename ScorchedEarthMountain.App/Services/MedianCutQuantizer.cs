namespace ScorchedEarthMountain.App;

internal sealed class MedianCutQuantizer
{
    private readonly IReadOnlyList<PixelData> _pixels;
    private readonly int _targetColorCount;

    public MedianCutQuantizer(IReadOnlyList<PixelData> pixels, int targetColorCount)
    {
        _pixels = pixels;
        _targetColorCount = targetColorCount;
    }

    public QuantizedImage Quantize(int width, int height)
    {
        List<ColorBucket> buckets = new() { new ColorBucket(_pixels.ToList()) };

        while (buckets.Count < _targetColorCount)
        {
            ColorBucket? splittable = buckets
                .Where(bucket => bucket.Pixels.Count > 1)
                .OrderByDescending(bucket => bucket.Range)
                .ThenByDescending(bucket => bucket.Pixels.Count)
                .FirstOrDefault();

            if (splittable is null)
            {
                break;
            }

            buckets.Remove(splittable);
            (ColorBucket left, ColorBucket right) = splittable.Split();
            buckets.Add(left);
            buckets.Add(right);
        }

        RgbColor[] palette = buckets
            .Select(bucket => bucket.AverageColor)
            .Take(_targetColorCount)
            .ToArray();

        if (palette.Length < _targetColorCount)
        {
            Array.Resize(ref palette, _targetColorCount);
            for (int i = buckets.Count; i < palette.Length; i++)
            {
                palette[i] = palette[0];
            }
        }

        byte[] indices = new byte[width * height];
        for (int i = 0; i < _pixels.Count; i++)
        {
            PixelData pixel = _pixels[i];
            indices[pixel.Y * width + pixel.X] = FindNearestPaletteIndex(pixel, palette);
        }

        return new QuantizedImage(palette, indices);
    }

    private static byte FindNearestPaletteIndex(PixelData pixel, RgbColor[] palette)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < palette.Length; i++)
        {
            RgbColor color = palette[i];
            int dr = pixel.R - color.R;
            int dg = pixel.G - color.G;
            int db = pixel.B - color.B;
            int distance = dr * dr + dg * dg + db * db;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return (byte)bestIndex;
    }

    private sealed class ColorBucket
    {
        public ColorBucket(List<PixelData> pixels)
        {
            Pixels = pixels;
        }

        public List<PixelData> Pixels { get; }

        public int Range
        {
            get
            {
                GetChannelRanges(out int rRange, out int gRange, out int bRange);
                return Math.Max(rRange, Math.Max(gRange, bRange));
            }
        }

        public RgbColor AverageColor
        {
            get
            {
                int r = 0;
                int g = 0;
                int b = 0;
                foreach (PixelData pixel in Pixels)
                {
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                }

                int count = Math.Max(1, Pixels.Count);
                return new RgbColor((byte)(r / count), (byte)(g / count), (byte)(b / count));
            }
        }

        public (ColorBucket Left, ColorBucket Right) Split()
        {
            GetChannelRanges(out int rRange, out int gRange, out int bRange);
            Func<PixelData, byte> selector = rRange >= gRange && rRange >= bRange
                ? pixel => pixel.R
                : gRange >= bRange
                    ? pixel => pixel.G
                    : pixel => pixel.B;

            List<PixelData> ordered = Pixels.OrderBy(selector).ToList();
            int midpoint = ordered.Count / 2;
            if (midpoint == 0)
            {
                midpoint = 1;
            }

            return (
                new ColorBucket(ordered.Take(midpoint).ToList()),
                new ColorBucket(ordered.Skip(midpoint).ToList())
            );
        }

        private void GetChannelRanges(out int rRange, out int gRange, out int bRange)
        {
            byte minR = byte.MaxValue;
            byte minG = byte.MaxValue;
            byte minB = byte.MaxValue;
            byte maxR = byte.MinValue;
            byte maxG = byte.MinValue;
            byte maxB = byte.MinValue;

            foreach (PixelData pixel in Pixels)
            {
                minR = Math.Min(minR, pixel.R);
                minG = Math.Min(minG, pixel.G);
                minB = Math.Min(minB, pixel.B);
                maxR = Math.Max(maxR, pixel.R);
                maxG = Math.Max(maxG, pixel.G);
                maxB = Math.Max(maxB, pixel.B);
            }

            rRange = maxR - minR;
            gRange = maxG - minG;
            bRange = maxB - minB;
        }
    }
}
