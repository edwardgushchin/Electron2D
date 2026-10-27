using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Electron2D;
using FileAccess = Electron2D.FileAccess;

internal static class ByteBufferTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    internal static void Run()
    {
        Numbers(); Text(); Arrays(); Compression(); Consumers();
        Console.WriteLine("ByteBuffer: 52 operations, numeric/Unicode boundaries, five codecs, file/UDP consumers and 4096 zero-byte prepared intervals passed.");
    }
    private static void Numbers()
    {
        var data = new byte[32];
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeS8(data, 1, sbyte.MinValue);
        Check(ByteBuffer.DecodeS8(data, 1) == sbyte.MinValue && ByteBuffer.HexEncode(data.AsSpan(1, 1)) == "80" && data[0] == 0xA5 && data[2] == 0xA5, "S8 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeS8(data, int.MaxValue, sbyte.MinValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS8(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS8(data, data.Length - 1 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeU8(data, 1, byte.MaxValue);
        Check(ByteBuffer.DecodeU8(data, 1) == byte.MaxValue && ByteBuffer.HexEncode(data.AsSpan(1, 1)) == "ff" && data[0] == 0xA5 && data[2] == 0xA5, "U8 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeU8(data, int.MaxValue, byte.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU8(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU8(data, data.Length - 1 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeS16(data, 1, short.MinValue);
        Check(ByteBuffer.DecodeS16(data, 1) == short.MinValue && ByteBuffer.HexEncode(data.AsSpan(1, 2)) == "0080" && data[0] == 0xA5 && data[3] == 0xA5, "S16 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeS16(data, int.MaxValue, short.MinValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS16(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS16(data, data.Length - 2 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeU16(data, 1, ushort.MaxValue);
        Check(ByteBuffer.DecodeU16(data, 1) == ushort.MaxValue && ByteBuffer.HexEncode(data.AsSpan(1, 2)) == "ffff" && data[0] == 0xA5 && data[3] == 0xA5, "U16 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeU16(data, int.MaxValue, ushort.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU16(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU16(data, data.Length - 2 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeS32(data, 1, int.MinValue);
        Check(ByteBuffer.DecodeS32(data, 1) == int.MinValue && ByteBuffer.HexEncode(data.AsSpan(1, 4)) == "00000080" && data[0] == 0xA5 && data[5] == 0xA5, "S32 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeS32(data, int.MaxValue, int.MinValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS32(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS32(data, data.Length - 4 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeU32(data, 1, uint.MaxValue);
        Check(ByteBuffer.DecodeU32(data, 1) == uint.MaxValue && ByteBuffer.HexEncode(data.AsSpan(1, 4)) == "ffffffff" && data[0] == 0xA5 && data[5] == 0xA5, "U32 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeU32(data, int.MaxValue, uint.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU32(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU32(data, data.Length - 4 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeS64(data, 1, long.MinValue);
        Check(ByteBuffer.DecodeS64(data, 1) == long.MinValue && ByteBuffer.HexEncode(data.AsSpan(1, 8)) == "0000000000000080" && data[0] == 0xA5 && data[9] == 0xA5, "S64 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeS64(data, int.MaxValue, long.MinValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS64(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeS64(data, data.Length - 8 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeU64(data, 1, ulong.MaxValue);
        Check(ByteBuffer.DecodeU64(data, 1) == ulong.MaxValue && ByteBuffer.HexEncode(data.AsSpan(1, 8)) == "ffffffffffffffff" && data[0] == 0xA5 && data[9] == 0xA5, "U64 golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeU64(data, int.MaxValue, ulong.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU64(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeU64(data, data.Length - 8 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeHalf(data, 1, 1.5f);
        Check(ByteBuffer.DecodeHalf(data, 1) == 1.5f && ByteBuffer.HexEncode(data.AsSpan(1, 2)) == "003e" && data[0] == 0xA5 && data[3] == 0xA5, "Half golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeHalf(data, int.MaxValue, 1.5f));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeHalf(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeHalf(data, data.Length - 2 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeFloat(data, 1, 1.5f);
        Check(ByteBuffer.DecodeFloat(data, 1) == 1.5f && ByteBuffer.HexEncode(data.AsSpan(1, 4)) == "0000c03f" && data[0] == 0xA5 && data[5] == 0xA5, "Float golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeFloat(data, int.MaxValue, 1.5f));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeFloat(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeFloat(data, data.Length - 4 + 1));
        data.AsSpan().Fill(0xA5); ByteBuffer.EncodeDouble(data, 1, 1.5);
        Check(ByteBuffer.DecodeDouble(data, 1) == 1.5 && ByteBuffer.HexEncode(data.AsSpan(1, 8)) == "000000000000f83f" && data[0] == 0xA5 && data[9] == 0xA5, "Double golden unaligned storage.");
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.EncodeDouble(data, int.MaxValue, 1.5));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeDouble(data, -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecodeDouble(data, data.Length - 8 + 1));
        ByteBuffer.EncodeS32(data, 0, -1); Check(ByteBuffer.DecodeS32(data, 0) == -1, "Signed -1.");
        ByteBuffer.EncodeFloat(data, 0, float.NegativeInfinity); Check(float.IsNegativeInfinity(ByteBuffer.DecodeFloat(data, 0)), "Infinity.");
        ByteBuffer.EncodeDouble(data, 0, double.NaN); Check(double.IsNaN(ByteBuffer.DecodeDouble(data, 0)), "NaN.");
        ByteBuffer.EncodeHalf(data, 0, 65520); Check(float.IsPositiveInfinity(ByteBuffer.DecodeHalf(data, 0)), "Half overflow follows IEEE.");
        ByteBuffer.EncodeFloat(data, 0, -0f); Check(BitConverter.SingleToInt32Bits(ByteBuffer.DecodeFloat(data, 0)) == int.MinValue, "Negative zero bit pattern.");
        var swapped = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        ByteBuffer.BSwap16(swapped, 1, -8); Check(ByteBuffer.HexEncode(swapped) == "00020104030605080709", "16-bit swaps retain incomplete tail.");
        ByteBuffer.BSwap32(swapped, 1, 1); Check(ByteBuffer.HexEncode(swapped) == "00030401020605080709", "32-bit bounded swap.");
        ByteBuffer.BSwap64(swapped, 1, 1); Check(ByteBuffer.HexEncode(swapped) == "00070805060201040309", "64-bit bounded swap.");
        var before = (byte[])swapped.Clone(); Reject<ArgumentOutOfRangeException>(() => ByteBuffer.BSwap64(swapped, 1, int.MaxValue));
        Check(swapped.SequenceEqual(before), "Overflow validation is atomic.");
        ByteBuffer.BSwap16([], 0); ByteBuffer.BSwap32(swapped, swapped.Length, 0);
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.BSwap16(swapped, -1, 0));
        for (var i = 0; i < 256; i++) Prepared(data);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 4096; i++) Prepared(data);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared scalar and swap work must allocate zero owner bytes.");
    }
    private static void Prepared(byte[] data)
    {
        ByteBuffer.EncodeU64(data, 1, 0x12345678ABCDEF01); ByteBuffer.BSwap64(data, 1, 1); ByteBuffer.BSwap64(data, 1, 1);
        Check(ByteBuffer.DecodeU64(data, 1) == 0x12345678ABCDEF01, "Prepared round trip.");
    }
    private static void Text()
    {
        const string text = "Привет 😀";
        Check(ByteBuffer.GetStringFromASCII([65, 233, 0, 66]) == "Aé", "Latin-1 and zero terminator.");
        Check(ByteBuffer.GetStringFromUTF8(Encoding.UTF8.GetBytes(text)) == text, "UTF-8.");
        Check(ByteBuffer.GetStringFromUTF8([0xEF, 0xBB, 0xBF, 65, 0, 66]) == "A\0B", "UTF-8 BOM and embedded zero.");
        Check(ByteBuffer.GetStringFromUTF16(Encoding.Unicode.GetBytes(text)) == text, "UTF-16 LE.");
        Check(ByteBuffer.GetStringFromUTF16(new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes(text)).ToArray()) == text, "UTF-16 BE BOM.");
        Check(ByteBuffer.GetStringFromUTF32(Encoding.UTF32.GetBytes(text)) == text, "UTF-32 LE.");
        Check(ByteBuffer.GetStringFromUTF32(new byte[] { 0, 0, 0xFE, 0xFF }.Concat(new UTF32Encoding(true, false, true).GetBytes(text)).ToArray()) == text, "UTF-32 BE BOM.");
        Check(ByteBuffer.GetStringFromWChar((OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF32).GetBytes(text)) == text, "Native wide characters.");
        Check(ByteBuffer.GetStringFromMultibyteChar([0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2], "windows-1251") == "Привет", "Named legacy code page.");
        Check(ByteBuffer.GetStringFromMultibyteChar([65]) == "A" && ByteBuffer.GetStringFromMultibyteChar([], "utf-8") == "", "Default and empty text.");
        Reject<ArgumentException>(() => ByteBuffer.GetStringFromMultibyteChar([65], "unavailable-e2d-encoding"));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromMultibyteChar([0xC0], "utf-8"));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromUTF8([0xC0, 0x80]));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromUTF16([0]));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromUTF16([0, 0xD8]));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromUTF32([0, 0, 0x11, 0]));
        Reject<DecoderFallbackException>(() => ByteBuffer.GetStringFromUTF32([0, 0, 0]));
        Check(ByteBuffer.GetStringFromUTF8([]) == "" && ByteBuffer.GetStringFromUTF16([]) == "" && ByteBuffer.GetStringFromUTF32([]) == "" && ByteBuffer.HexEncode([]) == "", "Empty text.");
    }
    private static void Arrays()
    {
        byte[] golden = [0, 0, 0x80, 0x3F, 0, 0, 0, 0x40, 0, 0, 0x40, 0x40, 0, 0, 0x80, 0x40];
        Check(ByteBuffer.ToFloat32Array(golden).SequenceEqual(new float[] { 1, 2, 3, 4 }), "Binary32 array.");
        Check(ByteBuffer.ToVector2Array(golden).SequenceEqual(new Vector2[] { new(1, 2), new(3, 4) }), "Vector pairs.");
        Check(ByteBuffer.ToVector3Array(golden.AsSpan(0, 12))[0] == new Vector3(1, 2, 3), "Numeric triples.");
        Check(ByteBuffer.ToVector4Array(golden)[0] == new Vector4(1, 2, 3, 4), "Numeric quads.");
        Check(ByteBuffer.ToColorArray(golden)[0] == new Color(1, 2, 3, 4), "RGBA binary32, not packed RGBA8.");
        Check(ByteBuffer.ToInt32Array([255, 255, 255, 255])[0] == -1 && ByteBuffer.ToInt64Array([255, 255, 255, 255, 255, 255, 255, 255])[0] == -1, "Signed arrays.");
        Check(ByteBuffer.ToFloat64Array([0, 0, 0, 0, 0, 0, 0xF8, 0x3F])[0] == 1.5, "Binary64 array.");
        Check(ByteBuffer.ToByteArray(ReadOnlySpan<float>.Empty).Length == 0, "Empty array encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToInt32Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Int32 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToInt64Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Int64 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToFloat32Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Float32 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToFloat64Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Float64 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToVector2Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Vector2 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToVector3Array(golden.AsSpan(0, 12)).AsSpan()).AsSpan().SequenceEqual(golden.AsSpan(0, 12)), "Vector3 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToVector4Array(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Vector4 reverse golden encoding.");
        Check(ByteBuffer.ToByteArray(ByteBuffer.ToColorArray(golden).AsSpan()).AsSpan().SequenceEqual(golden), "Color reverse golden encoding.");
        Check(ByteBuffer.HexEncode(ByteBuffer.ToByteArray(new string[] { "A", "", "é" })) == "410000c3a900", "UTF-8 string array zero terminators.");
        Check(ByteBuffer.ToByteArray(new string[] { "A\0B" }).SequenceEqual(new byte[] { 65, 0, 66, 0 }), "Embedded string zero bytes remain explicit.");
        Reject<ArgumentNullException>(() => ByteBuffer.ToByteArray(new string[] { "valid", null! }));
        Reject<EncoderFallbackException>(() => ByteBuffer.ToByteArray(new string[] { "valid", "\uD800" }));
        var independent = ByteBuffer.ToFloat32Array(golden); golden[3] = 0; Check(independent[0] == 1, "Array ownership.");
        Reject<ArgumentException>(() => ByteBuffer.ToInt32Array([0])); Reject<ArgumentException>(() => ByteBuffer.ToInt64Array([0]));
        Reject<ArgumentException>(() => ByteBuffer.ToFloat32Array([0])); Reject<ArgumentException>(() => ByteBuffer.ToFloat64Array([0]));
        Reject<ArgumentException>(() => ByteBuffer.ToVector2Array([0])); Reject<ArgumentException>(() => ByteBuffer.ToVector3Array([0]));
        Reject<ArgumentException>(() => ByteBuffer.ToVector4Array([0])); Reject<ArgumentException>(() => ByteBuffer.ToColorArray([0]));
        Check(ByteBuffer.ToVector3Array([]).Length == 0 && ByteBuffer.ToInt32Array([]).Length == 0, "Empty arrays.");
    }
    private static void Compression()
    {
        var random = new Random(7142);
        foreach (var mode in Enum.GetValues<FileCompressionMode>())
        {
            Check(ByteBuffer.Compress([], mode).Length == 0, "Empty compression.");
            foreach (var size in new[] { 1, 15, 16, 65535, 65536, 65537, 150000 })
            {
                var data = new byte[size]; random.NextBytes(data); var compressed = ByteBuffer.Compress(data, mode);
                Check(ByteBuffer.Decompress(compressed, size + 1, mode).SequenceEqual(data), "Codec boundary round trip " + mode + "/" + size);
                if (size > 1) Reject<InvalidDataException>(() => ByteBuffer.Decompress(compressed, size - 1, mode));
                if (mode is FileCompressionMode.Deflate or FileCompressionMode.Gzip or FileCompressionMode.Brotli)
                {
                    Check(ByteBuffer.DecompressDynamic(compressed, -1, mode).SequenceEqual(data), "Dynamic round trip.");
                    if (size > 1) Reject<InvalidDataException>(() => ByteBuffer.DecompressDynamic(compressed, size - 1, mode));
                }
                var forged = (byte[])compressed.Clone(); BinaryPrimitives.WriteInt64LittleEndian(forged.AsSpan(8), long.MaxValue);
                Reject<InvalidDataException>(() => ByteBuffer.Decompress(forged, size, mode));
                Reject<InvalidDataException>(() => ByteBuffer.Decompress(compressed.AsSpan(0, 10), size, mode));
                if (mode is FileCompressionMode.FastLz or FileCompressionMode.Zstandard)
                {
                    forged = (byte[])compressed.Clone(); forged[20] ^= 1;
                    Reject<InvalidDataException>(() => ByteBuffer.Decompress(forged, size, mode));
                }
            }
        }
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.Compress([], (FileCompressionMode)99));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.Decompress([], 0)); Reject<InvalidDataException>(() => ByteBuffer.Decompress([], 1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecompressDynamic([], -1));
        Reject<ArgumentOutOfRangeException>(() => ByteBuffer.DecompressDynamic([], 0, FileCompressionMode.Deflate));
        var deflate = ByteBuffer.Compress([1, 2, 3], FileCompressionMode.Deflate);
        Reject<InvalidDataException>(() => ByteBuffer.Decompress(deflate, 3, FileCompressionMode.Gzip));
    }
    private static void Consumers()
    {
        var packet = new byte[24]; ByteBuffer.EncodeU32(packet, 0, 0x12345678); ByteBuffer.EncodeS64(packet, 4, long.MinValue); ByteBuffer.EncodeFloat(packet, 12, 1.5f); ByteBuffer.EncodeDouble(packet, 16, -2.5);
        using (var stream = new StreamPeerBuffer { DataArray = packet })
            Check(stream.GetU32() == 0x12345678 && stream.Get64() == long.MinValue && stream.GetFloat() == 1.5f && stream.GetDouble() == -2.5, "Existing typed stream wire consumer.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-byte-buffer-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var mode in Enum.GetValues<FileCompressionMode>())
            {
                File.WriteAllBytes(path, ByteBuffer.Compress(packet, mode));
                using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode)) Check(file.ReadBytes(packet.Length).SequenceEqual(packet), "Buffer-to-file codec integration.");
                using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Write, mode)) file.WriteBytes(packet);
                Check(ByteBuffer.Decompress(File.ReadAllBytes(path), packet.Length, mode).SequenceEqual(packet), "File-to-buffer codec integration.");
            }
        }
        finally { File.Delete(path); }
        using var receive = new PacketPeerUDP(); using var send = new PacketPeerUDP(); receive.Bind(0, "127.0.0.1"); send.SetDestAddress("127.0.0.1", receive.GetLocalPort()); send.PutPacket(packet);
        var deadline = Stopwatch.GetTimestamp() + 5 * Stopwatch.Frequency;
        while (receive.GetAvailablePacketCount() == 0) { if (Stopwatch.GetTimestamp() > deadline) throw new TimeoutException("UDP loopback"); System.Threading.Thread.Yield(); }
        var received = receive.GetPacket(); Check(ByteBuffer.DecodeU32(received, 0) == 0x12345678 && ByteBuffer.DecodeS64(received, 4) == long.MinValue && ByteBuffer.DecodeFloat(received, 12) == 1.5f && ByteBuffer.DecodeDouble(received, 16) == -2.5, "Actual public UDP packet consumer.");
    }
}
