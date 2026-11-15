using Electron2D;
using static TextureArrayTests;
using IOFileAccess = Electron2D.FileAccess;
using IOPath = System.IO.Path;

internal static class FileBinaryTests
{
    // Each row represents one field in wire order, independent of the runtime encoders.
    private const string LittleEndian = "00FF 0000 FFFF 3412 00000000 FFFFFFFF 78563412 " +
        "0000000000000000 FFFFFFFFFFFFFFFF EFCDAB8967452301 " +
        "0180 007C 557E 01000080 000080FF 4523C17F 0100000000000080 000000000000F07F EFCDAB896745F87F " +
        "00000080 0000803F A5FF00";
    private const string BigEndian = "00FF 0000 FFFF 1234 00000000 FFFFFFFF 12345678 " +
        "0000000000000000 FFFFFFFFFFFFFFFF 0123456789ABCDEF " +
        "8001 7C00 7E55 80000001 FF800000 7FC12345 8000000000000001 7FF0000000000000 7FF8456789ABCDEF " +
        "80000000 3F800000 A5FF00";

    internal static void Run()
    {
        var directory = IOPath.GetFullPath(IOPath.Combine("obj", "file-binary-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var path = IOPath.Combine(directory, "binary");
            foreach (var bigEndian in new[] { false, true })
            {
                var expected = Convert.FromHexString((bigEndian ? BigEndian : LittleEndian).Replace(" ", ""));
                using (var writer = IOFileAccess.Open(path, FileAccessModeFlags.Write))
                {
                    Check(!writer.BigEndian, "Fresh opens default to little endian.");
                    writer.BigEndian = bigEndian;
                    Reject<InvalidOperationException>(() => writer.ReadBytes(0));
                    WriteFields(writer);
                    Check(writer.Position == expected.Length && writer.Length == expected.Length, "Exact write width.");
                }
                Check(File.ReadAllBytes(path).SequenceEqual(expected) && IOFileAccess.GetFileAsBytes(path).SequenceEqual(expected),
                    "Public writes match the independent byte vector.");
                File.WriteAllBytes(path, expected);
                using var reader = IOFileAccess.Open(path, FileAccessModeFlags.Read);
                Check(!reader.BigEndian, "A writer's byte order does not carry over to a new reader.");
                reader.BigEndian = bigEndian;
                Reject<InvalidOperationException>(() => reader.WriteBytes([]));
                Reject<InvalidOperationException>(() => reader.Resize(0));
                ReadFields(reader);
                Check(reader.Position == expected.Length - 3 && !reader.EOFReached, "Exact scalar reads leave the buffer suffix.");
                var prefix = reader.ReadBytes(2);
                prefix[0] = 0;
                Check(reader.ReadBytes(2).SequenceEqual(new byte[] { 0 }) && reader.EOFReached, "Short reads return the available prefix.");
                Check(reader.ReadBytes(0).Length == 0 && reader.EOFReached, "An empty read preserves EOF.");
                reader.SeekEnd(-3);
                Check(reader.ReadBytes(3).SequenceEqual(new byte[] { 0xa5, 0xff, 0 }) && !reader.EOFReached,
                    "Reading to exact end leaves EOF clear and returned buffers own independent bytes.");
                Reject<EndOfStreamException>(() => reader.ReadByte());
                reader.Close();
                reader.Close();
                Check(!reader.IsOpen, "Close is idempotent.");
                Reject<InvalidOperationException>(() => reader.ReadUInt16());
                Reject<InvalidOperationException>(() => _ = reader.Position);
                Reject<InvalidOperationException>(() => _ = reader.Length);
                Reject<InvalidOperationException>(() => _ = reader.EOFReached);
            }
            using (var raw = IOFileAccess.Open(path, FileAccessModeFlags.ReadWrite))
                VerifyCursor(raw);
            using (var compressed = IOFileAccess.OpenCompressed(IOPath.Combine(directory, "compressed"), FileAccessModeFlags.WriteRead, FileCompressionMode.Deflate))
                VerifyCursor(compressed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        Console.WriteLine("FileAccess binary: independent endian vectors, short/exact I/O, modes, cursor, resize and lifecycle passed.");
    }

    private static void WriteFields(IOFileAccess file)
    {
        file.WriteByte(0);
        file.WriteByte(byte.MaxValue);
        file.WriteUInt16(0);
        file.WriteUInt16(ushort.MaxValue);
        file.WriteUInt16(0x1234);
        file.WriteUInt32(0);
        file.WriteUInt32(uint.MaxValue);
        file.WriteUInt32(0x12345678);
        file.WriteUInt64(0);
        file.WriteUInt64(ulong.MaxValue);
        file.WriteUInt64(0x0123456789abcdef);
        foreach (ushort bits in new ushort[] { 0x8001, 0x7c00, 0x7e55 })
            file.WriteHalf(BitConverter.UInt16BitsToHalf(bits));
        foreach (uint bits in new uint[] { 0x80000001, 0xff800000, 0x7fc12345 })
            file.WriteSingle(BitConverter.UInt32BitsToSingle(bits));
        foreach (ulong bits in new ulong[] { 0x8000000000000001, 0x7ff0000000000000, 0x7ff8456789abcdef })
            file.WriteDouble(BitConverter.UInt64BitsToDouble(bits));
        file.WriteReal(-0.0f);
        file.WriteReal(1.0f);
        byte[] source = [0xa5, 0xff, 0];
        file.WriteBytes(source);
        source[0] = 0;
    }

    private static void ReadFields(IOFileAccess file)
    {
        Check(file.ReadByte() == 0 && file.ReadByte() == byte.MaxValue, "Unsigned byte boundaries.");
        Check(file.ReadUInt16() == 0 && file.ReadUInt16() == ushort.MaxValue && file.ReadUInt16() == 0x1234, "Unsigned 16-bit field values.");
        Check(file.ReadUInt32() == 0 && file.ReadUInt32() == uint.MaxValue && file.ReadUInt32() == 0x12345678, "Unsigned 32-bit field values.");
        Check(file.ReadUInt64() == 0 && file.ReadUInt64() == ulong.MaxValue && file.ReadUInt64() == 0x0123456789abcdef, "Unsigned 64-bit field values.");
        foreach (ushort bits in new ushort[] { 0x8001, 0x7c00, 0x7e55 })
            Check(BitConverter.HalfToUInt16Bits(file.ReadHalf()) == bits, "Half subnormal, infinity and NaN payload.");
        foreach (uint bits in new uint[] { 0x80000001, 0xff800000, 0x7fc12345 })
            Check(BitConverter.SingleToUInt32Bits(file.ReadSingle()) == bits, "Single subnormal, infinity and NaN payload.");
        foreach (ulong bits in new ulong[] { 0x8000000000000001, 0x7ff0000000000000, 0x7ff8456789abcdef })
            Check(BitConverter.DoubleToUInt64Bits(file.ReadDouble()) == bits, "Double subnormal, infinity and NaN payload.");
        Check(BitConverter.SingleToUInt32Bits(file.ReadReal()) == 0x80000000 && file.ReadReal() == 1.0f, "Real uses binary32 with signed zero.");
    }

    private static void VerifyCursor(IOFileAccess file)
    {
        file.Seek(0);
        file.WriteBytes([1, 2, 3]);
        file.Resize(3);
        file.Seek(1);
        Reject<EndOfStreamException>(() => file.ReadUInt32());
        Check(file.Position == 3 && file.Length == 3 && file.EOFReached, "A short scalar consumes the available prefix.");
        Reject<ArgumentOutOfRangeException>(() => file.Seek(-1));
        Reject<ArgumentOutOfRangeException>(() => file.Resize(-1));
        Reject<ArgumentOutOfRangeException>(() => file.ReadBytes(-1));
        Reject<IOException>(() => file.SeekEnd(-4));
        Check(file.Position == 3 && file.EOFReached, "Failed operations preserve the cursor and EOF.");
        file.WriteByte(4);
        Check(file.Position == 4 && file.Length == 4 && !file.EOFReached, "Successful write clears EOF.");
        Reject<EndOfStreamException>(() => file.ReadByte());
        file.Resize(2);
        Check(file.Position == 4 && file.Length == 2 && !file.EOFReached, "Shrinking preserves a cursor beyond the new end.");
        file.SeekEnd(3);
        Check(file.Position == 5 && file.Length == 2 && !file.EOFReached, "Seeking past end does not grow the file.");
        Reject<EndOfStreamException>(() => file.ReadByte());
        file.Resize(5);
        Check(file.Position == 5 && file.Length == 5 && !file.EOFReached, "Growing preserves the cursor and clears EOF.");
        file.SeekEnd(-3);
        Check(file.ReadBytes(3).SequenceEqual(new byte[3]), "The grown region is zero-filled.");
        file.SeekEnd();
        Check(file.Position == file.Length && !file.EOFReached, "Default SeekEnd selects exact end.");
        file.Dispose();
        Reject<ObjectDisposedException>(() => file.ReadBytes(0));
        Reject<ObjectDisposedException>(() => file.WriteBytes([]));
        Reject<ObjectDisposedException>(() => file.Seek(0));
        Reject<ObjectDisposedException>(() => file.Resize(0));
        Reject<ObjectDisposedException>(() => _ = file.BigEndian);
    }
}
