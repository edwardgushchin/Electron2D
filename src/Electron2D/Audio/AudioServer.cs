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
using System.Threading;

namespace Electron2D;

/// <summary>
/// Provides process-wide audio device, bus and voice playback state.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>AudioServer</c> is the public boundary for querying audio output state.
/// It keeps concrete platform backend handles internal to the runtime.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview exposes a single default <c>Master</c> bus and
/// internal voice lifecycle used by future playback nodes. User buses, volume
/// routing and audio effects are added by later audio tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All public methods synchronize access to process-wide audio state. Calls to
/// <see cref="Lock" /> and <see cref="Unlock" /> must be balanced on the same
/// thread.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="AudioStream" />
public static class AudioServer
{
    private static readonly object BackendLock = new();
    private static readonly Dictionary<AudioVoiceHandle, VoiceState> Voices = new();
    private static IAudioServerBackend backend = new ManagedAudioServerBackend();
    private static long nextVoiceId;

    /// <summary>
    /// Identifies the speaker layout used by the active audio output.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The active platform backend reports the closest layout it can guarantee.
    /// Electron2D 0.1.0 Preview defaults to <see cref="Stereo" />.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be used from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum SpeakerMode
    {
        /// <summary>
        /// Two-channel left and right speaker output.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with APIs that accept <see cref="SpeakerMode" />.
        /// </remarks>
        ///
        /// <since>
        /// This enum value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SpeakerMode" />
        Stereo = 0,

        /// <summary>
        /// Four-channel surround output with three front channels and one rear channel.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with APIs that accept <see cref="SpeakerMode" />.
        /// </remarks>
        ///
        /// <since>
        /// This enum value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SpeakerMode" />
        Surround31 = 1,

        /// <summary>
        /// Six-channel surround output with five main channels and one low-frequency channel.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with APIs that accept <see cref="SpeakerMode" />.
        /// </remarks>
        ///
        /// <since>
        /// This enum value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SpeakerMode" />
        Surround51 = 2,

        /// <summary>
        /// Eight-channel surround output with seven main channels and one low-frequency channel.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with APIs that accept <see cref="SpeakerMode" />.
        /// </remarks>
        ///
        /// <since>
        /// This enum value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SpeakerMode" />
        Surround71 = 3
    }

    /// <summary>
    /// Gets the audio mix rate in hertz.
    /// </summary>
    ///
    /// <returns>
    /// The number of output sample frames mixed per second.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The preview backend defaults to <c>48000</c>. Platform backends may
    /// report a different value when the active audio device requires it.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetOutputLatency" />
    /// <seealso cref="GetSpeakerMode" />
    public static float GetMixRate()
    {
        lock (BackendLock)
        {
            return backend.MixRate;
        }
    }

    /// <summary>
    /// Gets the current audio output latency estimate.
    /// </summary>
    ///
    /// <returns>
    /// The estimated output latency in seconds.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is an estimate supplied by the active backend. The default
    /// preview backend reports <c>0</c> because it does not own a physical
    /// output device.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMixRate" />
    public static float GetOutputLatency()
    {
        lock (BackendLock)
        {
            return backend.OutputLatency;
        }
    }

    /// <summary>
    /// Gets the speaker layout reported by the active audio output.
    /// </summary>
    ///
    /// <returns>
    /// The current speaker layout.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The value is a query result only. Changing speaker layout is handled by
    /// platform startup settings and is not exposed in this preview server API.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SpeakerMode" />
    public static SpeakerMode GetSpeakerMode()
    {
        lock (BackendLock)
        {
            return backend.SpeakerMode;
        }
    }

    /// <summary>
    /// Gets the number of audio buses available to the server.
    /// </summary>
    ///
    /// <returns>
    /// The number of buses, including the default <c>Master</c> bus.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Electron2D 0.1.0 Preview exposes only the default bus. User-defined
    /// buses and routing controls are added by the audio bus task.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName" />
    /// <seealso cref="GetBusIndex" />
    public static int GetBusCount()
    {
        lock (BackendLock)
        {
            return backend.BusCount;
        }
    }

    /// <summary>
    /// Gets the name of an audio bus by index.
    /// </summary>
    ///
    /// <param name="busIdx">The zero-based bus index to query.</param>
    ///
    /// <returns>
    /// The bus name.
    /// </returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <remarks>
    /// <para>
    /// The preview server contains only index <c>0</c>, named <c>Master</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetBusCount" />
    /// <seealso cref="GetBusIndex" />
    public static string GetBusName(int busIdx)
    {
        lock (BackendLock)
        {
            return backend.GetBusName(busIdx);
        }
    }

    /// <summary>
    /// Gets the index of an audio bus by name.
    /// </summary>
    ///
    /// <param name="busName">The bus name to search for.</param>
    ///
    /// <returns>
    /// The zero-based bus index, or <c>-1</c> when no bus has the requested name.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="busName" /> is empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="busName" /> is <c>null</c>.
    /// </exception>
    ///
    /// <remarks>
    /// <para>
    /// Bus lookup is case-sensitive. The preview server recognizes
    /// <c>Master</c> and returns <c>-1</c> for every other valid name.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName" />
    public static int GetBusIndex(string busName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(busName);

        lock (BackendLock)
        {
            return backend.GetBusIndex(busName);
        }
    }

