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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class AudioStreamPublicApiTests
{
    [Fact]
    public void AudioStreamExportsOnlyPreviewSurfaceMembers()
    {
        var publicMembers = typeof(Electron2D.AudioStream)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                ".ctor",
                "CanBeSampled",
                "GetLength",
                "IsMetaStream",
                "IsMonophonic"
            },
            publicMembers);
    }

    [Fact]
    public void AudioStreamDefaultMethodsMatchBaseResourceBehavior()
    {
        var stream = new TestAudioStream(length: 1.5f, monophonic: true);

        Assert.Equal(1.5f, stream.GetLength(), precision: 6);
        Assert.True(stream.IsMonophonic());
        Assert.False(stream.IsMetaStream());
        Assert.False(stream.CanBeSampled());
    }

    private sealed class TestAudioStream(float length, bool monophonic) : Electron2D.AudioStream
    {
        public override float GetLength()
        {
            return length;
        }

        public override bool IsMonophonic()
        {
            return monophonic;
        }
    }
}
