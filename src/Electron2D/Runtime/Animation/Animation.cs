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

/// <summary>
/// Stores keyframed value and method-call tracks for <see cref="AnimationPlayer"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// An <see cref="Animation"/> is a passive resource. It stores ordered tracks,
/// key times, key values and method-call entries, but it does not advance time
/// or modify scene nodes by itself.
/// </para>
/// <para>
/// Electron2D 0.1.0 Preview supports value tracks and method tracks only.
/// Editor-only tracks, audio tracks, section playback and blend-tree data are
/// outside this runtime baseline.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate an instance from one thread, normally
/// during loading or on the main scene thread. Read-only access is safe only
/// while no thread is mutating the resource.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="AnimationLibrary"/>
/// <seealso cref="AnimationPlayer"/>
public sealed class Animation : Resource
{
    private readonly List<AnimationTrack> tracks = new();
    private double length;

    /// <summary>
    /// Creates an empty animation resource.
    /// </summary>
    ///
    /// <remarks>
    /// New animations have a length of <c>0</c>, no tracks and
    /// <see cref="LoopModeEnum.None"/> loop behavior.
    /// </remarks>
    ///
    /// <threadsafety>
    /// Construction is safe on any thread when the instance is not shared until
    /// construction completes.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="Animation" />
    ///
    public Animation()
    {
    }

    /// <summary>
    /// Describes the supported animation track kinds.
    /// </summary>
    ///
    /// <remarks>
    /// The 0.1.0 Preview runtime intentionally keeps only the track kinds that
    /// can drive 2D scene state without editor-only systems.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum TrackTypeEnum
    {
        /// <summary>
        /// A track that applies keyframed <see cref="Variant"/> values to a
        /// property path.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept TrackTypeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="TrackTypeEnum" />
        ///
        Value = 0,

        /// <summary>
        /// A track that calls methods on a target node when playback crosses a
        /// key time.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept TrackTypeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="TrackTypeEnum" />
        ///
        Method = 1
    }

    /// <summary>
    /// Describes how value tracks produce values between neighboring keys.
    /// </summary>
    ///
    /// <remarks>
    /// Method tracks ignore this value because their keys represent discrete
    /// calls.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum InterpolationTypeEnum
    {
        /// <summary>
        /// Reuses the latest key at or before the requested time.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept InterpolationTypeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="InterpolationTypeEnum" />
        ///
        Nearest = 0,

        /// <summary>
        /// Interpolates supported numeric and 2D value types between keys.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept InterpolationTypeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="InterpolationTypeEnum" />
        ///
        Linear = 1
    }

    /// <summary>
    /// Describes how playback behaves when it reaches the end of the resource.
    /// </summary>
    ///
    /// <remarks>
    /// The loop mode is consumed by <see cref="AnimationPlayer"/>; the resource
    /// itself only stores the selected value.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum LoopModeEnum
    {
        /// <summary>
        /// Playback stops when it reaches <see cref="Length"/>.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept LoopModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="LoopModeEnum" />
        ///
        None = 0,

        /// <summary>
        /// Playback wraps from <see cref="Length"/> back to the beginning.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept LoopModeEnum.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="LoopModeEnum" />
        ///
        Linear = 1
    }

    /// <summary>
    /// Gets or sets the animation length in seconds.
    /// </summary>
    ///
    /// <value>
    /// A finite value greater than or equal to <c>0</c>.
    /// </value>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is not finite or is less than <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public double Length
    {
        get => length;
        set
        {
            ValidateNonNegativeFinite(value, nameof(value), "Animation length");
            length = value;
        }
    }

    /// <summary>
    /// Gets or sets the loop mode used by <see cref="AnimationPlayer"/>.
    /// </summary>
    ///
    /// <value>
    /// The loop behavior for playback of this resource.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public LoopModeEnum LoopMode { get; set; } = LoopModeEnum.None;

