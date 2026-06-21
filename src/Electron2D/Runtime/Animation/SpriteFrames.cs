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
/// Stores named texture-frame animations for <see cref="AnimatedSprite2D"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// A <see cref="SpriteFrames"/> resource owns animation names, frame textures,
/// relative frame durations, playback speeds and loop modes. The resource does
/// not run playback by itself; <see cref="AnimatedSprite2D"/> consumes it during
/// scene processing.
/// </para>
/// <para>
/// A new instance always contains an empty animation named <c>default</c>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Mutate it during loading or on the main
/// scene thread. Read-only access is safe from another thread only when no
/// thread is mutating the resource.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="AnimatedSprite2D"/>
/// <seealso cref="Texture2D"/>
public sealed class SpriteFrames : Resource
{
    private const string DefaultAnimation = "default";
    private const float DefaultAnimationSpeed = 5f;

    private readonly Dictionary<StringName, AnimationData> animations = new();

    /// <summary>
    /// Creates a new sprite-frame library with an empty <c>default</c>
    /// animation.
    /// </summary>
    ///
    /// <remarks>
    /// The default animation uses linear looping and a speed of five frames per
    /// second.
    /// </remarks>
    ///
    /// <threadsafety>
    /// Construction is safe on any thread when the instance is not shared until
    /// after construction completes.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="SpriteFrames" />
    ///
    public SpriteFrames()
    {
        animations.Add(DefaultAnimation, new AnimationData());
    }

    /// <summary>
    /// Describes how an animation loops at its boundaries.
    /// </summary>
    ///
    /// <remarks>
    /// The values are used by <see cref="AnimatedSprite2D"/> when playback
    /// reaches the first or last frame of an animation.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum LoopModeEnum
    {
        /// <summary>
        /// The animation stops when playback reaches the first or last frame.
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
        /// The animation wraps to the opposite end and continues in the same
        /// direction.
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
        Linear = 1,

        /// <summary>
        /// The animation changes direction each time playback reaches the
        /// first or last frame.
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
        Pingpong = 2
    }

    /// <summary>
    /// Adds an empty animation.
    /// </summary>
    ///
    /// <param name="animation">The non-empty animation name to add.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an animation with the same name already exists.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasAnimation"/>
    /// <seealso cref="RemoveAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void AddAnimation(StringName animation)
    {
        var name = ValidateAnimationName(animation, nameof(animation));
        if (animations.ContainsKey(name))
        {
            throw new InvalidOperationException($"Animation '{name}' already exists.");
        }

        animations.Add(name, new AnimationData());
    }

