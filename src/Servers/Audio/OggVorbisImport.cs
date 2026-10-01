using System.Buffers.Binary;

namespace Electron2D;

internal static class OggVorbisImport
{
    internal static OggPacketSequence Read(ReadOnlySpan<byte> data)
    {
        var pages = new List<byte[][]>(); var granules = new List<long>(); var pending = new List<byte>();
        uint? serial = null; uint? lastPage = null; var eos = false;
        for (var offset = 0; offset < data.Length;)
        {
            if (data.Length - offset < 27 || !data.Slice(offset, 4).SequenceEqual("OggS"u8) || data[offset + 4] != 0) throw new FormatException("Ogg page header is invalid or truncated.");
            var segments = data[offset + 26]; if (data.Length - offset < 27 + segments) throw new FormatException("Ogg lacing is truncated.");
            var size = 0; for (var i = 0; i < segments; i++) size += data[offset + 27 + i];
            var total = 27 + segments + size; if (data.Length - offset < total) throw new FormatException("Ogg page data is truncated.");
            var page = data.Slice(offset, total); if (Checksum(page) != BinaryPrimitives.ReadUInt32LittleEndian(page[22..])) throw new FormatException("Ogg page checksum is invalid.");
            var pageSerial = BinaryPrimitives.ReadUInt32LittleEndian(page[14..]); var number = BinaryPrimitives.ReadUInt32LittleEndian(page[18..]); var flags = page[5];
            if ((flags & ~7) != 0) throw new FormatException("Ogg page flags are invalid.");
            if (serial is null)
            {
                if ((flags & 2) == 0) { if (offset == 0) throw new FormatException("Ogg logical stream lacks its beginning page."); offset += total; continue; }
                serial = pageSerial;
            }
            if (pageSerial == serial && !eos)
            {
                if (lastPage is { } previous && number != unchecked(previous + 1)) throw new FormatException("Ogg page sequence is discontinuous.");
                if (((flags & 1) != 0) != (pending.Count != 0)) throw new FormatException("Ogg continued-packet state is inconsistent.");
                lastPage = number; var packets = new List<byte[]>(); var body = 27 + segments;
                for (var i = 0; i < segments; i++)
                {
                    var count = page[27 + i]; for (var j = 0; j < count; j++) pending.Add(page[body + j]); body += count;
                    if (count < 255) { packets.Add(pending.ToArray()); pending.Clear(); }
                }
                if (pages.Count == 0 && packets.Count > 0 && (packets[0].Length < 7 || packets[0][0] != 1 || !packets[0].AsSpan(1, 6).SequenceEqual("vorbis"u8)))
                { serial = null; lastPage = null; pending.Clear(); }
                else if (packets.Count > 0) { pages.Add(packets.ToArray()); granules.Add(BinaryPrimitives.ReadInt64LittleEndian(page[6..])); }
                eos = serial is not null && (flags & 4) != 0;
            }
            offset += total;
        }
        if (pending.Count != 0 || pages.Count == 0 || !eos) throw new FormatException("Ogg Vorbis packets are incomplete.");
        return new OggPacketSequence { PacketData = pages.ToArray(), GranulePositions = granules.ToArray() };
    }
    private static uint Checksum(ReadOnlySpan<byte> page)
    {
        uint crc = 0;
        for (var i = 0; i < page.Length; i++)
        {
            crc ^= (uint)(i is >= 22 and < 26 ? 0 : page[i]) << 24;
            for (var bit = 0; bit < 8; bit++) crc = (crc << 1) ^ ((crc & 0x80000000) != 0 ? 0x04C11DB7u : 0);
        }
        return crc;
    }
}
