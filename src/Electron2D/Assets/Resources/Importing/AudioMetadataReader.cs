/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Buffers.Binary;
using System.Text;

namespace Electron2D;

internal readonly record struct AudioSourceMetadata(
    AudioSourceFormat Format,
    int SampleRate,
    int ChannelCount,
    int BitsPerSample,
    long SampleCount,
    float LengthSeconds);

internal static class AudioMetadataReader
{
    public static AudioSourceMetadata Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var extension = Path.GetExtension(path);
        if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
        {
            return ReadWav(path);
        }

        if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            return ReadOggVorbis(path);
        }

        throw new FormatException($"Audio source extension '{extension}' is not supported.");
    }

    private static AudioSourceMetadata ReadWav(string path)
    {
        using var stream = File.OpenRead(path);
        if (ReadFourCc(stream) != "RIFF")
        {
            throw new FormatException("WAV file must start with RIFF.");
        }

        _ = ReadInt32(stream, "WAV RIFF size");
        if (ReadFourCc(stream) != "WAVE")
        {
            throw new FormatException("WAV file must use WAVE format.");
        }

        WavFormat? format = null;
        long dataLength = -1;
        while (stream.Position + 8 <= stream.Length)
        {
            var chunkId = ReadFourCc(stream);
            var chunkSize = ReadUInt32(stream, "WAV chunk size");
            var chunkStart = stream.Position;
            if (chunkStart + chunkSize > stream.Length)
            {
                throw new FormatException($"WAV chunk '{chunkId}' exceeds the file length.");
            }

            if (chunkId == "fmt ")
            {
                format = ReadWavFormat(stream, chunkSize);
            }
            else if (chunkId == "data")
            {
                dataLength = chunkSize;
            }

            stream.Position = chunkStart + chunkSize + (chunkSize % 2);
        }

        if (format is null)
        {
            throw new FormatException("WAV file does not contain a fmt chunk.");
        }

        if (dataLength < 0)
        {
            throw new FormatException("WAV file does not contain a data chunk.");
        }

        if (format.Value.BlockAlign <= 0)
        {
            throw new FormatException("WAV block align must be greater than zero.");
        }

        var sampleCount = dataLength / format.Value.BlockAlign;
        return new AudioSourceMetadata(
            AudioSourceFormat.Wav,
            format.Value.SampleRate,
            format.Value.ChannelCount,
            format.Value.BitsPerSample,
            sampleCount,
            (float)sampleCount / format.Value.SampleRate);
    }

    private static WavFormat ReadWavFormat(Stream stream, uint chunkSize)
    {
        if (chunkSize < 16)
        {
            throw new FormatException("WAV fmt chunk is too small.");
        }

        Span<byte> data = stackalloc byte[16];
        stream.ReadExactly(data);
        var audioFormat = BinaryPrimitives.ReadUInt16LittleEndian(data);
        if (audioFormat is not (1 or 3))
        {
            throw new FormatException($"WAV audio format '{audioFormat}' is not supported.");
        }

        var channelCount = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
        var sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        var blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
        var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(data[14..]);

        if (channelCount == 0)
        {
            throw new FormatException("WAV channel count must be greater than zero.");
        }

        if (sampleRate == 0)
        {
            throw new FormatException("WAV sample rate must be greater than zero.");
        }

        if (bitsPerSample == 0)
        {
            throw new FormatException("WAV bits per sample must be greater than zero.");
        }

        return new WavFormat(channelCount, (int)sampleRate, bitsPerSample, blockAlign);
    }

    private static AudioSourceMetadata ReadOggVorbis(string path)
    {
        using var stream = File.OpenRead(path);
        var packetBuilder = new MemoryStream();
        VorbisIdentification? identification = null;
        long finalGranule = -1;

        while (stream.Position < stream.Length)
        {
            var page = ReadOggPage(stream);
            if (page.GranulePosition >= 0)
            {
                finalGranule = page.GranulePosition;
            }

            var offset = 0;
            foreach (var segmentLength in page.SegmentTable)
            {
                packetBuilder.Write(page.Payload.AsSpan(offset, segmentLength));
                offset += segmentLength;
                if (segmentLength < 255)
                {
                    var packet = packetBuilder.ToArray();
                    packetBuilder.SetLength(0);
                    identification ??= TryReadVorbisIdentification(packet);
                }
            }
        }

        if (identification is null)
        {
            throw new FormatException("OGG file does not contain a Vorbis identification packet.");
        }

        var sampleCount = Math.Max(0, finalGranule);
        var lengthSeconds = sampleCount == 0
            ? 0f
            : (float)sampleCount / identification.Value.SampleRate;
        return new AudioSourceMetadata(
            AudioSourceFormat.OggVorbis,
            identification.Value.SampleRate,
            identification.Value.ChannelCount,
            BitsPerSample: 0,
            sampleCount,
            lengthSeconds);
    }

    private static OggPage ReadOggPage(Stream stream)
    {
        Span<byte> header = stackalloc byte[27];
        stream.ReadExactly(header);
        if (Encoding.ASCII.GetString(header[..4]) != "OggS")
        {
            throw new FormatException("OGG page must start with OggS.");
        }

        if (header[4] != 0)
        {
            throw new FormatException($"OGG bitstream version '{header[4]}' is not supported.");
        }

        var segmentCount = header[26];
        var segmentTable = new byte[segmentCount];
        stream.ReadExactly(segmentTable);
        var payloadLength = segmentTable.Sum(segment => segment);
        var payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        return new OggPage(
            BinaryPrimitives.ReadInt64LittleEndian(header[6..14]),
            segmentTable,
            payload);
    }

    private static VorbisIdentification? TryReadVorbisIdentification(byte[] packet)
    {
        if (packet.Length < 30 ||
            packet[0] != 0x01 ||
            Encoding.ASCII.GetString(packet, 1, 6) != "vorbis")
        {
            return null;
        }

        var channelCount = packet[11];
        var sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(12, 4));
        if (channelCount == 0)
        {
            throw new FormatException("Vorbis channel count must be greater than zero.");
        }

        if (sampleRate == 0)
        {
            throw new FormatException("Vorbis sample rate must be greater than zero.");
        }

        return new VorbisIdentification(channelCount, (int)sampleRate);
    }

    private static string ReadFourCc(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        return Encoding.ASCII.GetString(bytes);
    }

    private static int ReadInt32(Stream stream, string description)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        if (value < 0)
        {
            throw new FormatException($"{description} must not be negative.");
        }

        return value;
    }

    private static uint ReadUInt32(Stream stream, string description)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        if (value > int.MaxValue)
        {
            throw new FormatException($"{description} is too large.");
        }

        return value;
    }

    private readonly record struct WavFormat(int ChannelCount, int SampleRate, int BitsPerSample, int BlockAlign);

    private readonly record struct VorbisIdentification(int ChannelCount, int SampleRate);

    private readonly record struct OggPage(long GranulePosition, IReadOnlyList<byte> SegmentTable, byte[] Payload);
}
