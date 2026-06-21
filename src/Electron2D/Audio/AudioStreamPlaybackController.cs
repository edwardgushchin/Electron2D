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

internal sealed class AudioStreamPlaybackController
{
    private readonly Func<AudioVoicePlayback, AudioVoicePlayback> playbackTransform;
    private readonly Action finished;
    private readonly List<AudioVoiceHandle> voices = new();
    private AudioStream? stream;
    private StringName bus = new("Master");
    private int maxPolyphony = 1;
    private float pitchScale = 1f;
    private float volumeDb;
    private float playbackPosition;
    private bool playing;
    private bool streamPaused;

    public AudioStreamPlaybackController(
        Func<AudioVoicePlayback, AudioVoicePlayback>? playbackTransform = null,
        Action? finished = null)
    {
        this.playbackTransform = playbackTransform ?? (static playback => playback);
        this.finished = finished ?? (static () => { });
    }

    public AudioStream? Stream
    {
        get => stream;
        set
        {
            if (ReferenceEquals(stream, value))
            {
                return;
            }

            Stop();
            stream = value;
        }
    }

    public bool Autoplay { get; set; }

    public StringName Bus
    {
        get => bus;
        set => bus = value.IsEmpty() ? new StringName("Master") : value;
    }

    public int MaxPolyphony
    {
        get => maxPolyphony;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max polyphony must be at least one.");
            }

            maxPolyphony = value;
            TrimVoicesToLimit();
        }
    }

    public float PitchScale
    {
        get => pitchScale;
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Pitch scale must be finite.", nameof(value));
            }

            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Pitch scale must be greater than zero.");
            }

            pitchScale = value;
        }
    }

    public bool Playing
    {
        get
        {
            CleanupInactiveVoices(emitFinished: true);
            return playing;
        }
        set
        {
            if (value)
            {
                Play(0f);
                return;
            }

            Stop();
        }
    }

    public bool StreamPaused
    {
        get => streamPaused;
        set
        {
            if (streamPaused == value)
            {
                return;
            }

            streamPaused = value;
            if (streamPaused)
            {
                StopActiveVoices();
                return;
            }

            if (playing)
            {
                StartVoice(playbackPosition);
            }
        }
    }

    public float VolumeDb
    {
        get => volumeDb;
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Volume in decibels must be finite.", nameof(value));
            }

            volumeDb = value;
        }
    }

    public float VolumeLinear
    {
        get => DbToLinear(volumeDb);
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Linear volume must be finite.", nameof(value));
            }

            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Linear volume must be non-negative.");
            }

            volumeDb = LinearToDb(value);
        }
    }

    public IReadOnlyList<AudioVoiceHandle> ActiveVoices
    {
        get
        {
            CleanupInactiveVoices(emitFinished: true);
            return voices.ToArray();
        }
    }

    public float GetPlaybackPosition()
    {
        return playbackPosition;
    }

    public bool HasStreamPlayback()
    {
        CleanupInactiveVoices(emitFinished: true);
        return voices.Count > 0;
    }

    public void Play(float fromPosition = 0f)
    {
        ValidatePosition(fromPosition, nameof(fromPosition));

        CleanupInactiveVoices(emitFinished: false);
        if (stream is null)
        {
            Stop();
            return;
        }

        playing = true;
        streamPaused = false;
        playbackPosition = fromPosition;
        StartVoice(fromPosition);
    }

    public void Seek(float toPosition)
    {
        ValidatePosition(toPosition, nameof(toPosition));

        playbackPosition = toPosition;
        if (!playing || streamPaused)
        {
            return;
        }

        StopActiveVoices();
        StartVoice(toPosition);
    }

    public void Stop()
    {
        StopActiveVoices();
        playing = false;
        streamPaused = false;
        playbackPosition = 0f;
    }

    public void OnProcess()
    {
        CleanupInactiveVoices(emitFinished: true);
    }

    private void StartVoice(float startPosition)
    {
        if (stream is null || streamPaused)
        {
            return;
        }

        TrimVoicesToLimit(slotForNewVoice: true);

        var playback = new AudioVoicePlayback(
            VolumeDb: volumeDb,
            PitchScale: pitchScale,
            Loop: HasLoop(stream),
            StartPosition: startPosition,
            Pan: 0f,
            Bus: bus);
        voices.Add(AudioServer.PlayStream(stream, playbackTransform(playback)));
    }

    private void TrimVoicesToLimit(bool slotForNewVoice = false)
    {
        var limit = slotForNewVoice ? maxPolyphony - 1 : maxPolyphony;
        while (voices.Count > limit)
        {
            StopVoiceAt(0);
        }
    }

    private void StopActiveVoices()
    {
        foreach (var voice in voices.ToArray())
        {
            if (AudioServer.IsVoiceActive(voice))
            {
                AudioServer.StopVoice(voice);
            }
        }

        voices.Clear();
    }

    private void StopVoiceAt(int index)
    {
        var voice = voices[index];
        if (AudioServer.IsVoiceActive(voice))
        {
            AudioServer.StopVoice(voice);
        }

        voices.RemoveAt(index);
    }

    private void CleanupInactiveVoices(bool emitFinished)
    {
        for (var index = voices.Count - 1; index >= 0; index--)
        {
            if (!AudioServer.IsVoiceActive(voices[index]))
            {
                voices.RemoveAt(index);
            }
        }

        if (!playing || streamPaused || voices.Count != 0)
        {
            return;
        }

        playing = false;
        playbackPosition = 0f;
        if (emitFinished)
        {
            finished();
        }
    }

    private static bool HasLoop(AudioStream stream)
    {
        return stream is ImportedAudioStream imported && imported.HasLoop;
    }

    private static float DbToLinear(float db)
    {
        return db <= -80f ? 0f : MathF.Pow(10f, db / 20f);
    }

    private static float LinearToDb(float linear)
    {
        return linear <= 0f ? -80f : 20f * MathF.Log10(linear);
    }

    private static void ValidatePosition(float position, string parameterName)
    {
        if (!float.IsFinite(position))
        {
            throw new ArgumentException("Playback position must be finite.", parameterName);
        }

        if (position < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, position, "Playback position must be non-negative.");
        }
    }
}
