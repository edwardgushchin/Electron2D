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

public sealed class AudioStreamPlayerPublicApiTests
{
    [Fact]
    public void AudioStreamPlayerExportsPreviewPlaybackSurface()
    {
        var properties = typeof(Electron2D.AudioStreamPlayer)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var methods = typeof(Electron2D.AudioStreamPlayer)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(typeof(Electron2D.Node).IsAssignableFrom(typeof(Electron2D.AudioStreamPlayer)));
        Assert.Equal(
            [
                "Autoplay",
                "Bus",
                "MaxPolyphony",
                "PitchScale",
                "Playing",
                "Stream",
                "StreamPaused",
                "VolumeDb",
                "VolumeLinear"
            ],
            properties);
        Assert.Equal(
            [
                "GetPlaybackPosition",
                "HasStreamPlayback",
                "Play",
                "Seek",
                "Stop"
            ],
            methods);
    }

    [Fact]
    public void AudioStreamPlayer2DExportsPreviewSpatialPlaybackSurface()
    {
        var properties = typeof(Electron2D.AudioStreamPlayer2D)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var methods = typeof(Electron2D.AudioStreamPlayer2D)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(typeof(Electron2D.Node2D).IsAssignableFrom(typeof(Electron2D.AudioStreamPlayer2D)));
        Assert.Equal(
            [
                "AreaMask",
                "Attenuation",
                "Autoplay",
                "Bus",
                "MaxDistance",
                "MaxPolyphony",
                "PanningStrength",
                "PitchScale",
                "Playing",
                "Stream",
                "StreamPaused",
                "VolumeDb",
                "VolumeLinear"
            ],
            properties);
        Assert.Equal(
            [
                "GetPlaybackPosition",
                "HasStreamPlayback",
                "Play",
                "Seek",
                "Stop"
            ],
            methods);
    }

    [Fact]
    public void AudioStreamPlayerDefaultsMatchPreviewContract()
    {
        var player = new Electron2D.AudioStreamPlayer();

        Assert.Null(player.Stream);
        Assert.False(player.Autoplay);
        Assert.Equal(new Electron2D.StringName("Master"), player.Bus);
        Assert.Equal(1, player.MaxPolyphony);
        Assert.Equal(1f, player.PitchScale);
        Assert.False(player.Playing);
        Assert.False(player.StreamPaused);
        Assert.Equal(0f, player.VolumeDb);
        Assert.Equal(1f, player.VolumeLinear, precision: 6);
        Assert.Equal(0f, player.GetPlaybackPosition());
        Assert.False(player.HasStreamPlayback());
        Assert.True(player.HasSignal("finished"));
    }

    [Fact]
    public void AudioStreamPlayer2DDefaultsMatchPreviewContract()
    {
        var player = new Electron2D.AudioStreamPlayer2D();

        Assert.Equal(0, player.AreaMask);
        Assert.Equal(1f, player.Attenuation);
        Assert.Equal(2000f, player.MaxDistance);
        Assert.Equal(1f, player.PanningStrength);
        Assert.Null(player.Stream);
        Assert.False(player.Autoplay);
        Assert.Equal(new Electron2D.StringName("Master"), player.Bus);
        Assert.Equal(1, player.MaxPolyphony);
        Assert.Equal(1f, player.PitchScale);
        Assert.False(player.Playing);
        Assert.False(player.StreamPaused);
        Assert.Equal(0f, player.VolumeDb);
        Assert.Equal(1f, player.VolumeLinear, precision: 6);
        Assert.True(player.HasSignal("finished"));
    }

    [Fact]
    public void AudioStreamPlayerRejectsInvalidPlaybackValues()
    {
        var player = new Electron2D.AudioStreamPlayer();

        Assert.Throws<ArgumentOutOfRangeException>(() => { player.MaxPolyphony = 0; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { player.PitchScale = 0f; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { player.VolumeLinear = -0.01f; });
        Assert.Throws<ArgumentException>(() => { player.VolumeDb = float.NaN; });
        Assert.Throws<ArgumentException>(() => { player.VolumeLinear = float.NaN; });
        Assert.Throws<ArgumentException>(() => player.Play(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => player.Seek(-1f));
    }

    [Fact]
    public void AudioStreamPlayer2DRejectsInvalidSpatialValues()
    {
        var player = new Electron2D.AudioStreamPlayer2D();

        Assert.Throws<ArgumentOutOfRangeException>(() => { player.AreaMask = -1; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { player.Attenuation = -0.1f; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { player.MaxDistance = -0.1f; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { player.PanningStrength = -0.1f; });
        Assert.Throws<ArgumentException>(() => { player.Attenuation = float.NaN; });
        Assert.Throws<ArgumentException>(() => { player.MaxDistance = float.NaN; });
        Assert.Throws<ArgumentException>(() => { player.PanningStrength = float.NaN; });
    }
}
