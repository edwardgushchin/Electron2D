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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Electron2D;

/// <summary>
/// Plays <see cref="Animation"/> resources against nodes in the scene tree.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="AnimationPlayer"/> mounts one or more
/// <see cref="AnimationLibrary"/> resources, resolves animation names, advances
/// playback during <see cref="SceneTree"/> processing and applies supported
/// tracks to target nodes.
/// </para>
/// <para>
/// Electron2D 0.1-preview supports value tracks, method call tracks, FIFO
/// queue playback and the <c>animation_finished</c> signal. Blend trees, editor
/// timeline tooling and audio or 3D-specific tracks are outside this baseline.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate animation players on the
/// main scene thread that owns the node.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Animation"/>
/// <seealso cref="AnimationLibrary"/>
public sealed class AnimationPlayer : Node, ISceneTreeLifecycleHandler
{
    private const string DefaultRootNode = "..";
    private const string AnimationFinishedSignal = "animation_finished";

    private readonly Dictionary<StringName, AnimationLibrary> libraries = new();
    private readonly Queue<StringName> animationQueue = new();
    private float speedScale = 1f;
    private float customSpeed = 1f;
    private bool playing;

    /// <summary>
    /// Creates an animation player with the default root node path.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers the <c>animation_finished</c> signal and sets
    /// <see cref="RootNode"/> to <c>..</c>, so track paths resolve relative to
    /// the player's parent by default.
    /// </remarks>
    ///
    /// <threadsafety>
    /// Construction is safe on any thread when the instance is not shared until
    /// construction completes.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="AnimationPlayer" />
    ///
    public AnimationPlayer()
    {
        RootNode = new NodePath(DefaultRootNode);
        AddUserSignal(AnimationFinishedSignal);
    }

    /// <summary>
    /// Gets or sets the node path used as the base for animation track paths.
    /// </summary>
    ///
    /// <value>
    /// A path resolved relative to this player. The default is <c>..</c>.
    /// </value>
    ///
    /// <remarks>
    /// A value track path such as <c>Target:Position</c> first resolves
    /// <see cref="RootNode"/>, then resolves <c>Target</c> from that root.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="AnimationPlayer" />
    ///
    public NodePath RootNode { get; set; }

    /// <summary>
    /// Gets or sets the animation to play automatically when the node is ready.
    /// </summary>
    ///
    /// <value>
    /// The animation name. Use an empty value to disable autoplay.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public StringName Autoplay { get; set; }

    /// <summary>
    /// Gets or sets the animation selected for calls to <see cref="Play"/> with
    /// an empty name.
    /// </summary>
    ///
    /// <value>
    /// The assigned animation name, or an empty value when no animation is
    /// assigned.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public StringName AssignedAnimation { get; set; }

    /// <summary>
    /// Gets the animation currently controlled by playback.
    /// </summary>
    ///
    /// <value>
    /// The current animation name, or an empty value before playback starts.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is safe to read on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public StringName CurrentAnimation { get; private set; }

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    ///
    /// <value>
    /// The current position inside <see cref="CurrentAnimation"/>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is safe to read on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public double CurrentAnimationPosition { get; private set; }

    /// <summary>
    /// Gets the length of the current animation in seconds.
    /// </summary>
    ///
    /// <value>
    /// The resolved animation length, or <c>0</c> when no current animation is
    /// available.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is safe to read on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public double CurrentAnimationLength
    {
        get
        {
            return TryResolveAnimation(CurrentAnimation, out _, out _, out var animation)
                ? animation.Length
                : 0d;
        }
    }

    /// <summary>
    /// Gets or sets the global playback speed multiplier.
    /// </summary>
    ///
    /// <value>
    /// A finite speed multiplier. The default value is <c>1</c>.
    /// </value>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public float SpeedScale
    {
        get => speedScale;
        set
        {
            if (!Mathf.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Speed scale must be finite.");
            }

            speedScale = value;
        }
    }

