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
/// Electron2D 0.1-preview exposes the default <c>Master</c> bus,
/// user-defined buses, simple volume routing, mute and solo state. Audio
/// effects and editor-side bus layout editing are not part of this preview
/// server surface.
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
/// This class is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="AudioStream" />
/// <seealso cref="AudioStreamPlayer" />
/// <seealso cref="AudioStreamPlayer2D" />
public static class AudioServer
{
    private const string MasterBusName = "Master";
    private const float QuietVolumeDb = -80f;
    private static readonly object BackendLock = new();
    private static readonly List<AudioBus> Buses = new() { AudioBus.CreateMaster() };
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
    /// Electron2D 0.1-preview defaults to <see cref="Stereo" />.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be used from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
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
        /// This enum value is available since Electron2D 0.1-preview.
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
        /// This enum value is available since Electron2D 0.1-preview.
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
        /// This enum value is available since Electron2D 0.1-preview.
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
        /// This enum value is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// The bus count is always at least <c>1</c> because <c>Master</c> cannot
    /// be removed. User buses are stored in process-wide audio server state and
    /// are applied to voices started after a bus change.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName" />
    /// <seealso cref="GetBusIndex" />
    public static int GetBusCount()
    {
        lock (BackendLock)
        {
            return Buses.Count;
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
    /// Index <c>0</c> is always named <c>Master</c>. User buses occupy
    /// positive indices and may be reordered with <see cref="MoveBus(int, int)" />.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusCount" />
    /// <seealso cref="GetBusIndex" />
    public static string GetBusName(int busIdx)
    {
        lock (BackendLock)
        {
            return RequireBus(busIdx).Name;
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
    /// Bus lookup is case-sensitive. The method returns <c>-1</c> when no bus
    /// currently has <paramref name="busName" />.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName" />
    public static int GetBusIndex(string busName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(busName);

        lock (BackendLock)
        {
            return FindBusIndex(busName.Trim());
        }
    }

    /// <summary>
    /// Changes the number of audio buses.
    /// </summary>
    ///
    /// <param name="amount">
    /// The requested bus count, including the <c>Master</c> bus.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// The value must be at least <c>1</c>. Increasing the count appends
    /// user buses routed to <c>Master</c>. Decreasing the count removes buses
    /// from the end and reroutes remaining buses whose send target was removed.
    /// </para>
    /// <para>
    /// The change applies to voices started after the bus layout update.
    /// Existing voices keep the playback snapshot created when they started.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="amount" /> is less than <c>1</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusCount" />
    /// <seealso cref="AddBus(int)" />
    /// <seealso cref="RemoveBus(int)" />
    public static void SetBusCount(int amount)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Audio server must keep at least the Master bus.");
        }

        lock (BackendLock)
        {
            if (amount < Buses.Count)
            {
                Buses.RemoveRange(amount, Buses.Count - amount);
                NormalizeInvalidBusSends();
                return;
            }

            while (Buses.Count < amount)
            {
                Buses.Add(AudioBus.CreateUser(NextDefaultBusName(), new StringName(MasterBusName)));
            }
        }
    }

    /// <summary>
    /// Adds a user audio bus.
    /// </summary>
    ///
    /// <param name="atPosition">
    /// The insertion index, or <c>-1</c> to append the bus at the end.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// <c>Master</c> is fixed at index <c>0</c>, so explicit insertion
    /// positions must be from <c>1</c> through <see cref="GetBusCount" />.
    /// The new bus gets a unique default name and sends to <c>Master</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="atPosition" /> is outside the valid range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusName(int, string)" />
    /// <seealso cref="SetBusSend(int, StringName)" />
    public static void AddBus(int atPosition = -1)
    {
        lock (BackendLock)
        {
            var insertionIndex = atPosition == -1 ? Buses.Count : atPosition;
            if (insertionIndex < 1 || insertionIndex > Buses.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(atPosition), atPosition, "Audio bus insertion position is out of range.");
            }

            Buses.Insert(insertionIndex, AudioBus.CreateUser(NextDefaultBusName(), new StringName(MasterBusName)));
            NormalizeInvalidBusSends();
        }
    }

    /// <summary>
    /// Removes a user audio bus.
    /// </summary>
    ///
    /// <param name="index">
    /// The bus index to remove.
    /// </param>
    ///
    /// <remarks>
    /// Removing a bus reroutes remaining buses that sent to it back to
    /// <c>Master</c>. The <c>Master</c> bus at index <c>0</c> cannot be
    /// removed.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> does not identify an existing bus.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="index" /> is <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddBus(int)" />
    /// <seealso cref="SetBusCount(int)" />
    public static void RemoveBus(int index)
    {
        lock (BackendLock)
        {
            RequireUserBusIndex(index, nameof(index));
            var removedName = Buses[index].Name;
            Buses.RemoveAt(index);
            foreach (var bus in Buses)
            {
                if (StringNameEquals(bus.Send, removedName))
                {
                    bus.Send = new StringName(MasterBusName);
                }
            }

            NormalizeInvalidBusSends();
        }
    }

    /// <summary>
    /// Moves a user audio bus to a different index.
    /// </summary>
    ///
    /// <param name="index">
    /// The current bus index.
    /// </param>
    /// <param name="toIndex">
    /// The target bus index.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// The <c>Master</c> bus remains fixed at index <c>0</c>. If moving a bus
    /// would leave any user bus sending to itself or to a bus on its right, that
    /// send target is reset to <c>Master</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either index is outside the valid bus range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="index" /> or <paramref name="toIndex" /> is
    /// <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName(int)" />
    /// <seealso cref="SetBusSend(int, StringName)" />
    public static void MoveBus(int index, int toIndex)
    {
        lock (BackendLock)
        {
            RequireUserBusIndex(index, nameof(index));
            RequireUserBusIndex(toIndex, nameof(toIndex));
            if (index == toIndex)
            {
                return;
            }

            var bus = Buses[index];
            Buses.RemoveAt(index);
            Buses.Insert(toIndex, bus);
            NormalizeInvalidBusSends();
        }
    }

    /// <summary>
    /// Changes the name of a user audio bus.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to rename.
    /// </param>
    /// <param name="name">
    /// The new bus name.
    /// </param>
    ///
    /// <remarks>
    /// Bus names are case-sensitive and must be unique. Renaming a bus updates
    /// other bus send targets that referenced the old name.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is empty, whitespace-only or already
    /// used by another bus.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="busIdx" /> is <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusName(int)" />
    public static void SetBusName(int busIdx, string name)
    {
        var normalizedName = NormalizeBusName(name, nameof(name));

        lock (BackendLock)
        {
            RequireUserBusIndex(busIdx, nameof(busIdx));
            var existingIndex = FindBusIndex(normalizedName);
            if (existingIndex >= 0 && existingIndex != busIdx)
            {
                throw new ArgumentException($"Audio bus name '{normalizedName}' is already in use.", nameof(name));
            }

            var oldName = Buses[busIdx].Name;
            if (string.Equals(oldName, normalizedName, StringComparison.Ordinal))
            {
                return;
            }

            Buses[busIdx].Name = normalizedName;
            foreach (var bus in Buses)
            {
                if (StringNameEquals(bus.Send, oldName))
                {
                    bus.Send = new StringName(normalizedName);
                }
            }
        }
    }

    /// <summary>
    /// Changes the target bus that receives a user bus output.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to route.
    /// </param>
    /// <param name="send">
    /// The target bus name, or an empty value to use <c>Master</c> for user
    /// buses.
    /// </param>
    ///
    /// <remarks>
    /// User buses may only send to an existing bus on their left. This keeps the
    /// routing graph acyclic. <c>Master</c> accepts only an empty send target.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="send" /> names no existing bus.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested route would point from <c>Master</c> to another
    /// bus, to the same bus, or to a bus on the right.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusSend(int)" />
    public static void SetBusSend(int busIdx, StringName send)
    {
        lock (BackendLock)
        {
            RequireBusIndex(busIdx, nameof(busIdx));
            if (busIdx == 0)
            {
                if (!send.IsEmpty())
                {
                    throw new InvalidOperationException("Master bus cannot send to another bus.");
                }

                Buses[0].Send = default;
                return;
            }

            var targetName = send.IsEmpty() ? MasterBusName : NormalizeBusName(send.ToString(), nameof(send));
            var targetIndex = FindBusIndex(targetName);
            if (targetIndex < 0)
            {
                throw new ArgumentException($"Audio bus send target '{targetName}' does not exist.", nameof(send));
            }

            if (targetIndex >= busIdx)
            {
                throw new InvalidOperationException("Audio bus send target must be to the left of the routed bus.");
            }

            Buses[busIdx].Send = new StringName(targetName);
        }
    }

    /// <summary>
    /// Gets the target bus that receives a bus output.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to query.
    /// </param>
    ///
    /// <returns>
    /// The target bus name, or an empty <see cref="StringName" /> for
    /// <c>Master</c>.
    /// </returns>
    ///
    /// <remarks>
    /// User buses default to sending to <c>Master</c>. <c>Master</c> has no
    /// send target and returns an empty value.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusSend(int, StringName)" />
    public static StringName GetBusSend(int busIdx)
    {
        lock (BackendLock)
        {
            return RequireBus(busIdx).Send;
        }
    }

    /// <summary>
    /// Sets a bus volume offset in decibels.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to change.
    /// </param>
    /// <param name="volumeDb">
    /// The finite decibel offset.
    /// </param>
    ///
    /// <remarks>
    /// The value is added to the voice volume and to every other bus volume on
    /// the routing path. Setting volume on <c>Master</c> changes the global
    /// output level for future voices.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="volumeDb" /> is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusVolumeDb(int)" />
    /// <seealso cref="SetBusVolumeLinear(int, float)" />
    public static void SetBusVolumeDb(int busIdx, float volumeDb)
    {
        if (!float.IsFinite(volumeDb))
        {
            throw new ArgumentException("Audio bus volume must be finite.", nameof(volumeDb));
        }

        lock (BackendLock)
        {
            RequireBus(busIdx).VolumeDb = volumeDb;
        }
    }

    /// <summary>
    /// Gets a bus volume offset in decibels.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to query.
    /// </param>
    ///
    /// <returns>
    /// The bus decibel volume offset.
    /// </returns>
    ///
    /// <remarks>
    /// This value is stored on the bus itself. It does not include the volume of
    /// any other bus on the routing path.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusVolumeDb(int, float)" />
    /// <seealso cref="GetBusVolumeLinear(int)" />
    public static float GetBusVolumeDb(int busIdx)
    {
        lock (BackendLock)
        {
            return RequireBus(busIdx).VolumeDb;
        }
    }

    /// <summary>
    /// Sets a bus volume as linear gain.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to change.
    /// </param>
    /// <param name="volumeLinear">
    /// A finite non-negative linear gain.
    /// </param>
    ///
    /// <remarks>
    /// A value of <c>1</c> maps to <c>0 dB</c>. A value of <c>0</c> maps to the
    /// preview quiet floor used for deterministic runtime tests.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="volumeLinear" /> is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus
    /// or when <paramref name="volumeLinear" /> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetBusVolumeLinear(int)" />
    /// <seealso cref="SetBusVolumeDb(int, float)" />
    public static void SetBusVolumeLinear(int busIdx, float volumeLinear)
    {
        if (!float.IsFinite(volumeLinear))
        {
            throw new ArgumentException("Audio bus linear volume must be finite.", nameof(volumeLinear));
        }

        if (volumeLinear < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(volumeLinear), volumeLinear, "Audio bus linear volume must be non-negative.");
        }

        SetBusVolumeDb(busIdx, LinearToDb(volumeLinear));
    }

