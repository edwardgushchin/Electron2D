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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class AudioStreamImportTests
{
    [Fact]
    public void AudioStreamImporterImportsWavMetadataLoopAndPackaging()
    {
        using var project = AudioImportTestProject.Create();
        project.WriteBytes("audio/jump.wav", AudioImportTestProject.CreateWav(sampleRate: 48_000, channelCount: 2, bitsPerSample: 16, sampleCount: 96_000));
        project.WriteText(
            "audio/jump.wav.e2import.json",
            """
            {
              "mode": "Static",
              "loop": {
                "enabled": true,
                "begin": 0.25,
                "end": 1.75
              },
              "platforms": [
                { "name": "desktop", "packaging": "copy" },
                { "name": "android", "packaging": "streaming_asset" }
              ]
            }
            """);

        var report = project.CreatePipeline().ImportAll();

        var item = Assert.Single(report.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.NewSource, item.Reason);

        var metadata = project.ReadAudioMetadata(item);
        Assert.Equal("res://audio/jump.wav", metadata.SourcePath);
        Assert.Equal(Electron2D.AudioSourceFormat.Wav, metadata.Format);
        Assert.Equal(Electron2D.AudioImportMode.Static, metadata.Mode);
        Assert.Equal(48_000, metadata.SampleRate);
        Assert.Equal(2, metadata.ChannelCount);
        Assert.Equal(16, metadata.BitsPerSample);
        Assert.Equal(96_000L, metadata.SampleCount);
        Assert.Equal(2f, metadata.LengthSeconds, precision: 6);
        Assert.True(metadata.Loop.Enabled);
        Assert.Equal(0.25f, metadata.Loop.BeginSeconds, precision: 6);
        Assert.Equal(1.75f, metadata.Loop.EndSeconds, precision: 6);
        Assert.Equal(["android", "desktop"], metadata.PlatformPackages.Select(package => package.Name).ToArray());

        var audio = Electron2D.AudioImportResourceFactory.CreateAudioStream(metadata);
        Assert.Equal(2f, audio.GetLength(), precision: 6);
        Assert.False(audio.IsMonophonic());
        Assert.False(audio.IsMetaStream());
        Assert.True(audio.CanBeSampled());
    }

    [Fact]
    public void AudioStreamImporterImportsOggVorbisStreamingMetadata()
    {
        using var project = AudioImportTestProject.Create();
        project.WriteBytes("audio/theme.ogg", AudioImportTestProject.CreateOggVorbis(sampleRate: 44_100, channelCount: 1, sampleCount: 88_200));
        project.WriteText("audio/theme.ogg.e2import.json", "{ \"mode\": \"Streaming\" }");

        var report = project.CreatePipeline().ImportAll();

        var metadata = project.ReadAudioMetadata(Assert.Single(report.Items));
        Assert.Equal(Electron2D.AudioSourceFormat.OggVorbis, metadata.Format);
        Assert.Equal(Electron2D.AudioImportMode.Streaming, metadata.Mode);
        Assert.Equal(44_100, metadata.SampleRate);
        Assert.Equal(1, metadata.ChannelCount);
        Assert.Equal(0, metadata.BitsPerSample);
        Assert.Equal(88_200L, metadata.SampleCount);
        Assert.Equal(2f, metadata.LengthSeconds, precision: 6);
        Assert.False(metadata.Loop.Enabled);
        Assert.Equal(0f, metadata.Loop.BeginSeconds, precision: 6);
        Assert.Equal(2f, metadata.Loop.EndSeconds, precision: 6);
        Assert.Empty(metadata.PlatformPackages);

        var audio = Electron2D.AudioImportResourceFactory.CreateAudioStream(metadata);
        Assert.True(audio.IsMonophonic());
        Assert.False(audio.CanBeSampled());
    }

    [Fact]
    public void AudioStreamImporterTracksSidecarAsDependency()
    {
        using var project = AudioImportTestProject.Create();
        project.WriteBytes("audio/click.wav", AudioImportTestProject.CreateWav(sampleRate: 22_050, channelCount: 1, bitsPerSample: 16, sampleCount: 22_050));
        project.WriteText("audio/click.wav.e2import.json", "{ \"mode\": \"Static\" }");

        var first = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, Assert.Single(first.Items).Status);

        var second = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.UpToDate, Assert.Single(second.Items).Status);

        project.WriteText("audio/click.wav.e2import.json", "{ \"mode\": \"Streaming\" }");

        var third = project.CreatePipeline().ImportAll();
        var item = Assert.Single(third.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.DependencyChanged, item.Reason);

        var metadata = project.ReadAudioMetadata(item);
        Assert.Equal(Electron2D.AudioImportMode.Streaming, metadata.Mode);
    }

    private sealed class AudioImportTestProject : IDisposable
    {
        private AudioImportTestProject(string root)
        {
            Root = root;
            SourceRoot = Path.Combine(root, "sources");
            CacheRoot = Path.Combine(root, ".electron2d", "import-cache");
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(CacheRoot);
        }

        public string Root { get; }

        public string SourceRoot { get; }

        public string CacheRoot { get; }

        public static AudioImportTestProject Create()
        {
            return new AudioImportTestProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.AudioImportTests",
                Guid.NewGuid().ToString("N")));
        }

        public Electron2D.ResourceImportPipeline CreatePipeline()
        {
            return new Electron2D.ResourceImportPipeline(new Electron2D.ResourceImportOptions(
                Root,
                SourceRoot,
                CacheRoot,
                [new Electron2D.AudioStreamImporter()]));
        }

        public Electron2D.AudioImportMetadata ReadAudioMetadata(Electron2D.ResourceImportItemReport item)
        {
            return Electron2D.AudioImportMetadataTextSerializer.Deserialize(File.ReadAllText(Assert.Single(item.CacheFiles)));
        }

        public void WriteBytes(string relativePath, byte[] bytes)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }

        public void WriteText(string relativePath, string text)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public static byte[] CreateWav(int sampleRate, short channelCount, short bitsPerSample, int sampleCount)
        {
            var blockAlign = checked((short)(channelCount * bitsPerSample / 8));
            var byteRate = sampleRate * blockAlign;
            var dataLength = sampleCount * blockAlign;

            using var stream = new MemoryStream();
            stream.Write(Encoding.ASCII.GetBytes("RIFF"));
            WriteInt32(stream, 4 + (8 + 16) + (8 + dataLength));
            stream.Write(Encoding.ASCII.GetBytes("WAVE"));
            stream.Write(Encoding.ASCII.GetBytes("fmt "));
            WriteInt32(stream, 16);
            WriteInt16(stream, 1);
            WriteInt16(stream, channelCount);
            WriteInt32(stream, sampleRate);
            WriteInt32(stream, byteRate);
            WriteInt16(stream, blockAlign);
            WriteInt16(stream, bitsPerSample);
            stream.Write(Encoding.ASCII.GetBytes("data"));
            WriteInt32(stream, dataLength);
            stream.Write(new byte[dataLength]);
            return stream.ToArray();
        }

        public static byte[] CreateOggVorbis(int sampleRate, byte channelCount, long sampleCount)
        {
            var identificationPacket = new byte[30];
            identificationPacket[0] = 0x01;
            Encoding.ASCII.GetBytes("vorbis").CopyTo(identificationPacket, 1);
            identificationPacket[11] = channelCount;
            BinaryPrimitives.WriteInt32LittleEndian(identificationPacket.AsSpan(12, 4), sampleRate);
            identificationPacket[28] = 0x11;
            identificationPacket[29] = 0x01;

            using var stream = new MemoryStream();
            WriteOggPage(stream, headerType: 0x02, granulePosition: 0, sequence: 0, identificationPacket);
            WriteOggPage(stream, headerType: 0x04, granulePosition: sampleCount, sequence: 1, ReadOnlySpan<byte>.Empty);
            return stream.ToArray();
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static void WriteOggPage(Stream stream, byte headerType, long granulePosition, int sequence, ReadOnlySpan<byte> packet)
        {
            stream.Write(Encoding.ASCII.GetBytes("OggS"));
            stream.WriteByte(0);
            stream.WriteByte(headerType);
            WriteInt64(stream, granulePosition);
            WriteInt32(stream, 1);
            WriteInt32(stream, sequence);
            WriteInt32(stream, 0);
            if (packet.Length == 0)
            {
                stream.WriteByte(0);
                return;
            }

            stream.WriteByte(1);
            stream.WriteByte((byte)packet.Length);
            stream.Write(packet);
        }

        private static void WriteInt16(Stream stream, short value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
            stream.Write(bytes);
        }

        private static void WriteInt32(Stream stream, int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            stream.Write(bytes);
        }

        private static void WriteInt64(Stream stream, long value)
        {
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            stream.Write(bytes);
        }
    }
}