    /// <summary>
    /// Adds an animation library.
    /// </summary>
    ///
    /// <param name="name">
    /// The library name. An empty name represents the default library.
    /// </param>
    /// <param name="library">The library to mount.</param>
    ///
    /// <returns>
    /// <see cref="Error.Ok"/> on success, <see cref="Error.InvalidParameter"/>
    /// for a <c>null</c> library, or <see cref="Error.AlreadyExists"/> when the
    /// name is already used.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the player on the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RemoveAnimationLibrary"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Error AddAnimationLibrary(StringName name, AnimationLibrary library)
    {
        ThrowIfFreed();
        if (library is null)
        {
            return Error.InvalidParameter;
        }

        if (libraries.ContainsKey(name))
        {
            return Error.AlreadyExists;
        }

        libraries.Add(name, library);
        return Error.Ok;
    }

    /// <summary>
    /// Removes an animation library.
    /// </summary>
    ///
    /// <param name="name">
    /// The library name. An empty name addresses the default library.
    /// </param>
    ///
    /// <remarks>
    /// Removing a missing library is a no-op.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Mutate the player on the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="AnimationPlayer" />
    ///
    public void RemoveAnimationLibrary(StringName name)
    {
        ThrowIfFreed();
        libraries.Remove(name);
    }

    /// <summary>
    /// Checks whether an animation library is mounted.
    /// </summary>
    ///
    /// <param name="name">
    /// The library name. An empty name addresses the default library.
    /// </param>
    ///
    /// <returns><c>true</c> when the library exists; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public bool HasAnimationLibrary(StringName name)
    {
        ThrowIfFreed();
        return libraries.ContainsKey(name);
    }

    /// <summary>
    /// Gets an animation library.
    /// </summary>
    ///
    /// <param name="name">
    /// The library name. An empty name addresses the default library.
    /// </param>
    ///
    /// <returns>The mounted library, or <c>null</c> when missing.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public AnimationLibrary? GetAnimationLibrary(StringName name)
    {
        ThrowIfFreed();
        return libraries.TryGetValue(name, out var library) ? library : null;
    }

