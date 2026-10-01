using NV = Electron2D.NVorbisBindings;
using MP3 = Electron2D.NLayerBindings;
using System.Buffers.Binary;

namespace Electron2D;

internal sealed record AudioDecodedPCM(float[] Samples, int Channels, int Rate);

internal static class AudioFileDecoder
{
    internal static AudioDecodedPCM DecodeMP3(byte[] data)
    {
        using var memory = new MemoryStream(data, writable: false); var reader = new MP3.Decoder.MpegStreamReader(memory); var decoder = new MP3.MpegFrameDecoder();
        var channels = reader.Channels; var rate = reader.SampleRate; ValidateFormat(channels, rate);
        var samples = ReadAll(buffer =>
        {
            var frame = reader.NextFrame(); if (frame is null) return 0;
            if (frame.Layer != MP3.MpegLayer.LayerIII || frame.SampleRate != rate || frame.Channels != channels) throw new FormatException("A consistent MPEG Layer III stream is required.");
            return decoder.DecodeFrame(frame, buffer, 0);
        }, channels, data.Length);
        if (reader.SampleCount != samples.Length / channels) throw new FormatException("MPEG frame count differs from the declared stream length.");
        var (delay, padding) = Gapless(data);
        if (delay == 0 && padding == 0) { delay = reader.EncoderDelay; padding = reader.EncoderPadding; }
        if (delay != 0 || padding != 0)
        {
            var begin = checked((delay + 529) * channels); var end = checked(Math.Max(0, padding - 529) * channels);
            if (begin > samples.Length - end) throw new FormatException("MPEG gapless metadata exceeds decoded frames.");
            samples = samples.AsSpan(begin, samples.Length - begin - end).ToArray();
        }
        return new(samples, channels, rate);
    }
    private static (int Delay, int Padding) Gapless(ReadOnlySpan<byte> data)
    {
        var start = 0;
        if (data.Length >= 10 && data[..3].SequenceEqual("ID3"u8)) start = 10 + ((data[6] & 127) << 21) + ((data[7] & 127) << 14) + ((data[8] & 127) << 7) + (data[9] & 127);
        for (var i = start; i + 4 <= data.Length; i++)
        {
            var header = BinaryPrimitives.ReadUInt32BigEndian(data[i..]); if ((header & 0xFFE00000) != 0xFFE00000 || ((header >> 17) & 3) != 1 || ((header >> 19) & 3) == 1) continue;
            var mono = ((header >> 6) & 3) == 3; var mpeg1 = ((header >> 19) & 3) == 3;
            var tag = i + 4 + ((header & 0x10000) == 0 ? 2 : 0) + (mpeg1 ? mono ? 17 : 32 : mono ? 9 : 17);
            if (tag + 8 > data.Length || !(data.Slice(tag, 4).SequenceEqual("Xing"u8) || data.Slice(tag, 4).SequenceEqual("Info"u8))) return (0, 0);
            var flags = BinaryPrimitives.ReadUInt32BigEndian(data[(tag + 4)..]); tag += 8;
            if ((flags & 1) != 0) tag += 4; if ((flags & 2) != 0) tag += 4; if ((flags & 4) != 0) tag += 100; if ((flags & 8) != 0) tag += 4;
            if (tag + 36 > data.Length || !(data.Slice(tag, 4).SequenceEqual("LAME"u8) || data.Slice(tag, 4).SequenceEqual("Lavc"u8) || data.Slice(tag, 4).SequenceEqual("Lavf"u8))) return (0, 0);
            return ((data[tag + 21] << 4) | (data[tag + 22] >> 4), ((data[tag + 22] & 15) << 8) | data[tag + 23]);
        }
        return (0, 0);
    }

