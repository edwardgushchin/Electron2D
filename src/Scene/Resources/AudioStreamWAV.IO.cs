using System.Buffers.Binary;
using System.Text;
using SDL = SDL3.SDL;

namespace Electron2D;

public sealed partial class AudioStreamWAV
{
    /// <summary>Loads validated PCM/IEEE-float RIFF WAVE data with typed import options.</summary>
    /// <param name="streamData">Borrowed RIFF WAVE bytes.</param>
    /// <param name="options">Optional normalize/trim/rate/mono/loop/compression edits.</param>
    /// <returns>A caller-owned audio resource.</returns>
    /// <exception cref="FormatException">Input chunks, codec, dimensions or samples are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Import options are invalid.</exception>
    public static unsafe AudioStreamWAV LoadFromBuffer(ReadOnlySpan<byte> streamData, AudioWAVImportOptions? options = null)
    {
        options ??= new();
        if (streamData.Length < 12 || !streamData[..4].SequenceEqual("RIFF"u8) || !streamData.Slice(8, 4).SequenceEqual("WAVE"u8)) throw new FormatException("A RIFF WAVE header is required.");
        if (options.LimitRate && options.MaxRate <= 0 || options.Compression is { } compression && compression is not (Format.IMAADPCM or Format.QOA) || options.Loop is { } configured && configured is < AudioLoopMode.Disabled or > AudioLoopMode.Backward) throw new ArgumentOutOfRangeException(nameof(options));
        var tags = new Dictionary<string, string>(StringComparer.Ordinal); var loop = AudioLoopMode.Disabled; var begin = 0; var end = 0; var bits = 0; var hasFormat = false; var hasData = false;
        for (var offset = 12; offset + 8 <= streamData.Length;)
        {
            var id = streamData.Slice(offset, 4); var length = BinaryPrimitives.ReadUInt32LittleEndian(streamData.Slice(offset + 4)); offset += 8;
            var available = streamData.Length - offset; var actual = (int)Math.Min(length, (uint)available); var chunk = streamData.Slice(offset, actual);
            if (id.SequenceEqual("fmt "u8))
            {
                if (chunk.Length < 16) throw new FormatException("WAVE format chunk is truncated.");
                var code = BinaryPrimitives.ReadUInt16LittleEndian(chunk); var channels = BinaryPrimitives.ReadUInt16LittleEndian(chunk[2..]); bits = BinaryPrimitives.ReadUInt16LittleEndian(chunk[14..]);
                if (code is not (1 or 3) || channels is < 1 or > 2 || BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]) == 0 || bits is not (8 or 16 or 24 or 32 or 64) || code == 3 && bits is not (32 or 64)) throw new FormatException("Unsupported WAVE sample format.");
                hasFormat = true;
            }
            else if (id.SequenceEqual("data"u8)) { if (!hasFormat) throw new FormatException("WAVE data must follow its format chunk."); hasData = true; }
            else if (id.SequenceEqual("smpl"u8) && options.Loop is null && chunk.Length >= 60 && BinaryPrimitives.ReadUInt32LittleEndian(chunk[28..]) > 0)
            {
                var type = BinaryPrimitives.ReadUInt32LittleEndian(chunk[40..]); loop = type switch { 0 => AudioLoopMode.Forward, 1 => AudioLoopMode.PingPong, 2 => AudioLoopMode.Backward, _ => AudioLoopMode.Disabled };
                begin = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(chunk[44..])); end = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(chunk[48..]));
            }
            else if (id.SequenceEqual("LIST"u8) && chunk.Length >= 4 && chunk[..4].SequenceEqual("INFO"u8))
            {
                for (var index = 4; index + 8 <= chunk.Length;)
                {
                    var key = Encoding.ASCII.GetString(chunk.Slice(index, 4)); var size = BinaryPrimitives.ReadUInt32LittleEndian(chunk[(index + 4)..]); index += 8;
                    if (size > chunk.Length - index) throw new FormatException("WAVE metadata is truncated.");
                    tags[TagName(key)] = Encoding.UTF8.GetString(chunk.Slice(index, (int)size)).TrimEnd('\0'); index = checked(index + (int)size + ((int)size & 1));
                }
            }
            if (length > available && !id.SequenceEqual("data"u8)) throw new FormatException("WAVE chunk exceeds its input boundary.");
            var advance = (long)offset + actual + (length & 1); if (advance > streamData.Length) break; offset = (int)advance;
        }
        if (!hasFormat || !hasData) throw new FormatException("WAVE input lacks format or sample data.");
        float[] samples; int channelsCount, rate;
        fixed (byte* source = streamData)
        {
            var io = SDL.IOFromConstMem((nint)source, (nuint)streamData.Length);
            if (io == 0 || !SDL.LoadWAVIO(io, true, out var spec, out var native, out var length)) throw new FormatException("SDL WAVE decoding failed: " + SDL.GetError());
            try
            {
                channelsCount = spec.Channels; rate = spec.Freq;
                var target = new SDL.AudioSpec { Format = SDL.AudioFormat.AudioF32LE, Channels = channelsCount, Freq = rate };
                if (!SDL.ConvertAudioSamples(in spec, native, checked((int)length), in target, out var converted, out var convertedLength)) throw new FormatException("WAVE PCM conversion failed: " + SDL.GetError());
                try { samples = new float[convertedLength / 4]; new ReadOnlySpan<float>((void*)converted, samples.Length).CopyTo(samples); }
                finally { SDL.Free(converted); }
            }
            finally { SDL.Free(native); }
        }
        if (samples.Any(value => !float.IsFinite(value))) throw new FormatException("WAVE samples must be finite.");
        var frames = samples.Length / channelsCount;
        if (options.LimitRate && rate > options.MaxRate && frames > 0)
        {
            var count = (int)(frames * ((float)options.MaxRate / rate)); var resampled = new float[checked(count * channelsCount)];
            for (var c = 0; c < channelsCount; c++)
            {
                var position = 0; var fraction = 0f;
                for (var i = 0; i < count; i++)
                {
                    resampled[i * channelsCount + c] = Cubic(samples[Math.Max(0, position - 1) * channelsCount + c], samples[position * channelsCount + c], samples[Math.Min(frames - 1, position + 1) * channelsCount + c], samples[Math.Min(frames - 1, position + 2) * channelsCount + c], fraction);
                    fraction += (float)rate / options.MaxRate; var whole = (int)MathF.Floor(fraction); position += whole; fraction -= whole;
                }
            }
            if (loop != AudioLoopMode.Disabled) { begin = (int)(begin * ((float)count / frames)); end = (int)(end * ((float)count / frames)); }
            samples = resampled; rate = options.MaxRate; frames = count;
        }
        if (options.Normalize && samples.Length != 0) { var maximum = samples.Max(MathF.Abs); if (maximum > 0) for (var i = 0; i < samples.Length; i++) samples[i] /= maximum; }
        if (options.Trim && loop == AudioLoopMode.Disabled)
        {
            var first = -1; var last = frames - 1; var threshold = (float)Mathf.DBToLinear(-50);
            for (var i = 0; i < frames; i++) { var sum = 0f; for (var c = 0; c < channelsCount; c++) sum += MathF.Abs(samples[i * channelsCount + c]); if (sum / channelsCount > threshold) { if (first < 0) first = i; last = i; } }
            if (first >= 0 && first < last)
            {
                var trimmed = new float[(last - first) * channelsCount]; for (var i = first; i < last; i++) for (var c = 0; c < channelsCount; c++) trimmed[(i - first) * channelsCount + c] = samples[i * channelsCount + c] * (last - i < 500 ? (last - i - 1) / 500f : 1);
                samples = trimmed; frames = samples.Length / channelsCount;
            }
        }
        if (options.Loop is { } forced) { loop = forced; if (forced != AudioLoopMode.Disabled) { begin = options.LoopBegin < 0 ? Math.Clamp(options.LoopBegin + frames, 0, Math.Max(0, frames - 1)) : options.LoopBegin; end = options.LoopEnd < 0 ? Math.Clamp(options.LoopEnd + frames, 0, Math.Max(0, frames - 1)) : options.LoopEnd; } }
        if (options.ForceMono && channelsCount == 2) { var mono = new float[frames]; for (var i = 0; i < frames; i++) mono[i] = (samples[i * 2] + samples[i * 2 + 1]) / 2; samples = mono; channelsCount = 1; }
        var format = options.Compression ?? (options.Force8Bit || bits == 8 ? Format.PCM8 : Format.PCM16);
        return new AudioStreamWAV { SampleFormat = format, MixRate = rate, Stereo = channelsCount == 2, Loop = loop, LoopBegin = begin, LoopEnd = end, Data = AudioPCMCodec.Encode(samples, format, channelsCount, rate), Tags = tags };
    }
    private static float Cubic(float a, float b, float c, float d, float weight) => b + .5f * weight * (c - a + weight * (2 * a - 5 * b + 4 * c - d + weight * (3 * (b - c) + d - a)));
    private static string TagName(string key) => key switch { "IARL" => "location", "IART" => "artist", "ICMS" => "organization", "ICMT" => "comment", "ICNT" => "releasecountry", "ICOP" => "copyright", "ICRD" => "date", "IENC" => "encodedby", "IENG" => "engineer", "IFRM" => "tracktotal", "IGNR" => "genre", "IKEY" => "keywords", "ILNG" => "language", "IMED" or "ISRF" => "media", "IMUS" => "composer", "INAM" => "title", "IPRD" => "album", "IPRO" => "producer", "IPRT" => "tracknumber", "ISBJ" => "description", "ISFT" => "encoder", _ => key };
    /// <summary>Loads PCM/IEEE-float WAVE data through the engine file-access policy.</summary>
    /// <param name="path">Filesystem, resource or user path.</param>
    /// <param name="options">Optional typed import edits.</param>
    /// <returns>A caller-owned stream.</returns>
    public static AudioStreamWAV LoadFromFile(string path, AudioWAVImportOptions? options = null) => LoadFromBuffer(FileAccess.GetFileAsBytes(path), options);
    /// <summary>Saves internal PCM as an ordinary RIFF WAVE file.</summary>
    /// <param name="path">Engine-accessible output path; .wav is appended when absent.</param>
    /// <exception cref="NotSupportedException">Internal samples are compressed.</exception>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    public void SaveToWAV(string path)
    {
        ArgumentNullException.ThrowIfNull(path); byte[] bytes; int channels, rate; Format format;
        lock (_gate) { ThrowIfDisposed(); format = _format; if (format is Format.IMAADPCM or Format.QOA) throw new NotSupportedException("Compressed internal audio cannot be saved as PCM WAVE."); bytes = (byte[])_data.Clone(); channels = _stereo ? 2 : 1; rate = _mixRate; }
        if (rate <= 0) throw new InvalidOperationException("WAVE output requires a positive sample rate.");
        var targetPath = ProjectSettings.Instance.GlobalizePath(path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? path : path + ".wav");
        var header = new byte[44]; "RIFF"u8.CopyTo(header); BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), checked((uint)bytes.Length + 36)); "WAVEfmt "u8.CopyTo(header.AsSpan(8)); BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), 16); BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(20), 1); BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(22), (ushort)channels); BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(24), (uint)rate);
        var width = format == Format.PCM16 ? 2 : 1; BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(28), checked((uint)(rate * channels * width))); BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(32), (ushort)(channels * width)); BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(34), (ushort)(width * 8)); "data"u8.CopyTo(header.AsSpan(36)); BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(40), (uint)bytes.Length);
        if (format == Format.PCM8) for (var i = 0; i < bytes.Length; i++) bytes[i] = unchecked((byte)(unchecked((sbyte)bytes[i]) + 128)); var payload = new byte[checked(header.Length + bytes.Length)]; header.CopyTo(payload, 0); bytes.CopyTo(payload, header.Length); AtomicFile.Write(targetPath, payload);
    }
}