    /// <summary>
    /// Gets mounted library names in deterministic alphabetical order.
    /// </summary>
    ///
    /// <returns>An array containing all mounted library names.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public StringName[] GetAnimationLibraryList()
    {
        ThrowIfFreed();
        return libraries.Keys
            .OrderBy(name => name.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Checks whether an animation name resolves through mounted libraries.
    /// </summary>
    ///
    /// <param name="name">
    /// A bare animation name for the default library, or
    /// <c>library/animation</c> for a named library.
    /// </param>
    ///
    /// <returns><c>true</c> when the animation resolves; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public bool HasAnimation(StringName name)
    {
        ThrowIfFreed();
        return TryResolveAnimation(name, out _, out _, out _);
    }

    /// <summary>
    /// Starts playback of an animation.
    /// </summary>
    ///
    /// <param name="name">
    /// The animation to play, or an empty value to use
    /// <see cref="AssignedAnimation"/>.
    /// </param>
    /// <param name="customBlend">
    /// Reserved blend time parameter. The 0.1-preview runtime accepts the
    /// value but does not blend animations yet.
    /// </param>
    /// <param name="customSpeed">The finite custom speed multiplier.</param>
    /// <param name="fromEnd">
    /// <c>true</c> to start at the end of the animation; otherwise, <c>false</c>.
    /// </param>
    ///
    /// <remarks>
    /// Missing animations leave playback stopped. Value tracks are applied at
    /// the starting position immediately; method tracks are not called until
    /// time advances across their key times.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="customBlend"/> or
    /// <paramref name="customSpeed"/> is not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Queue"/>
    /// <seealso cref="Stop"/>
    public void Play(StringName name = default, double customBlend = -1d, float customSpeed = 1f, bool fromEnd = false)
    {
        ThrowIfFreed();
        if (!double.IsFinite(customBlend))
        {
            throw new ArgumentOutOfRangeException(nameof(customBlend), customBlend, "Custom blend must be finite.");
        }

        if (!Mathf.IsFinite(customSpeed))
        {
            throw new ArgumentOutOfRangeException(nameof(customSpeed), customSpeed, "Custom speed must be finite.");
        }

        var requestedName = name.IsEmpty() ? AssignedAnimation : name;
        if (!TryResolveAnimation(requestedName, out _, out _, out var animation))
        {
            playing = false;
            return;
        }

        AssignedAnimation = requestedName;
        CurrentAnimation = requestedName;
        CurrentAnimationPosition = fromEnd ? animation.Length : 0d;
        this.customSpeed = customSpeed;
        playing = true;
        ApplyValueTracks(animation, CurrentAnimationPosition);
    }

    /// <summary>
    /// Appends an animation to the playback queue.
    /// </summary>
    ///
    /// <param name="name">The animation name to queue.</param>
    ///
    /// <remarks>
    /// Missing or empty names are ignored. Queued animations start after the
    /// current non-looping animation emits <c>animation_finished</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetQueue"/>
    /// <seealso cref="ClearQueue"/>
    public void Queue(StringName name)
    {
        ThrowIfFreed();
        if (!name.IsEmpty() && HasAnimation(name))
        {
            animationQueue.Enqueue(name);
        }
    }

    /// <summary>
    /// Gets queued animation names.
    /// </summary>
    ///
    /// <returns>The queued names in playback order.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Queue"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public StringName[] GetQueue()
    {
        ThrowIfFreed();
        return animationQueue.ToArray();
    }

    /// <summary>
    /// Clears the playback queue.
    /// </summary>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Queue"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void ClearQueue()
    {
        ThrowIfFreed();
        animationQueue.Clear();
    }

    /// <summary>
    /// Pauses playback at the current position.
    /// </summary>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    /// <seealso cref="Stop"/>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void Pause()
    {
        ThrowIfFreed();
        playing = false;
    }

    /// <summary>
    /// Stops playback.
    /// </summary>
    ///
    /// <param name="keepState">
    /// <c>true</c> to keep the current position; <c>false</c> to reset to the
    /// beginning of the current animation.
    /// </param>
    ///
    /// <remarks>
    /// When <paramref name="keepState"/> is <c>false</c>, value tracks are
    /// applied at time <c>0</c> if the current animation can still be resolved.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Pause"/>
    public void Stop(bool keepState = false)
    {
        ThrowIfFreed();
        playing = false;
        customSpeed = 1f;
        if (!keepState)
        {
            CurrentAnimationPosition = 0d;
            if (TryResolveAnimation(CurrentAnimation, out _, out _, out var animation))
            {
                ApplyValueTracks(animation, CurrentAnimationPosition);
            }
        }
    }

    /// <summary>
    /// Gets whether playback is active.
    /// </summary>
    ///
    /// <returns><c>true</c> when playback is active; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="AnimationPlayer" />
    ///
    public bool IsPlaying()
    {
        ThrowIfFreed();
        return playing;
    }

    /// <summary>
    /// Advances playback by a time delta.
    /// </summary>
    ///
    /// <param name="delta">The finite, non-negative delta in seconds.</param>
    ///
    /// <remarks>
    /// This method is called automatically from <see cref="SceneTree"/>
    /// processing. Calling it manually is useful for deterministic tests and
    /// tools that need to advance a player without a full frame.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="delta"/> is not finite or is less than
    /// <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="AnimationPlayer" />
    ///
    public void Advance(double delta)
    {
        ThrowIfFreed();
        if (!double.IsFinite(delta) || delta < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Animation advance delta must be finite and non-negative.");
        }

        if (!playing)
        {
            return;
        }

        if (!TryResolveAnimation(CurrentAnimation, out _, out _, out var animation))
        {
            playing = false;
            return;
        }

        var playbackSpeed = speedScale * customSpeed;
        if (playbackSpeed == 0f)
        {
            return;
        }

        if (playbackSpeed < 0f)
        {
            AdvanceBackward(animation, delta, playbackSpeed);
            return;
        }

        AdvanceForward(animation, delta, playbackSpeed);
    }

    /// <summary>
    /// Starts autoplay after the player becomes ready.
    /// </summary>
    ///
    /// <remarks>
    /// If <see cref="Autoplay"/> is not empty, this callback calls
    /// <see cref="Play"/> with that animation name.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="AnimationPlayer" />
    ///
    public override void _Ready()
    {
        base._Ready();
        if (!Autoplay.IsEmpty())
        {
            Play(Autoplay);
        }
    }

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
    }

    void ISceneTreeLifecycleHandler.OnProcess(double delta)
    {
        Advance(delta);
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
    }

    private void AdvanceForward(Animation animation, double delta, float playbackSpeed)
    {
        var previous = CurrentAnimationPosition;
        var next = previous + delta * playbackSpeed;
        if (animation.LoopMode == Animation.LoopModeEnum.Linear && animation.Length > 0d)
        {
            AdvanceForwardLooping(animation, previous, next);
            return;
        }

        if (next >= animation.Length)
        {
            CallMethodTracks(animation, previous, animation.Length);
            CurrentAnimationPosition = animation.Length;
            ApplyValueTracks(animation, CurrentAnimationPosition);
            FinishCurrentAnimation();
            return;
        }

        CurrentAnimationPosition = next;
        CallMethodTracks(animation, previous, next);
        ApplyValueTracks(animation, CurrentAnimationPosition);
    }

    private void AdvanceForwardLooping(Animation animation, double previous, double next)
    {
        var position = previous;
        var target = next;
        while (target >= animation.Length)
        {
            CallMethodTracks(animation, position, animation.Length);
            target -= animation.Length;
            position = 0d;
        }

        CurrentAnimationPosition = target;
        CallMethodTracks(animation, position, CurrentAnimationPosition);
        ApplyValueTracks(animation, CurrentAnimationPosition);
    }

    private void AdvanceBackward(Animation animation, double delta, float playbackSpeed)
    {
        var previous = CurrentAnimationPosition;
        var next = previous + delta * playbackSpeed;
        if (next <= 0d)
        {
            CurrentAnimationPosition = 0d;
            ApplyValueTracks(animation, CurrentAnimationPosition);
            FinishCurrentAnimation();
            return;
        }

        CurrentAnimationPosition = next;
        ApplyValueTracks(animation, CurrentAnimationPosition);
    }

    private void FinishCurrentAnimation()
    {
        var finishedName = CurrentAnimation;
        playing = false;
        EmitSignal(AnimationFinishedSignal, finishedName);

        while (animationQueue.Count > 0 && !playing)
        {
            Play(animationQueue.Dequeue());
        }
    }

    private void ApplyValueTracks(Animation animation, double time)
    {
        for (var trackIdx = 0; trackIdx < animation.GetTrackCount(); trackIdx++)
        {
            if (animation.TrackGetType(trackIdx) != Animation.TrackTypeEnum.Value ||
                !animation.TrackIsEnabled(trackIdx))
            {
                continue;
            }

            var path = animation.TrackGetPath(trackIdx);
            var subnames = GetSubnames(path);
            if (subnames.Length == 0)
            {
                continue;
            }

            var target = ResolveTrackTarget(path);
            if (target is null)
            {
                continue;
            }

            var value = animation.ValueTrackInterpolate(trackIdx, time);
            TrySetMemberPath(target, subnames, 0, value);
        }
    }

    private void CallMethodTracks(Animation animation, double previous, double current)
    {
        if (current < previous)
        {
            return;
        }

        for (var trackIdx = 0; trackIdx < animation.GetTrackCount(); trackIdx++)
        {
            if (animation.TrackGetType(trackIdx) != Animation.TrackTypeEnum.Method ||
                !animation.TrackIsEnabled(trackIdx))
            {
                continue;
            }

            var target = ResolveTrackTarget(animation.TrackGetPath(trackIdx));
            if (target is null)
            {
                continue;
            }

            foreach (var key in animation.GetMethodTrackKeys(trackIdx))
            {
                if (key.Time > previous && key.Time <= current)
                {
                    CallMethodKey(target, key);
                }
            }
        }
    }

    private void CallMethodKey(Node target, AnimationMethodKey key)
    {
        var arguments = key.Arguments.Select(VariantToObject).ToArray();
        var callable = new Callable(target, key.Method.ToString());
        if (callable.TryCall(arguments, out _, out var exception) == Error.Ok || exception is null)
        {
            return;
        }

        GetTree()?.ReportUserCodeException(
            target,
            key.Method.ToString(),
            exception,
            RuntimeUserCodeFailureKind.LifecycleCallback);
    }

    private Node? ResolveTrackTarget(NodePath trackPath)
    {
        var root = ResolveAnimationRoot();
        return root?.GetNodeOrNull(trackPath);
    }

    private Node? ResolveAnimationRoot()
    {
        if (RootNode.IsEmpty())
        {
            return this;
        }

        return GetNodeOrNull(RootNode) ?? this;
    }

    private bool TryResolveAnimation(
        StringName requestedName,
        out StringName libraryName,
        out StringName animationName,
        out Animation animation)
    {
        animation = null!;
        libraryName = default;
        animationName = default;
        if (requestedName.IsEmpty())
        {
            return false;
        }

        SplitAnimationName(requestedName, out libraryName, out animationName);
        if (!libraries.TryGetValue(libraryName, out var library))
        {
            return false;
        }

        var resolved = library.GetAnimation(animationName);
        if (resolved is null)
        {
            return false;
        }

        animation = resolved;
        return true;
    }

    private static void SplitAnimationName(StringName requestedName, out StringName libraryName, out StringName animationName)
    {
        var text = requestedName.ToString();
        var separator = text.IndexOf('/', StringComparison.Ordinal);
        if (separator < 0)
        {
            libraryName = default;
            animationName = requestedName;
            return;
        }

        libraryName = new StringName(text[..separator]);
        animationName = new StringName(text[(separator + 1)..]);
    }

    private static string[] GetSubnames(NodePath path)
    {
        var count = path.GetSubnameCount();
        var subnames = new string[count];
        for (var index = 0; index < count; index++)
        {
            subnames[index] = path.GetSubname(index);
        }

        return subnames;
    }

    private static bool TrySetMemberPath(object target, string[] names, int index, Variant value)
    {
        if (index == names.Length - 1)
        {
            return TrySetDirectMember(target, names[index], value);
        }

        if (!TryGetMember(target, names[index], out var member, out var memberValue) ||
            memberValue is null ||
            !TrySetMemberPath(memberValue, names, index + 1, value))
        {
            return false;
        }

        return TrySetMemberValue(target, member, memberValue);
    }

    private static bool TrySetDirectMember(object target, string name, Variant value)
    {
        if (!TryFindWritableMember(target.GetType(), name, out var member, out var memberType))
        {
            return false;
        }

        if (!TryConvertVariant(value, memberType, out var converted))
        {
            return false;
        }

        return TrySetMemberValue(target, member, converted);
    }

    private static bool TryGetMember(object target, string name, out MemberInfo member, out object? value)
    {
        value = null;
        if (!TryFindWritableMember(target.GetType(), name, out member, out _))
        {
            return false;
        }

        value = member switch
        {
            PropertyInfo property => property.GetValue(target),
            FieldInfo field => field.GetValue(target),
            _ => null
        };
        return true;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070",
        Justification = "Animation property tracks intentionally inspect public instance properties and fields on runtime target nodes. A future metadata registry will replace this reflection path for stricter AOT targets.")]
    private static bool TryFindWritableMember(Type type, string name, out MemberInfo member, out Type memberType)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;
        member = type.GetProperty(name, Flags) as MemberInfo ?? type.GetField(name, Flags)!;
        if (member is null)
        {
            member = type
                .GetProperties(Flags)
                .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)) as MemberInfo ??
                type
                    .GetFields(Flags)
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))!;
        }

        memberType = member switch
        {
            PropertyInfo { CanWrite: true } property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => null!
        };

        return memberType is not null;
    }

    private static bool TrySetMemberValue(object target, MemberInfo member, object? value)
    {
        switch (member)
        {
            case PropertyInfo property when property.CanWrite:
                property.SetValue(target, value);
                return true;
            case FieldInfo field:
                field.SetValue(target, value);
                return true;
            default:
                return false;
        }
    }

    private static bool TryConvertVariant(Variant value, Type targetType, out object? converted)
    {
        converted = null;
        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value.IsNil())
        {
            return !nonNullableType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
        }

        if (nonNullableType == typeof(Variant))
        {
            converted = value;
            return true;
        }

        if (nonNullableType.IsEnum && value.VariantType == Variant.Type.Int)
        {
            converted = Enum.ToObject(nonNullableType, value.AsInt64());
            return true;
        }

        if (TryConvertKnownVariant(value, nonNullableType, out converted))
        {
            return true;
        }

        var boxed = VariantToObject(value);
        if (boxed is not null && nonNullableType.IsInstanceOfType(boxed))
        {
            converted = boxed;
            return true;
        }

        if (boxed is IConvertible && typeof(IConvertible).IsAssignableFrom(nonNullableType))
        {
            try
            {
                converted = Convert.ChangeType(boxed, nonNullableType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryConvertKnownVariant(Variant value, Type targetType, out object? converted)
    {
        converted = null;
        if (targetType == typeof(bool) && value.VariantType == Variant.Type.Bool)
        {
            converted = value.AsBool();
            return true;
        }

        if (targetType == typeof(string))
        {
            converted = value.VariantType == Variant.Type.StringName
                ? value.AsStringName().ToString()
                : value.Obj?.ToString();
            return true;
        }

        if (targetType == typeof(float) && value.VariantType is Variant.Type.Float or Variant.Type.Int)
        {
            converted = value.VariantType == Variant.Type.Float ? (float)value.AsDouble() : (float)value.AsInt64();
            return true;
        }

        if (targetType == typeof(double) && value.VariantType is Variant.Type.Float or Variant.Type.Int)
        {
            converted = value.VariantType == Variant.Type.Float ? value.AsDouble() : (double)value.AsInt64();
            return true;
        }

        if (targetType == typeof(int) && value.VariantType == Variant.Type.Int)
        {
            converted = value.AsInt32();
            return true;
        }

        if (targetType == typeof(long) && value.VariantType == Variant.Type.Int)
        {
            converted = value.AsInt64();
            return true;
        }

        if (targetType == typeof(Vector2) && value.VariantType == Variant.Type.Vector2)
        {
            converted = value.AsVector2();
            return true;
        }

        if (targetType == typeof(Color) && value.VariantType == Variant.Type.Color)
        {
            converted = value.AsColor();
            return true;
        }

        if (targetType == typeof(StringName))
        {
            converted = value.VariantType == Variant.Type.StringName
                ? value.AsStringName()
                : new StringName(value.Obj?.ToString());
            return true;
        }

        if (targetType == typeof(NodePath) && value.VariantType == Variant.Type.NodePath)
        {
            converted = value.AsNodePath();
            return true;
        }

        if (targetType == typeof(Rid) && value.VariantType == Variant.Type.Rid)
        {
            converted = value.AsRid();
            return true;
        }

        if (targetType == typeof(Callable) && value.VariantType == Variant.Type.Callable)
        {
            converted = value.AsCallable();
            return true;
        }

        if (typeof(Object).IsAssignableFrom(targetType) && value.VariantType == Variant.Type.Object)
        {
            converted = value.AsObject();
            return converted is null || targetType.IsInstanceOfType(converted);
        }

        return false;
    }

    private static object? VariantToObject(Variant value)
    {
        return value.IsNil() ? null : value.Obj;
    }
}
