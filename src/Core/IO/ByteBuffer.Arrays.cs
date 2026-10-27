namespace Electron2D;

public static partial class ByteBuffer
{
    /// <summary>Decodes complete little-endian int values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 4 bytes.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 4.</exception>
    public static int[] ToInt32Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 4);
        var result = new int[data.Length / 4];
        for (var i = 0; i < result.Length; i++) result[i] = DecodeS32(data, i * 4);
        return result;
    }

    /// <summary>Decodes complete little-endian long values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 8 bytes.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 8.</exception>
    public static long[] ToInt64Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 8);
        var result = new long[data.Length / 8];
        for (var i = 0; i < result.Length; i++) result[i] = DecodeS64(data, i * 8);
        return result;
    }

    /// <summary>Decodes complete little-endian float values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 4 bytes.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 4.</exception>
    public static float[] ToFloat32Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 4);
        var result = new float[data.Length / 4];
        for (var i = 0; i < result.Length; i++) result[i] = DecodeFloat(data, i * 4);
        return result;
    }

    /// <summary>Decodes complete little-endian double values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 8 bytes.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 8.</exception>
    public static double[] ToFloat64Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 8);
        var result = new double[data.Length / 8];
        for (var i = 0; i < result.Length; i++) result[i] = DecodeDouble(data, i * 8);
        return result;
    }

    /// <summary>Decodes complete little-endian Vector2 values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 8 bytes in component order.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 8.</exception>
    public static Vector2[] ToVector2Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 8);
        var result = new Vector2[data.Length / 8];
        for (var i = 0; i < result.Length; i++) result[i] = new(DecodeFloat(data, i * 8), DecodeFloat(data, i * 8 + 4));
        return result;
    }

    /// <summary>Decodes complete little-endian Vector3 values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 12 bytes in component order.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 12.</exception>
    public static Vector3[] ToVector3Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 12);
        var result = new Vector3[data.Length / 12];
        for (var i = 0; i < result.Length; i++) result[i] = new(DecodeFloat(data, i * 12), DecodeFloat(data, i * 12 + 4), DecodeFloat(data, i * 12 + 8));
        return result;
    }

    /// <summary>Decodes complete little-endian Vector4 values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 16 bytes in component order.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 16.</exception>
    public static Vector4[] ToVector4Array(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 16);
        var result = new Vector4[data.Length / 16];
        for (var i = 0; i < result.Length; i++) result[i] = new(DecodeFloat(data, i * 16), DecodeFloat(data, i * 16 + 4), DecodeFloat(data, i * 16 + 8), DecodeFloat(data, i * 16 + 12));
        return result;
    }

    /// <summary>Decodes complete little-endian Color values into independent storage.</summary>
    /// <param name="data">Borrowed source; each element occupies 16 bytes in component order.</param>
    /// <returns>A caller-owned array, with no live alias to the source.</returns>
    /// <exception cref="ArgumentException">The byte length is not a multiple of 16.</exception>
    public static Color[] ToColorArray(ReadOnlySpan<byte> data)
    {
        Elements(data.Length, 16);
        var result = new Color[data.Length / 16];
        for (var i = 0; i < result.Length; i++) result[i] = new(DecodeFloat(data, i * 16), DecodeFloat(data, i * 16 + 4), DecodeFloat(data, i * 16 + 8), DecodeFloat(data, i * 16 + 12));
        return result;
    }

    /// <summary>Encodes int values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 4 bytes.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<int> data)
    {
        var result = new byte[checked(data.Length * 4)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeS32(result, i * 4, data[i]);
        }
        return result;
    }

    /// <summary>Encodes long values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 8 bytes.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<long> data)
    {
        var result = new byte[checked(data.Length * 8)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeS64(result, i * 8, data[i]);
        }
        return result;
    }

    /// <summary>Encodes float values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 4 bytes.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<float> data)
    {
        var result = new byte[checked(data.Length * 4)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeFloat(result, i * 4, data[i]);
        }
        return result;
    }

    /// <summary>Encodes double values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 8 bytes.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<double> data)
    {
        var result = new byte[checked(data.Length * 8)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeDouble(result, i * 8, data[i]);
        }
        return result;
    }

    /// <summary>Encodes Vector2 values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 8 bytes in component order.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<Vector2> data)
    {
        var result = new byte[checked(data.Length * 8)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeFloat(result, i * 8, data[i].X);
            EncodeFloat(result, i * 8 + 4, data[i].Y);
        }
        return result;
    }

    /// <summary>Encodes Vector3 values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 12 bytes in component order.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<Vector3> data)
    {
        var result = new byte[checked(data.Length * 12)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeFloat(result, i * 12, data[i].X);
            EncodeFloat(result, i * 12 + 4, data[i].Y);
            EncodeFloat(result, i * 12 + 8, data[i].Z);
        }
        return result;
    }

    /// <summary>Encodes Vector4 values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 16 bytes in component order.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<Vector4> data)
    {
        var result = new byte[checked(data.Length * 16)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeFloat(result, i * 16, data[i].X);
            EncodeFloat(result, i * 16 + 4, data[i].Y);
            EncodeFloat(result, i * 16 + 8, data[i].Z);
            EncodeFloat(result, i * 16 + 12, data[i].W);
        }
        return result;
    }

    /// <summary>Encodes Color values into independent little-endian bytes.</summary>
    /// <param name="data">Borrowed values; each element occupies 16 bytes in component order.</param>
    /// <returns>A caller-owned byte array, with no live alias to the source.</returns>
    /// <exception cref="OverflowException">The byte count exceeds the signed managed length range.</exception>
    public static byte[] ToByteArray(ReadOnlySpan<Color> data)
    {
        var result = new byte[checked(data.Length * 16)];
        for (var i = 0; i < data.Length; i++)
        {
            EncodeFloat(result, i * 16, data[i].R);
            EncodeFloat(result, i * 16 + 4, data[i].G);
            EncodeFloat(result, i * 16 + 8, data[i].B);
            EncodeFloat(result, i * 16 + 12, data[i].A);
        }
        return result;
    }

    private static void Elements(int length, int size)
    {
        if (length % size != 0) throw new ArgumentException("The buffer contains an incomplete element.", "data");
    }
}