    internal static (AudioDecodedPCM PCM, Dictionary<string, string> Tags, long Version) DecodeVorbis(OggPacketSequence sequence)
    {
        var snapshot = sequence.Capture();
        if (snapshot.Pages.Length == 0 || snapshot.Granules.Length != snapshot.Pages.Length) throw new FormatException("Vorbis packet pages and granules are incomplete.");
        var headers = snapshot.Pages.SelectMany(page => page).Take(3).ToArray();
        if (headers.Length != 3 || headers.Where((header, index) => header.Length < 7 || header[0] != index * 2 + 1 || !header.AsSpan(1, 6).SequenceEqual("vorbis"u8)).Any()) throw new FormatException("Vorbis requires three complete ordered headers.");
        using var decoder = new NV.StreamDecoder(new PacketProvider(sequence.InstantiatePlayback())) { ClipSamples = false };
        ValidateFormat(decoder.Channels, decoder.SampleRate);
        var encodedBytes = snapshot.Pages.Sum(page => page.Sum(packet => (long)packet.Length));
        var samples = ReadAll(buffer => decoder.Read(buffer, 0, buffer.Length), decoder.Channels, encodedBytes);
        if (sequence.Version != snapshot.Version) throw new InvalidOperationException("Vorbis packets changed during decoding.");
        if (snapshot.Granules[^1] < 0 || samples.Length / decoder.Channels != snapshot.Granules[^1]) throw new FormatException("Vorbis final granule differs from decoded frames.");
        var tags = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var comment in decoder.Tags.All) if (comment.Value.Count > 0) tags[comment.Key.ToLowerInvariant()] = comment.Value[^1];
        return (new(samples, decoder.Channels, decoder.SampleRate), tags, snapshot.Version);
    }
    private static void ValidateFormat(int channels, int rate) { if (channels is < 1 or > 2 || rate <= 0) throw new FormatException("Only positive-rate mono/stereo compressed audio is supported."); }
    private static float[] ReadAll(Func<float[], int> read, int channels, long encodedBytes)
    {
        var chunks = new List<float[]>(); var scratch = new float[4096 * channels]; long total = 0;
        while (true)
        {
            var count = read(scratch); if (count == 0) break;
            if (count < 0 || count > scratch.Length || count % channels != 0) throw new FormatException("The audio decoder returned an invalid sample count.");
            total += count; if (total > int.MaxValue / sizeof(float) || total > encodedBytes * 4096) throw new FormatException("Decoded audio exceeds bounded sample storage.");
            for (var i = 0; i < count; i++) if (!float.IsFinite(scratch[i])) throw new FormatException("Decoded samples must be finite.");
            chunks.Add(scratch.AsSpan(0, count).ToArray());
        }
        var output = new float[(int)total]; var offset = 0; foreach (var chunk in chunks) { chunk.CopyTo(output, offset); offset += chunk.Length; }
        return output;
    }
    private sealed class PacketProvider(OggPacketSequencePlayback cursor) : NV.Contracts.IPacketProvider, IDisposable
    {
        private Packet? _peek;
        public bool CanSeek => false;
        public int StreamSerial => 1;
        public NV.Contracts.IPacket GetNextPacket() { var packet = PeekNextPacket(); _peek = null; if (packet is not null) cursor.Advance(); return packet!; }
        public NV.Contracts.IPacket PeekNextPacket()
        {
            if (_peek is not null) return _peek;
            if (cursor.Peek() is not { } entry) return null!;
            return _peek = new Packet(entry.Data) { GranulePosition = entry.Granule, IsEndOfStream = entry.EOS };
        }
        public long SeekTo(long granulePos, int preRoll, NV.Contracts.GetPacketGranuleCount getPacketGranuleCount) => throw new NotSupportedException("Cold packet decoding is sequential.");
        public long GetGranuleCount() => cursor.FinalGranule;
        public void Dispose() { _peek = null; cursor.Dispose(); }
    }
    private sealed class Packet(byte[] data) : NV.DataPacket
    {
        private int _offset;
        protected override int TotalBits => checked(data.Length * 8);
        protected override int ReadNextByte() => _offset < data.Length ? data[_offset++] : -1;
        public override void Reset() { _offset = 0; base.Reset(); }
    }
}
