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
namespace Electron2D;

internal enum AudioSourceFormat
{
    Wav,
    OggVorbis
}

internal enum AudioImportMode
{
    Static,
    Streaming
}

internal sealed class AudioImportMetadata
{
    public const string FormatName = "Electron2D.AudioImportMetadata";
    public const int CurrentVersion = 1;

    public AudioImportMetadata(
        string sourcePath,
        long uid,
        AudioSourceFormat format,
        AudioImportMode mode,
        int sampleRate,
        int channelCount,
        int bitsPerSample,
        long sampleCount,
        float lengthSeconds,
        AudioLoopMetadata? loop = null,
        IEnumerable<AudioPlatformPackage>? platformPackages = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Audio import UID must be positive.", nameof(uid));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(bitsPerSample);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        ArgumentOutOfRangeException.ThrowIfNegative(lengthSeconds);

        SourcePath = sourcePath;
        Uid = uid;
        Format = format;
        Mode = mode;
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        BitsPerSample = bitsPerSample;
        SampleCount = sampleCount;
        LengthSeconds = lengthSeconds;
        Loop = loop ?? AudioLoopMetadata.Disabled(lengthSeconds);
        PlatformPackages = (platformPackages ?? Array.Empty<AudioPlatformPackage>())
            .OrderBy(package => package.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public string SourcePath { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public AudioSourceFormat Format { get; }

    public AudioImportMode Mode { get; }

    public int SampleRate { get; }

    public int ChannelCount { get; }

    public int BitsPerSample { get; }

    public long SampleCount { get; }

    public float LengthSeconds { get; }

    public AudioLoopMetadata Loop { get; }

    public IReadOnlyList<AudioPlatformPackage> PlatformPackages { get; }
}

internal sealed class AudioLoopMetadata
{
    public AudioLoopMetadata(bool enabled, float beginSeconds, float endSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginSeconds);
        ArgumentOutOfRangeException.ThrowIfNegative(endSeconds);
        if (endSeconds < beginSeconds)
        {
            throw new ArgumentException("Audio loop end must be greater than or equal to loop begin.", nameof(endSeconds));
        }

        Enabled = enabled;
        BeginSeconds = beginSeconds;
        EndSeconds = endSeconds;
    }

    public bool Enabled { get; }

    public float BeginSeconds { get; }

    public float EndSeconds { get; }

    public static AudioLoopMetadata Disabled(float lengthSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lengthSeconds);
        return new AudioLoopMetadata(enabled: false, beginSeconds: 0f, endSeconds: lengthSeconds);
    }
}

internal sealed class AudioPlatformPackage
{
    public AudioPlatformPackage(string name, string packaging)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(packaging);

        Name = name;
        Packaging = packaging;
    }

    public string Name { get; }

    public string Packaging { get; }
}