    /// <summary>
    /// Adds a new track.
    /// </summary>
    ///
    /// <param name="type">The kind of track to add.</param>
    /// <param name="atPosition">
    /// The insertion index, or <c>-1</c> to append the track.
    /// </param>
    ///
    /// <returns>The zero-based index of the inserted track.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="type"/> is outside the supported enum range
    /// or <paramref name="atPosition"/> is outside the insertion range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RemoveTrack"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public int AddTrack(TrackTypeEnum type, int atPosition = -1)
    {
        ValidateTrackType(type, nameof(type));
        var insertIndex = atPosition == -1 ? tracks.Count : atPosition;
        if (insertIndex < 0 || insertIndex > tracks.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(atPosition), atPosition, "Track insertion index is outside the valid range.");
        }

        tracks.Insert(insertIndex, new AnimationTrack(type));
        return insertIndex;
    }

    /// <summary>
    /// Removes a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddTrack"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void RemoveTrack(int trackIdx)
    {
        tracks.RemoveAt(ValidateTrackIndex(trackIdx));
    }

    /// <summary>
    /// Gets the number of tracks stored in this resource.
    /// </summary>
    ///
    /// <returns>The track count.</returns>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public int GetTrackCount()
    {
        return tracks.Count;
    }

    /// <summary>
    /// Gets the kind of a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <returns>The track type.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public TrackTypeEnum TrackGetType(int trackIdx)
    {
        return GetTrack(trackIdx).Type;
    }

    /// <summary>
    /// Assigns a path to a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    /// <param name="path">
    /// The target path. Value tracks use node path plus property subnames;
    /// method tracks use the node path.
    /// </param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackGetPath"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void TrackSetPath(int trackIdx, NodePath path)
    {
        GetTrack(trackIdx).Path = path;
    }

    /// <summary>
    /// Gets the path assigned to a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <returns>The track path.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackSetPath"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public NodePath TrackGetPath(int trackIdx)
    {
        return GetTrack(trackIdx).Path;
    }

    /// <summary>
    /// Enables or disables a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    /// <param name="enabled"><c>true</c> to enable the track; otherwise, <c>false</c>.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackIsEnabled"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void TrackSetEnabled(int trackIdx, bool enabled)
    {
        GetTrack(trackIdx).Enabled = enabled;
    }

    /// <summary>
    /// Gets whether a track is enabled.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <returns><c>true</c> when the track is enabled; otherwise, <c>false</c>.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackSetEnabled"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public bool TrackIsEnabled(int trackIdx)
    {
        return GetTrack(trackIdx).Enabled;
    }

    /// <summary>
    /// Sets the interpolation mode for a value track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    /// <param name="interpolation">The interpolation mode to assign.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range or
    /// <paramref name="interpolation"/> is outside the supported enum range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a value
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackGetInterpolationType"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void TrackSetInterpolationType(int trackIdx, InterpolationTypeEnum interpolation)
    {
        ValidateInterpolationType(interpolation, nameof(interpolation));
        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Value, nameof(trackIdx));
        track.Interpolation = interpolation;
    }

    /// <summary>
    /// Gets the interpolation mode for a value track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <returns>The interpolation mode.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a value
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackSetInterpolationType"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public InterpolationTypeEnum TrackGetInterpolationType(int trackIdx)
    {
        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Value, nameof(trackIdx));
        return track.Interpolation;
    }

    /// <summary>
    /// Inserts or replaces a value key.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based value track index.</param>
    /// <param name="time">The finite key time in seconds. It must be at least <c>0</c>.</param>
    /// <param name="value">The value stored at the key.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range or
    /// <paramref name="time"/> is not finite or less than <c>0</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a value
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ValueTrackInterpolate"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void TrackInsertKey(int trackIdx, double time, Variant value)
    {
        ValidateNonNegativeFinite(time, nameof(time), "Key time");
        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Value, nameof(trackIdx));
        InsertValueKey(track.ValueKeys, new ValueKey(time, value));
    }

    /// <summary>
    /// Inserts or replaces a method-call key.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based method track index.</param>
    /// <param name="time">The finite key time in seconds. It must be at least <c>0</c>.</param>
    /// <param name="method">The method name to call on the track target.</param>
    /// <param name="arguments">The optional method arguments stored as Variant values.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="method"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range or
    /// <paramref name="time"/> is not finite or less than <c>0</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a method
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource from one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public void MethodTrackInsertKey(int trackIdx, double time, StringName method, Variant[]? arguments = null)
    {
        ValidateNonNegativeFinite(time, nameof(time), "Key time");
        if (method.IsEmpty())
        {
            throw new ArgumentException("Method name must not be empty.", nameof(method));
        }

        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Method, nameof(trackIdx));
        InsertMethodKey(track.MethodKeys, new MethodKey(time, method, arguments ?? Array.Empty<Variant>()));
    }

    /// <summary>
    /// Gets the number of keys in a track.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    ///
    /// <returns>The number of keys in the track.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public int TrackGetKeyCount(int trackIdx)
    {
        var track = GetTrack(trackIdx);
        return track.Type == TrackTypeEnum.Value ? track.ValueKeys.Count : track.MethodKeys.Count;
    }

    /// <summary>
    /// Gets a key time.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based track index.</param>
    /// <param name="keyIdx">The zero-based key index.</param>
    ///
    /// <returns>The key time in seconds.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> or <paramref name="keyIdx"/> is
    /// outside the valid range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Animation" />
    ///
    public double TrackGetKeyTime(int trackIdx, int keyIdx)
    {
        var track = GetTrack(trackIdx);
        return track.Type == TrackTypeEnum.Value
            ? track.ValueKeys[ValidateKeyIndex(keyIdx, track.ValueKeys.Count)].Time
            : track.MethodKeys[ValidateKeyIndex(keyIdx, track.MethodKeys.Count)].Time;
    }

    /// <summary>
    /// Gets a value key's Variant value.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based value track index.</param>
    /// <param name="keyIdx">The zero-based key index.</param>
    ///
    /// <returns>The stored key value.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> or <paramref name="keyIdx"/> is
    /// outside the valid range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a value
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackInsertKey"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Variant TrackGetKeyValue(int trackIdx, int keyIdx)
    {
        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Value, nameof(trackIdx));
        return track.ValueKeys[ValidateKeyIndex(keyIdx, track.ValueKeys.Count)].Value;
    }

    /// <summary>
    /// Evaluates a value track at a specific time.
    /// </summary>
    ///
    /// <param name="trackIdx">The zero-based value track index.</param>
    /// <param name="time">The finite time in seconds.</param>
    ///
    /// <returns>
    /// The nearest or interpolated value. A track with no keys returns a nil
    /// <see cref="Variant"/>.
    /// </returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trackIdx"/> is outside the track range or
    /// <paramref name="time"/> is not finite.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="trackIdx"/> does not reference a value
    /// track.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe when no thread is mutating the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TrackSetInterpolationType"/>
    /// <seealso cref="TrackInsertKey"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Variant ValueTrackInterpolate(int trackIdx, double time)
    {
        if (!double.IsFinite(time))
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Interpolation time must be finite.");
        }

        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Value, nameof(trackIdx));
        if (track.ValueKeys.Count == 0)
        {
            return default;
        }

        var keys = track.ValueKeys;
        if (time <= keys[0].Time)
        {
            return keys[0].Value;
        }

        if (time >= keys[^1].Time)
        {
            return keys[^1].Value;
        }

        var nextIndex = FindFirstKeyAfter(keys, time);
        var previous = keys[nextIndex - 1];
        var next = keys[nextIndex];
        if (track.Interpolation == InterpolationTypeEnum.Nearest)
        {
            return previous.Value;
        }

        var span = next.Time - previous.Time;
        if (span <= 0d)
        {
            return previous.Value;
        }

        var weight = (float)((time - previous.Time) / span);
        return InterpolateValues(previous.Value, next.Value, weight);
    }

    internal IReadOnlyList<AnimationMethodKey> GetMethodTrackKeys(int trackIdx)
    {
        var track = GetTrack(trackIdx);
        EnsureTrackType(track, TrackTypeEnum.Method, nameof(trackIdx));
        return track.MethodKeys;
    }

    private static Variant InterpolateValues(Variant from, Variant to, float weight)
    {
        if (from.VariantType != to.VariantType)
        {
            return from;
        }

        return from.VariantType switch
        {
            Variant.Type.Int => Variant.From((long)Math.Round(Mathf.Lerp((float)from.AsInt64(), (float)to.AsInt64(), weight))),
            Variant.Type.Float => Variant.From((double)Mathf.Lerp((float)from.AsDouble(), (float)to.AsDouble(), weight)),
            Variant.Type.Vector2 => Variant.From(from.AsVector2().Lerp(to.AsVector2(), weight)),
            Variant.Type.Color => Variant.From(from.AsColor().Lerp(to.AsColor(), weight)),
            _ => from
        };
    }

    private static void InsertValueKey(List<ValueKey> keys, ValueKey key)
    {
        var existingIndex = keys.FindIndex(item => item.Time == key.Time);
        if (existingIndex >= 0)
        {
            keys[existingIndex] = key;
            return;
        }

        var insertIndex = keys.FindIndex(item => item.Time > key.Time);
        if (insertIndex < 0)
        {
            keys.Add(key);
            return;
        }

        keys.Insert(insertIndex, key);
    }

    private static void InsertMethodKey(List<AnimationMethodKey> keys, MethodKey key)
    {
        var publicKey = new AnimationMethodKey(key.Time, key.Method, key.Arguments);
        var existingIndex = keys.FindIndex(item => item.Time == key.Time);
        if (existingIndex >= 0)
        {
            keys[existingIndex] = publicKey;
            return;
        }

        var insertIndex = keys.FindIndex(item => item.Time > key.Time);
        if (insertIndex < 0)
        {
            keys.Add(publicKey);
            return;
        }

        keys.Insert(insertIndex, publicKey);
    }

    private static int FindFirstKeyAfter(List<ValueKey> keys, double time)
    {
        for (var index = 0; index < keys.Count; index++)
        {
            if (keys[index].Time > time)
            {
                return index;
            }
        }

        return keys.Count - 1;
    }

    private static void ValidateTrackType(TrackTypeEnum type, string parameterName)
    {
        if (type is not TrackTypeEnum.Value and not TrackTypeEnum.Method)
        {
            throw new ArgumentOutOfRangeException(parameterName, type, "Unsupported animation track type.");
        }
    }

    private static void ValidateInterpolationType(InterpolationTypeEnum type, string parameterName)
    {
        if (type is not InterpolationTypeEnum.Nearest and not InterpolationTypeEnum.Linear)
        {
            throw new ArgumentOutOfRangeException(parameterName, type, "Unsupported animation interpolation type.");
        }
    }

    private static void ValidateNonNegativeFinite(double value, string parameterName, string label)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{label} must be finite and greater than or equal to zero.");
        }
    }

    private static int ValidateKeyIndex(int keyIdx, int keyCount)
    {
        if (keyIdx < 0 || keyIdx >= keyCount)
        {
            throw new ArgumentOutOfRangeException(nameof(keyIdx), keyIdx, "Key index is outside the track key range.");
        }

        return keyIdx;
    }

    private int ValidateTrackIndex(int trackIdx)
    {
        if (trackIdx < 0 || trackIdx >= tracks.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(trackIdx), trackIdx, "Track index is outside the animation track range.");
        }

        return trackIdx;
    }

    private AnimationTrack GetTrack(int trackIdx)
    {
        return tracks[ValidateTrackIndex(trackIdx)];
    }

    private static void EnsureTrackType(AnimationTrack track, TrackTypeEnum expected, string parameterName)
    {
        if (track.Type != expected)
        {
            throw new InvalidOperationException($"Track '{parameterName}' must reference a {expected} track.");
        }
    }

    private sealed class AnimationTrack
    {
        public AnimationTrack(TrackTypeEnum type)
        {
            Type = type;
        }

        public TrackTypeEnum Type { get; }

        public NodePath Path { get; set; }

        public bool Enabled { get; set; } = true;

        public InterpolationTypeEnum Interpolation { get; set; } = InterpolationTypeEnum.Linear;

        public List<ValueKey> ValueKeys { get; } = new();

        public List<AnimationMethodKey> MethodKeys { get; } = new();
    }

    private readonly record struct ValueKey(double Time, Variant Value);

    private readonly record struct MethodKey(double Time, StringName Method, Variant[] Arguments);
}

internal readonly record struct AnimationMethodKey(double Time, StringName Method, Variant[] Arguments);