    /// <summary>
    /// Locks the audio server for a group of audio changes.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Use this method when several audio operations need to be observed as a
    /// single change by the backend. Every successful call must be paired with
    /// <see cref="Unlock" /> on the same thread.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread, but the matching
    /// <see cref="Unlock" /> must be called from the same thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Unlock" />
    public static void Lock()
    {
        Monitor.Enter(BackendLock);
        try
        {
            backend.Lock();
        }
        catch
        {
            Monitor.Exit(BackendLock);
            throw;
        }
    }

    /// <summary>
    /// Unlocks the audio server after a grouped audio change.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Call this method once for every successful <see cref="Lock" /> call.
    /// Calling it without owning the audio server lock is an error.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method must be called from the same thread that called
    /// <see cref="Lock" />.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Lock" />
    public static void Unlock()
    {
        try
        {
            backend.Unlock();
        }
        finally
        {
            Monitor.Exit(BackendLock);
        }
    }

    internal static string CurrentBackendName
    {
        get
        {
            lock (BackendLock)
            {
                return backend.Name;
            }
        }
    }

    internal static AudioVoiceHandle PlayStream(AudioStream stream, AudioVoicePlayback playback)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidatePlayback(playback);
        ValidateStream(stream);

        lock (BackendLock)
        {
            var backendHandle = backend.Play(stream, playback);
            if (!backendHandle.IsValid())
            {
                throw new InvalidOperationException("Audio backend returned an invalid voice handle.");
            }

            var voice = new AudioVoiceHandle(++nextVoiceId);
            Voices.Add(voice, new VoiceState(stream, playback, backendHandle));
            return voice;
        }
    }

    internal static void StopVoice(AudioVoiceHandle voice)
    {
        lock (BackendLock)
        {
            var state = RequireVoice(voice);
            backend.Stop(state.BackendHandle);
            backend.Release(state.BackendHandle);
            Voices.Remove(voice);
        }
    }

    internal static bool IsVoiceActive(AudioVoiceHandle voice)
    {
        if (!voice.IsValid())
        {
            return false;
        }

        lock (BackendLock)
        {
            return Voices.TryGetValue(voice, out var state) && backend.IsPlaying(state.BackendHandle);
        }
    }

    internal static int GetActiveVoiceCount()
    {
        lock (BackendLock)
        {
            var active = 0;
            foreach (var state in Voices.Values)
            {
                if (backend.IsPlaying(state.BackendHandle))
                {
                    active++;
                }
            }

            return active;
        }
    }

    internal static void CleanupFinishedVoices()
    {
        lock (BackendLock)
        {
            foreach (var item in Voices.ToArray())
            {
                if (backend.IsPlaying(item.Value.BackendHandle))
                {
                    continue;
                }

                backend.Release(item.Value.BackendHandle);
                Voices.Remove(item.Key);
            }
        }
    }

    internal static void SetBackend(IAudioServerBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        lock (BackendLock)
        {
            ReleaseAllVoices();
            AudioServer.backend = backend;
            nextVoiceId = 0L;
        }
    }

    private static void ValidatePlayback(AudioVoicePlayback playback)
    {
        if (!float.IsFinite(playback.VolumeDb))
        {
            throw new ArgumentException("Audio voice volume must be finite.", nameof(playback));
        }

        if (!float.IsFinite(playback.PitchScale) || playback.PitchScale <= 0f)
        {
            throw new ArgumentException("Audio voice pitch scale must be finite and greater than zero.", nameof(playback));
        }
    }

    private static void ValidateStream(AudioStream stream)
    {
        if (!Object.IsInstanceValid(stream))
        {
            throw new InvalidOperationException("Audio stream instance was freed.");
        }

        var length = stream.GetLength();
        if (!float.IsFinite(length) || length < 0f)
        {
            throw new ArgumentException("Audio stream length must be finite and non-negative.", nameof(stream));
        }
    }

    private static VoiceState RequireVoice(AudioVoiceHandle voice)
    {
        if (!voice.IsValid() || !Voices.TryGetValue(voice, out var state))
        {
            throw new ArgumentException($"Audio voice '{voice}' is not alive.", nameof(voice));
        }

        return state;
    }

    private static void ReleaseAllVoices()
    {
        foreach (var state in Voices.Values)
        {
            if (backend.IsPlaying(state.BackendHandle))
            {
                backend.Stop(state.BackendHandle);
            }

            backend.Release(state.BackendHandle);
        }

        Voices.Clear();
    }

    private sealed class VoiceState
    {
        public VoiceState(AudioStream stream, AudioVoicePlayback playback, AudioBackendVoiceHandle backendHandle)
        {
            Stream = stream;
            Playback = playback;
            BackendHandle = backendHandle;
        }

        public AudioStream Stream { get; }

        public AudioVoicePlayback Playback { get; }

        public AudioBackendVoiceHandle BackendHandle { get; }
    }
}
