using System.Buffers.Binary;
using Q = Electron2D.QOABindings;

namespace Electron2D;

internal static class AudioPCMCodec
{
    private static readonly int[] Steps = [7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767];
    private static readonly int[] Indices = [-1, -1, -1, -1, 2, 4, 6, 8];
    internal static float[] Decode(byte[] data, AudioStreamWAV.Format format, int channels)
    {
        if (data.Length == 0) return [];
        if (format == AudioStreamWAV.Format.QOA)
        {
            var decoder = new Decoder(data);
            if (!decoder.ReadHeader() || decoder.GetChannels() != channels) throw new FormatException("QOA header or channel count is invalid.");
            var frames = decoder.GetTotalSamples(); if (frames > data.Length * 20L || frames > int.MaxValue / channels) throw new FormatException("QOA sample count exceeds bounded input storage.");
            var result = new float[frames * channels]; var scratch = new short[Q.QOABase.MaxFrameSamples * channels]; var offset = 0;
            while (!decoder.IsEnd())
            {
                var count = decoder.ReadFrame(scratch); if (count <= 0) throw new FormatException("QOA frame data is truncated or invalid.");
                for (var i = 0; i < count * channels; i++) result[offset++] = scratch[i] / 32767f;
            }
            return result;
        }
        var countSamples = format == AudioStreamWAV.Format.IMAADPCM ? checked(data.Length * 2) : format == AudioStreamWAV.Format.PCM16 ? data.Length / 2 : data.Length;
        countSamples -= countSamples % channels; var samples = new float[countSamples];
        if (format == AudioStreamWAV.Format.IMAADPCM)
        {
            if (data.Length % channels != 0) throw new FormatException("IMA channel-byte groups are truncated.");
            Span<int> predictor = stackalloc int[2]; Span<int> stepIndex = stackalloc int[2]; predictor.Clear(); stepIndex.Clear();
            for (var frame = 0; frame < countSamples / channels; frame++) for (var c = 0; c < channels; c++)
                {
                    var value = data[(frame / 2) * channels + c]; var nibble = (frame & 1) == 0 ? value & 15 : value >> 4;
                    var step = Steps[stepIndex[c]]; var diff = step >> 3;
                    if ((nibble & 1) != 0) diff += step >> 2; if ((nibble & 2) != 0) diff += step >> 1; if ((nibble & 4) != 0) diff += step;
                    predictor[c] = Math.Clamp(predictor[c] + ((nibble & 8) == 0 ? diff : -diff), -32768, 32767);
                    stepIndex[c] = Math.Clamp(stepIndex[c] + Indices[nibble & 7], 0, 88); samples[frame * channels + c] = predictor[c] / 32767f;
                }
        }
        else for (var i = 0; i < samples.Length; i++) samples[i] = format == AudioStreamWAV.Format.PCM8 ? unchecked((sbyte)data[i]) * (256f / 32767) : BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(i * 2)) / 32767f;
        return samples;
    }
    internal static byte[] Encode(float[] samples, AudioStreamWAV.Format format, int channels, int rate)
    {
        if (samples.Length == 0) return [];
        if (format == AudioStreamWAV.Format.QOA)
        {
            var encoder = new Encoder(); var frames = samples.Length / channels;
            if (!encoder.WriteHeader(frames, channels, rate)) throw new ArgumentException("QOA sample dimensions are invalid.");
            var scratch = new short[Q.QOABase.MaxFrameSamples * channels];
            for (var offset = 0; offset < frames; offset += Q.QOABase.MaxFrameSamples)
            {
                var count = Math.Min(Q.QOABase.MaxFrameSamples, frames - offset);
                for (var i = 0; i < count * channels; i++) scratch[i] = Quantize(samples[offset * channels + i]);
                if (!encoder.WriteFrame(scratch, count)) throw new InvalidOperationException("QOA encoding failed.");
            }
            return encoder.Bytes.ToArray();
        }
        if (format == AudioStreamWAV.Format.IMAADPCM)
        {
            var frames = samples.Length / channels; var result = new byte[checked(((frames + 1) / 2) * channels)];
            Span<int> predictor = stackalloc int[2]; Span<int> index = stackalloc int[2]; predictor.Clear(); index.Clear();
            for (var frame = 0; frame < frames; frame++) for (var c = 0; c < channels; c++)
                {
                    var target = Quantize(samples[frame * channels + c]); var delta = target - predictor[c]; var nibble = delta < 0 ? 8 : 0; delta = Math.Abs(delta);
                    var step = Steps[index[c]]; var diff = step >> 3;
                    if (delta >= step) { nibble |= 4; delta -= step; diff += step; }
                    if (delta >= step >> 1) { nibble |= 2; delta -= step >> 1; diff += step >> 1; }
                    if (delta >= step >> 2) { nibble |= 1; diff += step >> 2; }
                    predictor[c] = Math.Clamp(predictor[c] + ((nibble & 8) == 0 ? diff : -diff), -32768, 32767); index[c] = Math.Clamp(index[c] + Indices[nibble & 7], 0, 88);
                    result[(frame / 2) * channels + c] |= (byte)(nibble << ((frame & 1) * 4));
                }
            return result;
        }
        var output = new byte[checked(samples.Length * (format == AudioStreamWAV.Format.PCM16 ? 2 : 1))];
        for (var i = 0; i < samples.Length; i++)
            if (format == AudioStreamWAV.Format.PCM16) BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(i * 2), (short)Math.Clamp((int)(samples[i] * 32768), -32768, 32767));
            else output[i] = unchecked((byte)(sbyte)Math.Clamp((int)(samples[i] * 128), -128, 127));
        return output;
    }
    private static short Quantize(float value) => (short)Math.Clamp((int)(value * 32767), -32768, 32767);
    private sealed class Decoder(byte[] data) : Q.QOADecoder
    {
        private int _position;
        protected override int ReadByte() => _position < data.Length ? data[_position++] : -1;
        protected override void SeekToByte(int position) { if ((uint)position > (uint)data.Length) throw new ArgumentOutOfRangeException(nameof(position)); _position = position; }
    }
    private sealed class Encoder : Q.QOAEncoder
    {
        internal readonly List<byte> Bytes = [];
        protected override bool WriteLong(long value) { for (var shift = 56; shift >= 0; shift -= 8) Bytes.Add((byte)(value >> shift)); return true; }
    }
}