    /// <summary>
    /// Adds a frame to an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation that receives the frame.</param>
    /// <param name="texture">The texture to display for the frame.</param>
    /// <param name="duration">The positive relative frame duration.</param>
    /// <param name="atPosition">
    /// The insertion index, or <c>-1</c> to append the frame.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// The absolute frame duration in seconds is calculated by
    /// <see cref="AnimatedSprite2D"/> from the relative duration, animation
    /// speed and playing speed.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="texture"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="duration"/> is not finite or not greater
    /// than zero, or when <paramref name="atPosition"/> is outside the valid
    /// insertion range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetFrame"/>
    /// <seealso cref="GetFrameTexture"/>
    public void AddFrame(StringName animation, Texture2D texture, float duration = 1f, int atPosition = -1)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ValidatePositiveFinite(duration, nameof(duration), "Frame duration");
        var data = GetExistingAnimation(animation, nameof(animation));
        var insertIndex = atPosition == -1 ? data.Frames.Count : atPosition;
        if (insertIndex < 0 || insertIndex > data.Frames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(atPosition), atPosition, "Frame insertion index is outside the animation frame range.");
        }

        data.Frames.Insert(insertIndex, new FrameData(texture, duration));
    }

    /// <summary>
    /// Removes all frames from one animation.
    /// </summary>
    ///
    /// <param name="animation">The animation to clear.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ClearAll"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void Clear(StringName animation)
    {
        GetExistingAnimation(animation, nameof(animation)).Frames.Clear();
    }

    /// <summary>
    /// Removes all animations and recreates an empty <c>default</c> animation.
    /// </summary>
    ///
    /// <remarks>
    /// This keeps the resource in a valid state for nodes whose
    /// <see cref="AnimatedSprite2D.Animation"/> is still set to
    /// <c>default</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Clear"/>
    public void ClearAll()
    {
        animations.Clear();
        animations.Add(DefaultAnimation, new AnimationData());
    }

    /// <summary>
    /// Duplicates an animation into a new animation name.
    /// </summary>
    ///
    /// <param name="animationFrom">The existing animation to copy.</param>
    /// <param name="animationTo">The new animation name.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when either animation name is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animationFrom"/> does not exist or
    /// <paramref name="animationTo"/> already exists.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RenameAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void DuplicateAnimation(StringName animationFrom, StringName animationTo)
    {
        var source = GetExistingAnimation(animationFrom, nameof(animationFrom));
        var targetName = ValidateAnimationName(animationTo, nameof(animationTo));
        if (animations.ContainsKey(targetName))
        {
            throw new InvalidOperationException($"Animation '{targetName}' already exists.");
        }

        animations.Add(targetName, source.Clone());
    }

    /// <summary>
    /// Gets whether an animation uses linear looping.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    ///
    /// <returns>
    /// <c>true</c> when <see cref="GetAnimationLoopMode"/> returns
    /// <see cref="LoopModeEnum.Linear"/>; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetAnimationLoop"/>
    /// <seealso cref="GetAnimationLoopMode"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public bool GetAnimationLoop(StringName animation)
    {
        return GetAnimationLoopMode(animation) == LoopModeEnum.Linear;
    }

    /// <summary>
    /// Gets the loop mode for an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    ///
    /// <returns>The loop mode assigned to the animation.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetAnimationLoopMode"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public LoopModeEnum GetAnimationLoopMode(StringName animation)
    {
        return GetExistingAnimation(animation, nameof(animation)).LoopMode;
    }

    /// <summary>
    /// Gets animation names in deterministic alphabetical order.
    /// </summary>
    ///
    /// <returns>An array containing all animation names.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public StringName[] GetAnimationNames()
    {
        return animations.Keys
            .OrderBy(name => name.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Gets the speed of an animation in frames per second.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    ///
    /// <returns>The animation speed in frames per second.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetAnimationSpeed"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public float GetAnimationSpeed(StringName animation)
    {
        return GetExistingAnimation(animation, nameof(animation)).Speed;
    }

    /// <summary>
    /// Gets the number of frames in an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    ///
    /// <returns>The frame count for the animation.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddFrame"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public int GetFrameCount(StringName animation)
    {
        return GetExistingAnimation(animation, nameof(animation)).Frames.Count;
    }

    /// <summary>
    /// Gets a frame's relative duration.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    /// <param name="index">The zero-based frame index.</param>
    ///
    /// <returns>The positive relative frame duration.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the animation frame
    /// range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetFrame"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public float GetFrameDuration(StringName animation, int index)
    {
        return GetFrame(animation, index, nameof(index)).Duration;
    }

    /// <summary>
    /// Gets a frame's texture.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    /// <param name="index">The zero-based frame index.</param>
    ///
    /// <returns>The texture assigned to the frame.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the animation frame
    /// range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddFrame"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Texture2D GetFrameTexture(StringName animation, int index)
    {
        return GetFrame(animation, index, nameof(index)).Texture;
    }

    /// <summary>
    /// Checks whether an animation exists.
    /// </summary>
    ///
    /// <param name="animation">The animation name to check.</param>
    ///
    /// <returns>
    /// <c>true</c> when the resource contains <paramref name="animation"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when no thread is mutating
    /// the resource.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public bool HasAnimation(StringName animation)
    {
        return !animation.IsEmpty() && animations.ContainsKey(animation);
    }

    /// <summary>
    /// Removes an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation to remove.</param>
    ///
    /// <remarks>
    /// Removing the final animation recreates an empty <c>default</c>
    /// animation.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddAnimation"/>
    public void RemoveAnimation(StringName animation)
    {
        var name = ValidateAnimationName(animation, nameof(animation));
        if (!animations.Remove(name))
        {
            throw new InvalidOperationException($"Animation '{name}' does not exist.");
        }

        EnsureDefaultAnimationWhenEmpty();
    }

    /// <summary>
    /// Removes one frame from an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation that owns the frame.</param>
    /// <param name="index">The zero-based frame index to remove.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the animation frame
    /// range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddFrame"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void RemoveFrame(StringName animation, int index)
    {
        var data = GetExistingAnimation(animation, nameof(animation));
        ValidateFrameIndex(index, data.Frames.Count, nameof(index));
        data.Frames.RemoveAt(index);
    }

    /// <summary>
    /// Renames an animation.
    /// </summary>
    ///
    /// <param name="animation">The existing animation name.</param>
    /// <param name="newName">The new animation name.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when either animation name is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist or
    /// <paramref name="newName"/> already exists.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="DuplicateAnimation"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void RenameAnimation(StringName animation, StringName newName)
    {
        var sourceName = ValidateAnimationName(animation, nameof(animation));
        var targetName = ValidateAnimationName(newName, nameof(newName));
        if (!animations.TryGetValue(sourceName, out var data))
        {
            throw new InvalidOperationException($"Animation '{sourceName}' does not exist.");
        }

        if (animations.ContainsKey(targetName))
        {
            throw new InvalidOperationException($"Animation '{targetName}' already exists.");
        }

        animations.Remove(sourceName);
        animations.Add(targetName, data);
    }

    /// <summary>
    /// Sets whether an animation uses linear looping.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    /// <param name="loop">
    /// <c>true</c> to set <see cref="LoopModeEnum.Linear"/>; <c>false</c> to
    /// set <see cref="LoopModeEnum.None"/>.
    /// </param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAnimationLoop"/>
    /// <seealso cref="SetAnimationLoopMode"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void SetAnimationLoop(StringName animation, bool loop)
    {
        SetAnimationLoopMode(animation, loop ? LoopModeEnum.Linear : LoopModeEnum.None);
    }

    /// <summary>
    /// Sets the loop mode for an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    /// <param name="loopMode">The loop mode to assign.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAnimationLoopMode"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void SetAnimationLoopMode(StringName animation, LoopModeEnum loopMode)
    {
        GetExistingAnimation(animation, nameof(animation)).LoopMode = loopMode;
    }

    /// <summary>
    /// Sets the speed of an animation in frames per second.
    /// </summary>
    ///
    /// <param name="animation">The animation name.</param>
    /// <param name="fps">The positive finite speed in frames per second.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fps"/> is not finite or not greater than
    /// zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetAnimationSpeed"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void SetAnimationSpeed(StringName animation, float fps)
    {
        ValidatePositiveFinite(fps, nameof(fps), "Animation speed");
        GetExistingAnimation(animation, nameof(animation)).Speed = fps;
    }

    /// <summary>
    /// Replaces one frame in an animation.
    /// </summary>
    ///
    /// <param name="animation">The animation that owns the frame.</param>
    /// <param name="index">The zero-based frame index to replace.</param>
    /// <param name="texture">The replacement texture.</param>
    /// <param name="duration">The positive relative frame duration.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="animation"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="texture"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the animation frame
    /// range, or when <paramref name="duration"/> is not finite or not greater
    /// than zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="animation"/> does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the resource on one thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetFrameTexture"/>
    /// <seealso cref="GetFrameDuration"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void SetFrame(StringName animation, int index, Texture2D texture, float duration = 1f)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ValidatePositiveFinite(duration, nameof(duration), "Frame duration");
        var data = GetExistingAnimation(animation, nameof(animation));
        ValidateFrameIndex(index, data.Frames.Count, nameof(index));
        data.Frames[index] = new FrameData(texture, duration);
    }

    internal bool TryGetFrame(StringName animation, int index, out Texture2D? texture, out float duration)
    {
        texture = null;
        duration = 1f;
        if (animation.IsEmpty() || !animations.TryGetValue(animation, out var data) || index < 0 || index >= data.Frames.Count)
        {
            return false;
        }

        var frame = data.Frames[index];
        texture = frame.Texture;
        duration = frame.Duration;
        return true;
    }

    private static StringName ValidateAnimationName(StringName animation, string parameterName)
    {
        if (animation.IsEmpty())
        {
            throw new ArgumentException("Animation name must not be empty.", parameterName);
        }

        return animation;
    }

    private static void ValidatePositiveFinite(float value, string parameterName, string label)
    {
        if (!Mathf.IsFinite(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{label} must be finite and greater than zero.");
        }
    }

    private static void ValidateFrameIndex(int index, int frameCount, string parameterName)
    {
        if (index < 0 || index >= frameCount)
        {
            throw new ArgumentOutOfRangeException(parameterName, index, "Frame index is outside the animation frame range.");
        }
    }

    private AnimationData GetExistingAnimation(StringName animation, string parameterName)
    {
        var name = ValidateAnimationName(animation, parameterName);
        if (!animations.TryGetValue(name, out var data))
        {
            throw new InvalidOperationException($"Animation '{name}' does not exist.");
        }

        return data;
    }

    private FrameData GetFrame(StringName animation, int index, string parameterName)
    {
        var data = GetExistingAnimation(animation, nameof(animation));
        ValidateFrameIndex(index, data.Frames.Count, parameterName);
        return data.Frames[index];
    }

    private void EnsureDefaultAnimationWhenEmpty()
    {
        if (animations.Count == 0)
        {
            animations.Add(DefaultAnimation, new AnimationData());
        }
    }

    private sealed class AnimationData
    {
        public List<FrameData> Frames { get; } = new();

        public float Speed { get; set; } = DefaultAnimationSpeed;

        public LoopModeEnum LoopMode { get; set; } = LoopModeEnum.Linear;

        public AnimationData Clone()
        {
            var clone = new AnimationData
            {
                Speed = Speed,
                LoopMode = LoopMode
            };
            clone.Frames.AddRange(Frames);
            return clone;
        }
    }

    private readonly record struct FrameData(Texture2D Texture, float Duration);
}
