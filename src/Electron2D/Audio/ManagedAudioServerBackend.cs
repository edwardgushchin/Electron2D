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

internal sealed class ManagedAudioServerBackend : IAudioServerBackend
{
    private readonly Dictionary<AudioBackendVoiceHandle, VoiceState> voices = new();
    private long nextVoiceId;
    private int lockDepth;

    public string Name => "managed";

    public float MixRate => 48_000f;

    public float OutputLatency => 0f;

    public AudioServer.SpeakerMode SpeakerMode => AudioServer.SpeakerMode.Stereo;

    public int BusCount => 1;

    public int ActiveVoiceCount => voices.Count;

    public int LockDepth => lockDepth;

    public string GetBusName(int busIdx)
    {
        return busIdx == 0
            ? "Master"
            : throw new ArgumentOutOfRangeException(nameof(busIdx), busIdx, "Audio bus index is out of range.");
    }

    public int GetBusIndex(string busName)
    {
        return string.Equals(busName, "Master", StringComparison.Ordinal) ? 0 : -1;
    }

    public AudioBackendVoiceHandle Play(AudioStream stream, AudioVoicePlayback playback)
    {
        var handle = new AudioBackendVoiceHandle(++nextVoiceId);
        voices.Add(handle, new VoiceState(stream, playback));
        return handle;
    }

    public void Stop(AudioBackendVoiceHandle voice)
    {
        Ensure(voice).Playing = false;
    }

    public bool IsPlaying(AudioBackendVoiceHandle voice)
    {
        return voices.TryGetValue(voice, out var state) && state.Playing;
    }

    public void Release(AudioBackendVoiceHandle voice)
    {
        if (!voices.Remove(voice))
        {
            throw new ArgumentException($"Audio backend voice '{voice}' is not alive.", nameof(voice));
        }
    }

    public void Lock()
    {
        lockDepth++;
    }

    public void Unlock()
    {
        if (lockDepth > 0)
        {
            lockDepth--;
        }
    }

    public void CompleteAllVoices()
    {
        foreach (var state in voices.Values)
        {
            state.Playing = false;
        }
    }

    private VoiceState Ensure(AudioBackendVoiceHandle voice)
    {
        if (!voices.TryGetValue(voice, out var state))
        {
            throw new ArgumentException($"Audio backend voice '{voice}' is not alive.", nameof(voice));
        }

        return state;
    }

    private sealed class VoiceState
    {
        public VoiceState(AudioStream stream, AudioVoicePlayback playback)
        {
            Stream = stream;
            Playback = playback;
        }

        public AudioStream Stream { get; }

        public AudioVoicePlayback Playback { get; }

        public bool Playing { get; set; } = true;
    }
}
