namespace ScorchedEarthMountain.App;

internal static class MatrixHelpers
{
    public static byte[][] RotateRight(IReadOnlyList<byte[]> matrix)
    {
        int sourceRows = matrix.Count;
        int sourceColumns = matrix[0].Length;
        byte[][] result = new byte[sourceColumns][];
        for (int column = 0; column < sourceColumns; column++)
        {
            result[column] = new byte[sourceRows];
            for (int row = 0; row < sourceRows; row++)
            {
                result[column][row] = matrix[sourceRows - 1 - row][column];
            }
        }

        return result;
    }

    public static byte[][] RotateLeft(IReadOnlyList<byte[]> matrix)
    {
        return RotateRight(RotateRight(RotateRight(matrix)));
    }

    public static byte[][] Mirror(IReadOnlyList<byte[]> matrix)
    {
        byte[][] result = new byte[matrix.Count][];
        for (int i = 0; i < matrix.Count; i++)
        {
            result[i] = matrix[matrix.Count - 1 - i].ToArray();
        }

        return result;
    }

    public static List<byte[]> Unpad(IReadOnlyList<byte[]> matrix, byte padValue)
    {
        List<byte[]> result = new(matrix.Count);
        foreach (byte[] row in matrix)
        {
            int lastIndex = row.Length - 1;
            while (lastIndex >= 0 && row[lastIndex] == padValue)
            {
                lastIndex--;
            }

            byte[] trimmed = new byte[lastIndex + 1];
            if (trimmed.Length > 0)
            {
                Array.Copy(row, trimmed, trimmed.Length);
            }

            result.Add(trimmed);
        }

        return result;
    }

    public static byte[][] PadColumns(IReadOnlyList<byte[]> columns, int height, byte padValue)
    {
        byte[][] result = new byte[columns.Count][];
        for (int i = 0; i < columns.Count; i++)
        {
            byte[] source = columns[i];
            byte[] padded = new byte[height];
            Array.Copy(source, padded, source.Length);
            for (int j = source.Length; j < height; j++)
            {
                padded[j] = padValue;
            }

            result[i] = padded;
        }

        return result;
    }
}