    /// <summary>
    /// Gets a bus volume as linear gain.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to query.
    /// </param>
    ///
    /// <returns>
    /// The bus volume as linear gain.
    /// </returns>
    ///
    /// <remarks>
    /// The returned value is converted from the bus decibel volume only. It does
    /// not include any routed parent bus.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusVolumeLinear(int, float)" />
    /// <seealso cref="GetBusVolumeDb(int)" />
    public static float GetBusVolumeLinear(int busIdx)
    {
        lock (BackendLock)
        {
            return DbToLinear(RequireBus(busIdx).VolumeDb);
        }
    }

    /// <summary>
    /// Enables or disables mute for a bus.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to change.
    /// </param>
    /// <param name="enable">
    /// <c>true</c> to mute the bus; otherwise <c>false</c>.
    /// </param>
    ///
    /// <remarks>
    /// A muted bus silences voices whose routing path contains that bus. The
    /// change applies to voices started after the state change.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsBusMute(int)" />
    /// <seealso cref="SetBusSolo(int, bool)" />
    public static void SetBusMute(int busIdx, bool enable)
    {
        lock (BackendLock)
        {
            RequireBus(busIdx).Mute = enable;
        }
    }

    /// <summary>
    /// Checks whether a bus is muted.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to query.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the bus is muted; otherwise <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// The result describes the bus flag only. It does not inspect parent buses
    /// on the routing path.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusMute(int, bool)" />
    public static bool IsBusMute(int busIdx)
    {
        lock (BackendLock)
        {
            return RequireBus(busIdx).Mute;
        }
    }

