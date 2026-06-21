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

public sealed class AudioServerPublicApiTests
{
    [Fact]
    public void AudioServerExportsOnlyPreviewSurfaceMembers()
    {
        var publicMembers = typeof(Electron2D.AudioServer)
            .GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "GetBusCount",
                "GetBusIndex",
                "GetBusName",
                "GetMixRate",
                "GetOutputLatency",
                "GetSpeakerMode",
                "Lock",
                "SpeakerMode",
                "Unlock"
            },
            publicMembers);
    }

    [Fact]
    public void AudioServerDefaultsExposeMasterBusAndDeviceQueries()
    {
        Assert.Equal(48_000f, Electron2D.AudioServer.GetMixRate());
        Assert.Equal(0f, Electron2D.AudioServer.GetOutputLatency());
        Assert.Equal(Electron2D.AudioServer.SpeakerMode.Stereo, Electron2D.AudioServer.GetSpeakerMode());
        Assert.Equal(1, Electron2D.AudioServer.GetBusCount());
        Assert.Equal("Master", Electron2D.AudioServer.GetBusName(0));
        Assert.Equal(0, Electron2D.AudioServer.GetBusIndex("Master"));
        Assert.Equal(-1, Electron2D.AudioServer.GetBusIndex("Missing"));
    }

    [Fact]
    public void PublicAudioApiDoesNotExposeBackendTrackOrVoiceTypes()
    {
        var exportedTypes = typeof(Electron2D.AudioServer).Assembly.GetExportedTypes();

        Assert.Contains(exportedTypes, type => type.FullName == "Electron2D.AudioServer");
        Assert.Contains(exportedTypes, type => type.FullName == "Electron2D.AudioServer+SpeakerMode");
        Assert.DoesNotContain(exportedTypes, type => type.FullName == "Electron2D.IAudioServerBackend");
        Assert.DoesNotContain(exportedTypes, type => type.FullName == "Electron2D.ManagedAudioServerBackend");
        Assert.DoesNotContain(exportedTypes, type => type.FullName == "Electron2D.AudioVoiceHandle");
        Assert.DoesNotContain(exportedTypes, IsAudioBackendName);

        foreach (var type in exportedTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                AssertPublicSignatureDoesNotExposeAudioBackend(method.ReturnType);
                foreach (var parameter in method.GetParameters())
                {
                    AssertPublicSignatureDoesNotExposeAudioBackend(parameter.ParameterType);
                }
            }
        }
    }

    private static void AssertPublicSignatureDoesNotExposeAudioBackend(Type type)
    {
        var name = type.FullName ?? type.Name;

        Assert.DoesNotContain("AudioVoice", name, StringComparison.Ordinal);
        Assert.DoesNotContain("AudioBackend", name, StringComparison.Ordinal);
        Assert.DoesNotContain("Mixer", name, StringComparison.OrdinalIgnoreCase);
        Assert.False(
            name.Contains("Audio", StringComparison.Ordinal) &&
            name.Contains("Track", StringComparison.Ordinal),
            $"Public signature exposes an audio backend track name: {name}");
    }

    private static bool IsAudioBackendName(Type type)
    {
        var name = type.FullName ?? type.Name;
        return name.Contains("Audio", StringComparison.Ordinal) &&
            (name.Contains("Backend", StringComparison.Ordinal) ||
             name.Contains("Voice", StringComparison.Ordinal) ||
             name.Contains("Track", StringComparison.Ordinal) ||
             name.Contains("Mixer", StringComparison.OrdinalIgnoreCase));
    }
}
