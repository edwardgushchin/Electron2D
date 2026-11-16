using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using Electron2D;
using FileAccess = Electron2D.FileAccess;

internal static partial class FileCompressionTests
{
    private static void IndependentVectors(string directory)
    {
        foreach (var mode in Enum.GetValues<FileCompressionMode>())
        foreach (var size in new[] { 0, 1, 15, 16, 65535, 65536, 65537, 131089 })
        {
            var data = Enumerable.Range(0, size).Select(index => (byte)(index * 73 + index / 251)).ToArray();
            var envelope = Vector(data, mode);
            var path = System.IO.Path.Combine(directory, "independent-vector");
            File.WriteAllBytes(path, envelope);
            Check(ByteBuffer.Decompress(envelope, Math.Max(1, size), mode).SequenceEqual(data), "Independent buffer vector.");
            using (var read = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode))
            {
                Check(read.ReadBytes(size + 1).SequenceEqual(data), "Independent file vector.");
                read.Seek(0);
                Check(read.ReadBytes(size + 1).SequenceEqual(data), "Repeated file read.");
            }
            foreach (var other in Enum.GetValues<FileCompressionMode>().Where(other => other != mode))
            {
                Reject<InvalidDataException>(() => ByteBuffer.Decompress(envelope, Math.Max(1, size), other));
                Reject<InvalidDataException>(() => FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, other).Dispose());
            }
            if (size > 1) Reject<InvalidDataException>(() => ByteBuffer.Decompress(envelope, size - 1, mode));
            if (mode is FileCompressionMode.Deflate or FileCompressionMode.Gzip or FileCompressionMode.Brotli)
            {
                Check(ByteBuffer.DecompressDynamic(envelope, -1, mode).SequenceEqual(data), "Independent dynamic vector.");
                if (size > 1) Reject<InvalidDataException>(() => ByteBuffer.DecompressDynamic(envelope, size - 1, mode));
            }
            var bad = (byte[])envelope.Clone(); bad[6] = 255;
            File.WriteAllBytes(path, bad);
            var snapshot = File.ReadAllBytes(path);
            Reject<InvalidDataException>(() => FileAccess.OpenCompressed(path, FileAccessModeFlags.ReadWrite, mode).Dispose());
            Check(File.ReadAllBytes(path).SequenceEqual(snapshot), "Failed editing open preserves encoded bytes.");
            Reject<InvalidDataException>(() => ByteBuffer.Decompress(bad, Math.Max(1, size), mode));
            Reject<InvalidDataException>(() => ByteBuffer.Decompress(envelope.AsSpan(0, 15), Math.Max(1, size), mode));
            if (size > 0)
            {
                var truncated = envelope[..(16 + (envelope.Length - 16) / 2)];
                Reject<InvalidDataException>(() => ByteBuffer.Decompress(truncated, Math.Max(1, size), mode));
                File.WriteAllBytes(path, truncated);
                Reject<InvalidDataException>(() => FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode).Dispose());
            }
        }
        foreach (var raw in new[] { -1, 5, 256, int.MaxValue })
        {
            var mode = (FileCompressionMode)raw;
            var path = System.IO.Path.Combine(directory, "invalid-codec");
            File.WriteAllBytes(path, [17]);
            foreach (var access in Enum.GetValues<FileAccessModeFlags>())
                Reject<ArgumentOutOfRangeException>(() => FileAccess.OpenCompressed(path, access, mode).Dispose());
            Check(File.ReadAllBytes(path).SequenceEqual(new byte[] { 17 }), "Invalid codec preserves destination.");
            Reject<ArgumentOutOfRangeException>(() => ByteBuffer.Compress([], mode));
            Reject<ArgumentOutOfRangeException>(() => ByteBuffer.Decompress([], 1, mode));
            Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecompressDynamic([], -1, mode));
        }
        Console.WriteLine("Independent codec vectors: 40 empty/tiny/boundary/multiblock file/buffer cases, wrong codecs, budgets, repeated reads and failed-open atomicity passed.");
    }

    private static byte[] Vector(byte[] data, FileCompressionMode mode)
    {
        using var output = new MemoryStream();
        output.Write("E2DCMP"u8); output.WriteByte(1); output.WriteByte((byte)mode);
        var length = new byte[8]; BinaryPrimitives.WriteInt64LittleEndian(length, data.Length); output.Write(length);
        if (mode is FileCompressionMode.FastLz or FileCompressionMode.Zstandard)
        {
            // Literal-only FastLZ and raw-block Zstandard frames do not use the engine encoder.
            for (var offset = 0; offset < data.Length; offset += 65536)
            {
                var block = data.AsSpan(offset, Math.Min(65536, data.Length - offset)).ToArray();
                using var payload = new MemoryStream();
                if (mode == FileCompressionMode.FastLz)
                {
                    var padded = new byte[Math.Max(16, block.Length)]; block.CopyTo(padded, 0);
                    for (var start = 0; start < padded.Length; start += 32)
                    {
                        var count = Math.Min(32, padded.Length - start);
                        payload.WriteByte((byte)(count - 1)); payload.Write(padded, start, count);
                    }
                }
                else
                {
                    payload.Write(Convert.FromHexString("28B52FFDA0"));
                    var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, block.Length); payload.Write(header);
                    BinaryPrimitives.WriteInt32LittleEndian(header, (block.Length << 3) | 1); payload.Write(header, 0, 3);
                    payload.Write(block);
                }
                var countBytes = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(countBytes, (int)payload.Length);
                output.Write(countBytes); output.Write(SHA256.HashData(block)); payload.Position = 0; payload.CopyTo(output);
            }
        }
        else
        {
            using var encoder = mode switch
            {
                FileCompressionMode.Deflate => (Stream)new DeflateStream(output, CompressionLevel.NoCompression, true),
                FileCompressionMode.Gzip => new GZipStream(output, CompressionLevel.NoCompression, true),
                _ => new BrotliStream(output, CompressionLevel.Fastest, true)
            };
            encoder.Write(data);
        }
        return output.ToArray();
    }
}