    /// <summary>
    /// Enables or disables solo for a bus.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to change.
    /// </param>
    /// <param name="enable">
    /// <c>true</c> to solo the bus; otherwise <c>false</c>.
    /// </param>
    ///
    /// <remarks>
    /// When any bus is solo, voices are audible only if their routing path
    /// contains at least one solo bus. The change applies to voices started
    /// after the state change.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsBusSolo(int)" />
    /// <seealso cref="SetBusMute(int, bool)" />
    public static void SetBusSolo(int busIdx, bool enable)
    {
        lock (BackendLock)
        {
            RequireBus(busIdx).Solo = enable;
        }
    }

    /// <summary>
    /// Checks whether a bus is soloed.
    /// </summary>
    ///
    /// <param name="busIdx">
    /// The bus index to query.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the bus is soloed; otherwise <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// The result describes the bus flag only. It does not inspect other buses.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="busIdx" /> does not identify an existing bus.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetBusSolo(int, bool)" />
    public static bool IsBusSolo(int busIdx)
    {
        lock (BackendLock)
        {
            return RequireBus(busIdx).Solo;
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
    /// This method is available since Electron2D 0.1-preview.
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
    /// This method is available since Electron2D 0.1-preview.
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
            var resolvedPlayback = ResolvePlayback(playback);
            var backendHandle = backend.Play(stream, resolvedPlayback);
            if (!backendHandle.IsValid())
            {
                throw new InvalidOperationException("Audio backend returned an invalid voice handle.");
            }

            var voice = new AudioVoiceHandle(++nextVoiceId);
            Voices.Add(voice, new VoiceState(stream, resolvedPlayback, backendHandle));
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

    internal static AudioVoicePlayback GetVoicePlayback(AudioVoiceHandle voice)
    {
        lock (BackendLock)
        {
            return RequireVoice(voice).Playback;
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
            ResetBusGraph();
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

        if (!float.IsFinite(playback.StartPosition) || playback.StartPosition < 0f)
        {
            throw new ArgumentException("Audio voice start position must be finite and non-negative.", nameof(playback));
        }

        if (!float.IsFinite(playback.Pan) || playback.Pan < -1f || playback.Pan > 1f)
        {
            throw new ArgumentException("Audio voice pan must be finite and between -1 and 1.", nameof(playback));
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

    private static void ResetBusGraph()
    {
        Buses.Clear();
        Buses.Add(AudioBus.CreateMaster());
    }

    private static AudioVoicePlayback ResolvePlayback(AudioVoicePlayback playback)
    {
        var busIndex = FindBusIndex(playback.Bus.IsEmpty() ? MasterBusName : playback.Bus.ToString());
        if (busIndex < 0)
        {
            busIndex = 0;
        }

        var path = BuildBusPath(busIndex);
        var volumeDb = playback.VolumeDb;
        foreach (var bus in path)
        {
            volumeDb += bus.VolumeDb;
        }

        var anySolo = Buses.Any(bus => bus.Solo);
        if (path.Any(bus => bus.Mute) || (anySolo && !path.Any(bus => bus.Solo)))
        {
            volumeDb = QuietVolumeDb;
        }

        return new AudioVoicePlayback(
            VolumeDb: volumeDb,
            PitchScale: playback.PitchScale,
            Loop: playback.Loop,
            StartPosition: playback.StartPosition,
            Pan: playback.Pan,
            Bus: new StringName(Buses[busIndex].Name));
    }

    private static IReadOnlyList<AudioBus> BuildBusPath(int busIndex)
    {
        var path = new List<AudioBus>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var currentIndex = busIndex;

        while (true)
        {
            var bus = Buses[currentIndex];
            if (!visited.Add(bus.Name))
            {
                throw new InvalidOperationException("Audio bus routing cycle detected.");
            }

            path.Add(bus);
            if (currentIndex == 0)
            {
                return path;
            }

            var sendIndex = FindBusIndex(bus.Send.IsEmpty() ? MasterBusName : bus.Send.ToString());
            currentIndex = sendIndex >= 0 && sendIndex < currentIndex ? sendIndex : 0;
        }
    }

    private static AudioBus RequireBus(int index)
    {
        RequireBusIndex(index, nameof(index));
        return Buses[index];
    }

    private static void RequireBusIndex(int index, string parameterName)
    {
        if (index < 0 || index >= Buses.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName, index, "Audio bus index is out of range.");
        }
    }

    private static void RequireUserBusIndex(int index, string parameterName)
    {
        RequireBusIndex(index, parameterName);
        if (index == 0)
        {
            throw new InvalidOperationException("Master bus cannot be changed by this operation.");
        }
    }

    private static int FindBusIndex(string name)
    {
        for (var index = 0; index < Buses.Count; index++)
        {
            if (string.Equals(Buses[index].Name, name, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormalizeBusName(string name, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(name);
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Audio bus name cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string NextDefaultBusName()
    {
        var number = 1;
        while (true)
        {
            var name = "Bus " + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (FindBusIndex(name) < 0)
            {
                return name;
            }

            number++;
        }
    }

    private static void NormalizeInvalidBusSends()
    {
        Buses[0].Send = default;
        for (var index = 1; index < Buses.Count; index++)
        {
            var targetName = Buses[index].Send.IsEmpty() ? MasterBusName : Buses[index].Send.ToString();
            var targetIndex = FindBusIndex(targetName);
            if (targetIndex < 0 || targetIndex >= index)
            {
                Buses[index].Send = new StringName(MasterBusName);
            }
        }
    }

    private static bool StringNameEquals(StringName name, string value)
    {
        return string.Equals(name.ToString(), value, StringComparison.Ordinal);
    }

    private static float DbToLinear(float db)
    {
        return db <= QuietVolumeDb ? 0f : MathF.Pow(10f, db / 20f);
    }

    private static float LinearToDb(float linear)
    {
        return linear <= 0f ? QuietVolumeDb : 20f * MathF.Log10(linear);
    }

    private sealed class AudioBus
    {
        private AudioBus(string name, StringName send)
        {
            Name = name;
            Send = send;
        }

        public string Name { get; set; }

        public StringName Send { get; set; }

        public float VolumeDb { get; set; }

        public bool Mute { get; set; }

        public bool Solo { get; set; }

        public static AudioBus CreateMaster()
        {
            return new AudioBus(MasterBusName, default);
        }

        public static AudioBus CreateUser(string name, StringName send)
        {
            return new AudioBus(name, send);
        }
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
