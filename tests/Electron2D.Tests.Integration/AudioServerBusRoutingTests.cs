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
public sealed class AudioServerBusRoutingTests
{
    [Fact]
    public void AudioServerManagesUserBuses()
    {
        ResetAudioServer();

        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(1, "Music");
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(2, "Sfx");
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(3, "Voice");

        Assert.Equal(4, Electron2D.AudioServer.GetBusCount());
        Assert.Equal(1, Electron2D.AudioServer.GetBusIndex("Music"));
        Assert.Equal(2, Electron2D.AudioServer.GetBusIndex("Sfx"));
        Assert.Equal(3, Electron2D.AudioServer.GetBusIndex("Voice"));
        Assert.Equal(new Electron2D.StringName("Master"), Electron2D.AudioServer.GetBusSend(3));

        Electron2D.AudioServer.MoveBus(3, 1);

        Assert.Equal("Voice", Electron2D.AudioServer.GetBusName(1));
        Assert.Equal("Music", Electron2D.AudioServer.GetBusName(2));
        Assert.Equal("Sfx", Electron2D.AudioServer.GetBusName(3));

        Electron2D.AudioServer.RemoveBus(2);

        Assert.Equal(3, Electron2D.AudioServer.GetBusCount());
        Assert.Equal(-1, Electron2D.AudioServer.GetBusIndex("Music"));
        Assert.Equal(1, Electron2D.AudioServer.GetBusIndex("Voice"));
        Assert.Equal(2, Electron2D.AudioServer.GetBusIndex("Sfx"));
    }

    [Fact]
    public void AudioServerRejectsInvalidBusChanges()
    {
        ResetAudioServer();
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(1, "Music");
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(2, "Sfx");

        Assert.Throws<ArgumentOutOfRangeException>(() => Electron2D.AudioServer.SetBusCount(0));
        Assert.Throws<InvalidOperationException>(() => Electron2D.AudioServer.RemoveBus(0));
        Assert.Throws<InvalidOperationException>(() => Electron2D.AudioServer.MoveBus(0, 1));
        Assert.Throws<InvalidOperationException>(() => Electron2D.AudioServer.SetBusName(0, "Main"));
        Assert.Throws<ArgumentException>(() => Electron2D.AudioServer.SetBusName(2, "Music"));
        Assert.Throws<ArgumentException>(() => Electron2D.AudioServer.SetBusName(2, " "));
        Assert.Throws<ArgumentException>(() => Electron2D.AudioServer.SetBusSend(1, new Electron2D.StringName("Missing")));
        Assert.Throws<InvalidOperationException>(() => Electron2D.AudioServer.SetBusSend(1, new Electron2D.StringName("Sfx")));
        Assert.Throws<InvalidOperationException>(() => Electron2D.AudioServer.SetBusSend(0, new Electron2D.StringName("Music")));
        Assert.Throws<ArgumentException>(() => Electron2D.AudioServer.SetBusVolumeDb(1, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Electron2D.AudioServer.SetBusVolumeLinear(1, -0.01f));
    }

    [Fact]
    public void AudioServerAppliesVolumeAlongRoutingPath()
    {
        ResetAudioServer();
        Electron2D.AudioServer.SetBusVolumeDb(0, -3f);
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(1, "Music");
        Electron2D.AudioServer.SetBusVolumeDb(1, -6f);
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(2, "Sfx");
        Electron2D.AudioServer.SetBusSend(2, new Electron2D.StringName("Music"));
        Electron2D.AudioServer.SetBusVolumeDb(2, -2f);

        var voice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 1f),
            new Electron2D.AudioVoicePlayback(
                VolumeDb: -1f,
                PitchScale: 1f,
                Loop: false,
                Bus: new Electron2D.StringName("Sfx")));

        var playback = Electron2D.AudioServer.GetVoicePlayback(voice);

        Assert.Equal(new Electron2D.StringName("Sfx"), playback.Bus);
        Assert.Equal(-12f, playback.VolumeDb, precision: 5);
    }

    [Fact]
    public void AudioServerFallsBackUnknownPlaybackBusToMaster()
    {
        ResetAudioServer();
        Electron2D.AudioServer.SetBusVolumeDb(0, -4f);

        var voice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 1f),
            new Electron2D.AudioVoicePlayback(
                VolumeDb: -2f,
                PitchScale: 1f,
                Loop: false,
                Bus: new Electron2D.StringName("Missing")));

        var playback = Electron2D.AudioServer.GetVoicePlayback(voice);

        Assert.Equal(new Electron2D.StringName("Master"), playback.Bus);
        Assert.Equal(-6f, playback.VolumeDb, precision: 5);
    }

    [Fact]
    public void AudioServerAppliesMuteAndSoloRouting()
    {
        ResetAudioServer();
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(1, "Music");
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(2, "Sfx");
        Electron2D.AudioServer.SetBusSend(2, new Electron2D.StringName("Music"));

        Electron2D.AudioServer.SetBusMute(1, true);

        var mutedVoice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 1f),
            new Electron2D.AudioVoicePlayback(
                VolumeDb: 0f,
                PitchScale: 1f,
                Loop: false,
                Bus: new Electron2D.StringName("Sfx")));

        Assert.Equal(-80f, Electron2D.AudioServer.GetVoicePlayback(mutedVoice).VolumeDb, precision: 5);

        ResetAudioServer();
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(1, "Music");
        Electron2D.AudioServer.AddBus();
        Electron2D.AudioServer.SetBusName(2, "Sfx");
        Electron2D.AudioServer.SetBusSend(2, new Electron2D.StringName("Music"));
        Electron2D.AudioServer.SetBusSolo(1, true);

        var audibleVoice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 1f),
            new Electron2D.AudioVoicePlayback(
                VolumeDb: 0f,
                PitchScale: 1f,
                Loop: false,
                Bus: new Electron2D.StringName("Sfx")));
        var quietVoice = Electron2D.AudioServer.PlayStream(
            new TestAudioStream(length: 1f),
            new Electron2D.AudioVoicePlayback(
                VolumeDb: 0f,
                PitchScale: 1f,
                Loop: false,
                Bus: new Electron2D.StringName("Master")));

        Assert.Equal(0f, Electron2D.AudioServer.GetVoicePlayback(audibleVoice).VolumeDb, precision: 5);
        Assert.Equal(-80f, Electron2D.AudioServer.GetVoicePlayback(quietVoice).VolumeDb, precision: 5);
    }

    private static void ResetAudioServer()
    {
        Electron2D.AudioServer.SetBackend(new Electron2D.ManagedAudioServerBackend());
        Electron2D.AudioServer.SetBusCount(1);
        Electron2D.AudioServer.SetBusVolumeDb(0, 0f);
        Electron2D.AudioServer.SetBusMute(0, false);
        Electron2D.AudioServer.SetBusSolo(0, false);
    }

    private sealed class TestAudioStream(float length) : Electron2D.AudioStream
    {
        public override float GetLength()
        {
            return length;
        }
    }
}
