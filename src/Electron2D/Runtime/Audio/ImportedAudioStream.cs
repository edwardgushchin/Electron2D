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

internal sealed class ImportedAudioStream : AudioStream
{
    private readonly AudioImportMetadata metadata;

    public ImportedAudioStream(AudioImportMetadata metadata)
    {
        this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        TakeOverPath(metadata.SourcePath);
    }

    public AudioImportMetadata Metadata => metadata;

    public bool HasLoop
    {
        get
        {
            ThrowIfFreed();
            return metadata.Loop.Enabled;
        }
    }

    public float LoopBeginSeconds
    {
        get
        {
            ThrowIfFreed();
            return metadata.Loop.BeginSeconds;
        }
    }

    public float LoopEndSeconds
    {
        get
        {
            ThrowIfFreed();
            return metadata.Loop.EndSeconds;
        }
    }

    public override float GetLength()
    {
        ThrowIfFreed();
        return metadata.LengthSeconds;
    }

    public override bool IsMonophonic()
    {
        ThrowIfFreed();
        return metadata.ChannelCount == 1;
    }

    public override bool IsMetaStream()
    {
        ThrowIfFreed();
        return false;
    }

    public override bool CanBeSampled()
    {
        ThrowIfFreed();
        return metadata.Mode == AudioImportMode.Static;
    }
}
