using Electron2D;
using System.Buffers.Binary;
using System.Numerics;

internal static class MatrixValueCodecTests
{
    internal static void Run()
    {
        var value = new Matrix4x4();
        for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) value[row, column] = row * 4 + column + .25f;
        foreach (var big in new[] { false, true })
        {
            using var stream = new StreamPeerBuffer { BigEndian = big };
            var codec = ResourceFileTypes.Codec<Matrix4x4>(); codec.Write(stream, value);
            var expected = new byte[64];
            for (var index = 0; index < 16; index++)
                if (big) BinaryPrimitives.WriteSingleBigEndian(expected.AsSpan(index * 4), index + .25f);
                else BinaryPrimitives.WriteSingleLittleEndian(expected.AsSpan(index * 4), index + .25f);
            Check(stream.DataArray.SequenceEqual(expected), "Matrix wire words use explicit row order and selected byte order");
            stream.Seek(0); Check(codec.Read(stream) == value, "All sixteen matrix words roundtrip");
            using var optional = new StreamPeerBuffer { BigEndian = big };
            var nullable = ResourceFileTypes.Codec<Matrix4x4?>(); nullable.Write(optional, null); nullable.Write(optional, value); optional.Seek(0);
            Check(nullable.Read(optional) is null && nullable.Read(optional) == value, "Nullable matrices preserve absence and presence");
            using var arrays = new StreamPeerBuffer { BigEndian = big }; var array = ResourceFileTypes.Codec<Matrix4x4[]>();
            array.Write(arrays, null!); array.Write(arrays, []); array.Write(arrays, [value, Matrix4x4.Identity]); arrays.Seek(0);
            Check(array.Read(arrays) is null && array.Read(arrays).Length == 0 && array.Read(arrays).SequenceEqual([value, Matrix4x4.Identity]), "Matrix arrays preserve null/empty/elements");
            using var truncated = new StreamPeerBuffer { BigEndian = big, DataArray = expected[..63] };
            try { codec.Read(truncated); throw new InvalidOperationException("Expected truncated matrix rejection"); } catch (EndOfStreamException) { }
        }
        Console.WriteLine("Matrix codecs: row words, both byte orders, nullable/array values and truncation passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
