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
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(AudioServerCollection.Name)]
public sealed class AudioStreamPlayerPlaybackTests
{
    [Fact]
    public void AudioStreamPlayerPlaysPausesResumesSeeksAndStopsStream()
    {
        ResetBackend();
        var player = new Electron2D.AudioStreamPlayer
        {
            Stream = new TestAudioStream(length: 3f),
            VolumeDb = -6f,
            PitchScale = 1.5f
        };

        player.Play(0.25f);

        var voice = Assert.Single(player.ActiveVoices);
        var playback = Electron2D.AudioServer.GetVoicePlayback(voice);
        Assert.True(player.Playing);
        Assert.True(player.HasStreamPlayback());
        Assert.Equal(0.25f, player.GetPlaybackPosition(), precision: 6);
        Assert.Equal(-6f, playback.VolumeDb);
        Assert.Equal(1.5f, playback.PitchScale);
        Assert.Equal(0.25f, playback.StartPosition);
        Assert.False(playback.Loop);

        player.StreamPaused = true;

        Assert.True(player.Playing);
        Assert.False(player.HasStreamPlayback());
        Assert.Empty(player.ActiveVoices);
        Assert.Equal(0, Electron2D.AudioServer.GetActiveVoiceCount());

        player.StreamPaused = false;

        Assert.True(player.Playing);
        Assert.True(player.HasStreamPlayback());
        Assert.Equal(0.25f, Electron2D.AudioServer.GetVoicePlayback(Assert.Single(player.ActiveVoices)).StartPosition);

        player.Seek(1.25f);

        Assert.Equal(1.25f, player.GetPlaybackPosition(), precision: 6);
        Assert.Equal(1.25f, Electron2D.AudioServer.GetVoicePlayback(Assert.Single(player.ActiveVoices)).StartPosition);

        player.Stop();

        Assert.False(player.Playing);
        Assert.False(player.HasStreamPlayback());
        Assert.Equal(0f, player.GetPlaybackPosition(), precision: 6);
        Assert.Equal(0, Electron2D.AudioServer.GetActiveVoiceCount());
    }

    [Fact]
    public void AudioStreamPlayerPlayingPropertyStartsAndStopsPlayback()
    {
        ResetBackend();
        var player = new Electron2D.AudioStreamPlayer
        {
            Stream = new TestAudioStream(length: 1f)
        };

        player.Playing = true;

        Assert.True(player.Playing);
        Assert.True(player.HasStreamPlayback());

        player.Playing = false;

        Assert.False(player.Playing);
        Assert.False(player.HasStreamPlayback());
    }

    [Fact]
    public void AudioStreamPlayerCutsOldestVoiceWhenMaxPolyphonyIsReached()
    {
        ResetBackend();
        var player = new Electron2D.AudioStreamPlayer
        {
            Stream = new TestAudioStream(length: 1f),
            MaxPolyphony = 1
        };

        player.Play();
        var first = Assert.Single(player.ActiveVoices);

        player.Play(0.5f);
        var second = Assert.Single(player.ActiveVoices);

        Assert.NotEqual(first, second);
        Assert.False(Electron2D.AudioServer.IsVoiceActive(first));
        Assert.True(Electron2D.AudioServer.IsVoiceActive(second));
        Assert.Equal(0.5f, Electron2D.AudioServer.GetVoicePlayback(second).StartPosition);
    }

    [Fact]
    public void AudioStreamPlayerUsesImportedLoopMetadata()
    {
        ResetBackend();
        var stream = Electron2D.AudioImportResourceFactory.CreateAudioStream(new Electron2D.AudioImportMetadata(
            sourcePath: "res://audio/theme.ogg",
            uid: 123L,
            format: Electron2D.AudioSourceFormat.OggVorbis,
            mode: Electron2D.AudioImportMode.Streaming,
            sampleRate: 48_000,
            channelCount: 2,
            bitsPerSample: 0,
            sampleCount: 96_000,
            lengthSeconds: 2f,
            loop: new Electron2D.AudioLoopMetadata(enabled: true, beginSeconds: 0.25f, endSeconds: 1.75f)));
        var player = new Electron2D.AudioStreamPlayer
        {
            Stream = stream
        };

        player.Play();

        Assert.True(Electron2D.AudioServer.GetVoicePlayback(Assert.Single(player.ActiveVoices)).Loop);
    }

    [Fact]
    public void AudioStreamPlayer2DCalculatesEffectiveVolumeAndPan()
    {
        ResetBackend();
        var player = new Electron2D.AudioStreamPlayer2D
        {
            Stream = new TestAudioStream(length: 1f),
            Position = new Electron2D.Vector2(100f, 0f),
            MaxDistance = 200f,
            Attenuation = 1f,
            PanningStrength = 0.5f,
            VolumeDb = 0f
        };

        player.Play();

        var playback = Electron2D.AudioServer.GetVoicePlayback(Assert.Single(player.ActiveVoices));
        Assert.Equal(-6.0206f, playback.VolumeDb, precision: 3);
        Assert.Equal(0.25f, playback.Pan, precision: 6);
    }

    [Fact]
    public void AudioStreamPlayerAutoplayStartsOnEnterTreeAndStopsOnExitTree()
    {
        ResetBackend();
        var tree = new Electron2D.SceneTree();
        var player = new Electron2D.AudioStreamPlayer
        {
            Stream = new TestAudioStream(length: 1f),
            Autoplay = true
        };

        tree.Root.AddChild(player);

        Assert.True(player.Playing);
        Assert.True(player.HasStreamPlayback());

        tree.Root.RemoveChild(player);

        Assert.False(player.Playing);
        Assert.False(player.HasStreamPlayback());
        Assert.Equal(0, Electron2D.AudioServer.GetActiveVoiceCount());
    }

    private static Electron2D.ManagedAudioServerBackend ResetBackend()
    {
        var backend = new Electron2D.ManagedAudioServerBackend();
        Electron2D.AudioServer.SetBackend(backend);
        return backend;
    }

    private sealed class TestAudioStream(float length) : Electron2D.AudioStream
    {
        public override float GetLength()
        {
            return length;
        }
    }
}
