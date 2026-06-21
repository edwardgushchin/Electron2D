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
public sealed class AudioServerVoiceTests
{
    [Fact]
    public void AudioServerCreatesMultipleVoicesAndStopsByOwnHandles()
    {
        ResetBackend();

        var first = new TestAudioStream(length: 0.5f);
        var second = new TestAudioStream(length: 2.0f);

        var firstVoice = Electron2D.AudioServer.PlayStream(
            first,
            new Electron2D.AudioVoicePlayback(VolumeDb: -3f, PitchScale: 1f, Loop: false));
        var secondVoice = Electron2D.AudioServer.PlayStream(
            second,
            new Electron2D.AudioVoicePlayback(VolumeDb: -6f, PitchScale: 1.25f, Loop: true));

        Assert.True(firstVoice.IsValid());
        Assert.True(secondVoice.IsValid());
        Assert.NotEqual(firstVoice, secondVoice);
        Assert.Equal(2, Electron2D.AudioServer.GetActiveVoiceCount());
        Assert.True(Electron2D.AudioServer.IsVoiceActive(firstVoice));
        Assert.True(Electron2D.AudioServer.IsVoiceActive(secondVoice));

        Electron2D.AudioServer.StopVoice(firstVoice);

        Assert.False(Electron2D.AudioServer.IsVoiceActive(firstVoice));
        Assert.True(Electron2D.AudioServer.IsVoiceActive(secondVoice));
        Assert.Equal(1, Electron2D.AudioServer.GetActiveVoiceCount());
        Assert.Throws<ArgumentException>(() => Electron2D.AudioServer.StopVoice(firstVoice));
    }

    [Fact]
    public void AudioServerCleansUpVoicesCompletedByBackend()
    {
        var backend = ResetBackend();
        var firstVoice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 0.25f),
            new Electron2D.AudioVoicePlayback(VolumeDb: 0f, PitchScale: 1f, Loop: false));
        var secondVoice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 0.75f),
            new Electron2D.AudioVoicePlayback(VolumeDb: -1f, PitchScale: 0.5f, Loop: false));

        backend.CompleteAllVoices();
        Electron2D.AudioServer.CleanupFinishedVoices();

        Assert.False(Electron2D.AudioServer.IsVoiceActive(firstVoice));
        Assert.False(Electron2D.AudioServer.IsVoiceActive(secondVoice));
        Assert.Equal(0, Electron2D.AudioServer.GetActiveVoiceCount());
        Assert.Equal(0, backend.ActiveVoiceCount);
    }

    [Theory]
    [InlineData(float.NaN, 1.0f)]
    [InlineData(0.0f, 0.0f)]
    [InlineData(0.0f, -1.0f)]
    [InlineData(0.0f, float.PositiveInfinity)]
    public void AudioServerRejectsInvalidPlaybackParameters(float volumeDb, float pitchScale)
    {
        ResetBackend();

        Assert.Throws<ArgumentException>(
            () => Electron2D.AudioServer.PlayStream(
                new TestAudioStream(length: 1f),
                new Electron2D.AudioVoicePlayback(VolumeDb: volumeDb, PitchScale: pitchScale, Loop: false)));
    }

    [Fact]
    public void AudioServerRejectsFreedStreams()
    {
        ResetBackend();
        var stream = new TestAudioStream(length: 1f);
        stream.Free();

        Assert.Throws<InvalidOperationException>(
            () => Electron2D.AudioServer.PlayStream(
                stream,
                new Electron2D.AudioVoicePlayback(VolumeDb: 0f, PitchScale: 1f, Loop: false)));
    }

    [Fact]
    public void AudioServerSwitchesInternalBackend()
    {
        var backend = new RecordingAudioServerBackend();
        Electron2D.AudioServer.SetBackend(backend);

        try
        {
            var stream = new TestAudioStream(length: 1f);
            var playback = new Electron2D.AudioVoicePlayback(VolumeDb: -8f, PitchScale: 1.5f, Loop: true);

            var voice = Electron2D.AudioServer.PlayStream(stream, playback);

            Assert.Equal("recording", Electron2D.AudioServer.CurrentBackendName);
            Assert.True(voice.IsValid());
            Assert.Equal(44_100f, Electron2D.AudioServer.GetMixRate());
            Assert.Equal(0.0125f, Electron2D.AudioServer.GetOutputLatency());
            Assert.Equal(Electron2D.AudioServer.SpeakerMode.Surround51, Electron2D.AudioServer.GetSpeakerMode());
            Assert.Equal(1, backend.PlayCalls);
            Assert.Same(stream, backend.LastStream);
            Assert.Equal(playback, backend.LastPlayback);
        }
        finally
        {
            ResetBackend();
        }
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

    private sealed class RecordingAudioServerBackend : Electron2D.IAudioServerBackend
    {
        private readonly Dictionary<Electron2D.AudioBackendVoiceHandle, bool> voices = new();
        private long nextVoice = 7000L;

        public string Name => "recording";

        public float MixRate => 44_100f;

        public float OutputLatency => 0.0125f;

        public Electron2D.AudioServer.SpeakerMode SpeakerMode => Electron2D.AudioServer.SpeakerMode.Surround51;

        public int BusCount => 1;

        public int PlayCalls { get; private set; }

        public Electron2D.AudioStream? LastStream { get; private set; }

        public Electron2D.AudioVoicePlayback LastPlayback { get; private set; }

        public string GetBusName(int busIdx)
        {
            return busIdx == 0 ? "Master" : throw new ArgumentOutOfRangeException(nameof(busIdx));
        }

        public int GetBusIndex(string busName)
        {
            return string.Equals(busName, "Master", StringComparison.Ordinal) ? 0 : -1;
        }

        public Electron2D.AudioBackendVoiceHandle Play(Electron2D.AudioStream stream, Electron2D.AudioVoicePlayback playback)
        {
            PlayCalls++;
            LastStream = stream;
            LastPlayback = playback;

            var handle = new Electron2D.AudioBackendVoiceHandle(++nextVoice);
            voices.Add(handle, true);
            return handle;
        }

        public void Stop(Electron2D.AudioBackendVoiceHandle voice)
        {
            voices[voice] = false;
        }

        public bool IsPlaying(Electron2D.AudioBackendVoiceHandle voice)
        {
            return voices.TryGetValue(voice, out var playing) && playing;
        }

        public void Release(Electron2D.AudioBackendVoiceHandle voice)
        {
            voices.Remove(voice);
        }

        public void Lock()
        {
        }

        public void Unlock()
        {
        }
    }
}
