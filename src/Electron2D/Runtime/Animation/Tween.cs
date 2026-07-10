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
/// Runs sequential property, interval and callback animation steps.
/// </summary>
///
/// <remarks>
/// <para>
/// Create runtime tweens with <see cref="SceneTree.CreateTween"/> or
/// <see cref="Node.CreateTween"/>. A manually constructed tween is useful only
/// as an invalid value for API parity checks and cannot receive tweeners.
/// </para>
/// <para>
/// Electron2D 0.1-preview processes tweens after node process callbacks and
/// before draw callbacks. This baseline supports sequential tweeners, easing,
/// pause/resume, stop, cancellation and completion signals.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tweens on the main scene
/// thread that owns the scene tree.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="SceneTree.CreateTween"/>
/// <seealso cref="Node.CreateTween"/>
/// <seealso cref="Tweener"/>
public class Tween : RefCounted
{
    private const string FinishedSignal = "finished";
    private const string StepFinishedSignal = "step_finished";
    internal const double Epsilon = 0.000000000001d;

    private readonly List<Tweener> _tweeners = new();
    private SceneTree? _tree;
    private Node? _boundNode;
    private TransitionType _defaultTransition = TransitionType.Linear;
    private EaseType _defaultEase = EaseType.InOut;
    private int _currentIndex;
    private bool _valid;
    private bool _running;
    private double _totalElapsedTime;
    private double _speedScale = 1d;

    /// <summary>
    /// Creates an invalid standalone tween.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Standalone tweens are not registered in a scene tree and cannot receive
    /// tweeners. Use <see cref="SceneTree.CreateTween"/> or
    /// <see cref="Node.CreateTween"/> for runtime animation.
    /// </para>
    /// <para>
    /// The constructor registers the <c>finished</c> and
    /// <c>step_finished</c> signals so callers can inspect signal availability
    /// even before the tween is registered.
    /// </para>
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
    ///
    /// <seealso cref="IsValid"/>
    public Tween()
    {
        AddUserSignal(FinishedSignal);
        AddUserSignal(StepFinishedSignal);
    }

    internal Tween(SceneTree tree)
        : this()
    {
        _tree = tree;
        _valid = true;
        _running = true;
    }

    /// <summary>
    /// Lists transition curves available to tween interpolation.
    /// </summary>
    ///
    /// <remarks>
    /// A transition curve shapes normalized time before it is applied to the
    /// animated value. The current release implements deterministic scalar
    /// formulas for all listed values.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    public enum TransitionType
    {
        /// <summary>
        /// Uses the normalized time without curve shaping.
        /// </summary>
        ///
        /// <remarks>
        /// The interpolated value advances at a constant rate.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Linear = 0,

        /// <summary>
        /// Uses a sine-shaped curve.
        /// </summary>
        ///
        /// <remarks>
        /// This curve starts or ends smoothly depending on the selected
        /// <see cref="EaseType"/>.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Sine = 1,

        /// <summary>
        /// Uses a fifth-power curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve is steeper than <see cref="Quart"/> and
        /// <see cref="Quad"/>.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Quint = 2,

        /// <summary>
        /// Uses a fourth-power curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve is steeper than <see cref="Cubic"/> and
        /// <see cref="Quad"/>.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Quart = 3,

        /// <summary>
        /// Uses a quadratic curve.
        /// </summary>
        ///
        /// <remarks>
        /// With <see cref="EaseType.In"/>, half of the duration produces
        /// one quarter of the value delta.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Quad = 4,

        /// <summary>
        /// Uses an exponential curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve changes slowly near the eased side and quickly near the
        /// opposite side.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Expo = 5,

        /// <summary>
        /// Uses an elastic overshoot curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve may move past the target range before settling.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Elastic = 6,

        /// <summary>
        /// Uses a cubic curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve is a middle ground between <see cref="Quad"/> and
        /// <see cref="Quart"/>.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Cubic = 7,

        /// <summary>
        /// Uses a circular curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve follows a quarter-circle acceleration profile.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Circ = 8,

        /// <summary>
        /// Uses a bounce curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve produces deterministic bounce-like rebounds near the
        /// eased side.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Bounce = 9,

        /// <summary>
        /// Uses a backtracking overshoot curve.
        /// </summary>
        ///
        /// <remarks>
        /// The curve initially moves opposite the target direction for
        /// <see cref="EaseType.In"/> and mirrors that behavior for
        /// <see cref="EaseType.Out"/>.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Back = 10,

        /// <summary>
        /// Uses a smooth spring-style curve.
        /// </summary>
        ///
        /// <remarks>
        /// The current baseline maps this value to a deterministic smoothstep
        /// curve without oscillation.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TransitionType" />
        ///
        Spring = 11
    }

    /// <summary>
    /// Lists how a transition curve is applied to normalized time.
    /// </summary>
    ///
    /// <remarks>
    /// Easing controls which part of the interpolation receives the selected
    /// <see cref="TransitionType"/> curve.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    public enum EaseType
    {
        /// <summary>
        /// Applies the transition curve at the beginning of the interpolation.
        /// </summary>
        ///
        /// <remarks>
        /// Values start slowly for accelerating transition curves.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="EaseType" />
        ///
        In = 0,

        /// <summary>
        /// Applies the transition curve at the end of the interpolation.
        /// </summary>
        ///
        /// <remarks>
        /// Values end slowly for accelerating transition curves.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="EaseType" />
        ///
        Out = 1,

        /// <summary>
        /// Applies the transition curve to both halves of the interpolation.
        /// </summary>
        ///
        /// <remarks>
        /// Values start and end smoothly for accelerating transition curves.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="EaseType" />
        ///
        InOut = 2,

        /// <summary>
        /// Applies out easing to the first half and in easing to the second half.
        /// </summary>
        ///
        /// <remarks>
        /// Values move quickly away from the start, slow near the middle and
        /// accelerate toward the end.
        /// </remarks>
        /// <since>
        /// This API is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="EaseType" />
        ///
        OutIn = 3
    }

    /// <summary>
    /// Binds this tween to a node lifetime.
    /// </summary>
    ///
    /// <param name="node">The node that controls automatic tween processing.</param>
    ///
    /// <returns>This tween instance.</returns>
    ///
    /// <remarks>
    /// <para>
    /// A bound tween advances automatically only while the node is valid and
    /// inside a scene tree. The tween remains valid while the node is detached,
    /// but <see cref="IsRunning"/> returns <c>false</c> until the node is valid
    /// again.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this tween is not registered in a scene tree.
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
    /// <seealso cref="Node.CreateTween"/>
    public Tween BindNode(Node node)
    {
        EnsureValid();
        ArgumentNullException.ThrowIfNull(node);
        if (!ElectronObject.IsInstanceValid(node))
        {
            throw new InvalidOperationException("Bound node must be a valid instance.");
        }

        _boundNode = node;
        return this;
    }

    /// <summary>
    /// Adds a property tweener to this tween.
    /// </summary>
    ///
    /// <param name="object">The ElectronObject that owns the property.</param>
    /// <param name="property">The property path to write.</param>
    /// <param name="finalVal">The final value to assign when the tweener completes.</param>
    /// <param name="duration">The non-negative duration in seconds.</param>
    ///
    /// <returns>The created <see cref="PropertyTweener"/>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The property path may address a public property or field by name, and
    /// may address nested public members with colon-separated subnames such as
    /// <c>Position:X</c>.
    /// </para>
    /// <para>
    /// The initial value is captured the first time this tweener starts. Later
    /// calls to <see cref="Stop"/> reset elapsed time but keep that captured
    /// initial value.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="object"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="duration"/> is negative or not finite.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this tween is not valid.
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
    /// <seealso cref="PropertyTweener"/>
    /// <seealso cref="TweenInterval"/>
    /// <seealso cref="TweenCallback"/>
    public PropertyTweener TweenProperty(ElectronObject @object, NodePath property, Variant finalVal, double duration)
    {
        EnsureValid();
        ArgumentNullException.ThrowIfNull(@object);
        ValidateFiniteNonNegative(duration, nameof(duration));

        var names = GetPropertyNames(property);
        if (names.Length == 0)
        {
            throw new ArgumentException("Property path must contain at least one member name.", nameof(property));
        }

        var tweener = new PropertyTweener(this, @object, names, finalVal, duration, _defaultTransition, _defaultEase);
        _tweeners.Add(tweener);
        return tweener;
    }

    /// <summary>
    /// Adds a callback tweener to this tween.
    /// </summary>
    ///
    /// <param name="callback">The callable to invoke when the sequence reaches this step.</param>
    ///
    /// <returns>The created <see cref="CallbackTweener"/>.</returns>
    ///
    /// <remarks>
    /// The callback is called once when its turn in the sequence is reached.
    /// Calling <see cref="Kill"/> before the step is reached prevents the
    /// callback from running.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="callback"/> is null-like.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this tween is not valid.
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
    /// <seealso cref="CallbackTweener"/>
    /// <seealso cref="TweenInterval"/>
    public CallbackTweener TweenCallback(Callable callback)
    {
        EnsureValid();
        if (callback.IsNull())
        {
            throw new ArgumentException("Callback must not be null.", nameof(callback));
        }

        var tweener = new CallbackTweener(this, callback);
        _tweeners.Add(tweener);
        return tweener;
    }

    /// <summary>
    /// Adds a time-only interval to this tween.
    /// </summary>
    ///
    /// <param name="time">The non-negative interval duration in seconds.</param>
    ///
    /// <returns>The created <see cref="IntervalTweener"/>.</returns>
    ///
    /// <remarks>
    /// An interval consumes time without modifying target objects. It is useful
    /// for delaying later property or callback steps.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="time"/> is negative or not finite.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this tween is not valid.
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
    /// <seealso cref="IntervalTweener"/>
    /// <seealso cref="TweenCallback"/>
    public IntervalTweener TweenInterval(double time)
    {
        EnsureValid();
        ValidateFiniteNonNegative(time, nameof(time));

        var tweener = new IntervalTweener(this, time);
        _tweeners.Add(tweener);
        return tweener;
    }

    /// <summary>
    /// Sets the default transition for subsequently added property tweeners.
    /// </summary>
    ///
    /// <param name="trans">The transition curve to use.</param>
    ///
    /// <returns>This tween instance.</returns>
    ///
    /// <remarks>
    /// Existing property tweeners keep the transition they had when they were
    /// created or later changed through <see cref="PropertyTweener.SetTrans"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trans"/> is outside the supported enum range.
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
    /// <seealso cref="SetEase"/>
    public Tween SetTrans(TransitionType trans)
    {
        EnsureTransition(trans);
        _defaultTransition = trans;
        return this;
    }

    /// <summary>
    /// Sets the default ease mode for subsequently added property tweeners.
    /// </summary>
    ///
    /// <param name="ease">The ease mode to use.</param>
    ///
    /// <returns>This tween instance.</returns>
    ///
    /// <remarks>
    /// Existing property tweeners keep the ease mode they had when they were
    /// created or later changed through <see cref="PropertyTweener.SetEase"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ease"/> is outside the supported enum range.
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
    /// <seealso cref="SetTrans"/>
    public Tween SetEase(EaseType ease)
    {
        EnsureEase(ease);
        _defaultEase = ease;
        return this;
    }

    /// <summary>
    /// Sets the speed multiplier used by future tween processing steps.
    /// </summary>
    ///
    /// <param name="speed">The finite non-negative speed multiplier.</param>
    ///
    /// <returns>This tween instance.</returns>
    ///
    /// <remarks>
    /// A value of <c>1</c> advances at normal speed. A value of <c>0</c> keeps
    /// the tween valid but prevents positive frame deltas from advancing time.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="speed"/> is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="Tween" />
    ///
    public Tween SetSpeedScale(double speed)
    {
        ValidateFiniteNonNegative(speed, nameof(speed));
        _speedScale = speed;
        return this;
    }

    /// <summary>
    /// Pauses automatic processing for this tween.
    /// </summary>
    ///
    /// <remarks>
    /// Pausing preserves the current step and elapsed time. Use
    /// <see cref="Play"/> to resume automatic processing or
    /// <see cref="CustomStep"/> to advance manually while paused.
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
    /// <seealso cref="Play"/>
    public void Pause()
    {
        ThrowIfFreed();
        if (_valid)
        {
            _running = false;
        }
    }

    /// <summary>
    /// Starts or resumes automatic processing for this tween.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Calling this method after <see cref="Pause"/> resumes from the current
    /// step. Calling it after <see cref="Stop"/> starts again from elapsed time
    /// zero.
    /// </para>
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
    /// <seealso cref="Stop"/>
    public void Play()
    {
        ThrowIfFreed();
        if (_valid && _currentIndex < _tweeners.Count)
        {
            _running = true;
        }
    }

    /// <summary>
    /// Stops this tween and rewinds its sequence time.
    /// </summary>
    ///
    /// <remarks>
    /// Stop keeps the tween valid and does not roll target properties back.
    /// Previously captured initial values are kept so a later <see cref="Play"/>
    /// starts the same sequence again from time zero.
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
    /// <seealso cref="Play"/>
    /// <seealso cref="Kill"/>
    public void Stop()
    {
        ThrowIfFreed();
        if (!_valid)
        {
            return;
        }

        _running = false;
        _currentIndex = 0;
        _totalElapsedTime = 0d;
        foreach (var tweener in _tweeners)
        {
            tweener.Reset();
        }
    }

    /// <summary>
    /// Cancels this tween and removes it from scene tree processing.
    /// </summary>
    ///
    /// <remarks>
    /// Killing a tween clears pending tweeners and does not emit the
    /// <c>finished</c> signal.
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
    /// <seealso cref="Stop"/>
    public void Kill()
    {
        ThrowIfFreed();
        Invalidate(emitFinished: false, clearTweeners: true);
    }

    /// <summary>
    /// Manually advances this tween by a delta time.
    /// </summary>
    ///
    /// <param name="delta">The finite non-negative delta time in seconds.</param>
    ///
    /// <returns>
    /// <c>true</c> when the tween is still valid after the step; otherwise,
    /// <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Manual stepping works while the tween is paused. It still respects bound
    /// node validity and does not advance a tween whose bound node is outside
    /// the scene tree.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="delta"/> is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="Tween" />
    ///
    public bool CustomStep(double delta)
    {
        ThrowIfFreed();
        ValidateFiniteNonNegative(delta, nameof(delta));
        Step(delta, ignoreRunningState: true);
        return _valid;
    }

    /// <summary>
    /// Checks whether this tween is currently advancing automatically.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when this tween is valid, playing, has pending tweeners and
    /// its bound node can be processed; otherwise, <c>false</c>.
    /// </returns>
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
    /// <seealso cref="Tween" />
    ///
    public bool IsRunning()
    {
        ThrowIfFreed();
        return _valid && _running && HasProcessableBinding() && _currentIndex < _tweeners.Count;
    }

    /// <summary>
    /// Checks whether this tween is registered and can accept tweeners.
    /// </summary>
    ///
    /// <returns><c>true</c> when the tween is valid; otherwise, <c>false</c>.</returns>
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
    /// <seealso cref="Tween" />
    ///
    public bool IsValid()
    {
        ThrowIfFreed();
        return _valid;
    }

    /// <summary>
    /// Checks whether this tween has any tweeners.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when this tween is valid and contains at least one tweener;
    /// otherwise, <c>false</c>.
    /// </returns>
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
    /// <seealso cref="Tween" />
    ///
    public bool HasTweeners()
    {
        ThrowIfFreed();
        return _valid && _tweeners.Count > 0;
    }

    /// <summary>
    /// Gets the accumulated processed time for this tween.
    /// </summary>
    ///
    /// <returns>
    /// The total elapsed time in seconds after speed scaling has been applied.
    /// </returns>
    ///
    /// <remarks>
    /// The value is reset by <see cref="Stop"/> and stops increasing while the
    /// tween is paused unless <see cref="CustomStep"/> is called.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="Tween" />
    ///
    public double GetTotalElapsedTime()
    {
        ThrowIfFreed();
        return _totalElapsedTime;
    }

    /// <summary>
    /// Computes an eased interpolated value from an initial value and delta.
    /// </summary>
    ///
    /// <param name="initialValue">The value at elapsed time zero.</param>
    /// <param name="deltaValue">The difference between the final and initial values.</param>
    /// <param name="elapsedTime">The finite non-negative elapsed time in seconds.</param>
    /// <param name="duration">The finite non-negative duration in seconds.</param>
    /// <param name="transType">The transition curve to apply.</param>
    /// <param name="easeType">The ease mode to apply.</param>
    ///
    /// <returns>
    /// The interpolated value for supported numeric, <see cref="Vector2"/> and
    /// <see cref="Color"/> variants; otherwise, <paramref name="initialValue"/>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The normalized interpolation weight is clamped to the range
    /// <c>0</c> through <c>1</c>. A zero duration produces the final supported
    /// value immediately.
    /// </para>
    /// <para>
    /// The supported type set is intentionally small for 0.1-preview:
    /// integer, floating-point, <see cref="Vector2"/> and <see cref="Color"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a numeric argument is negative or not finite, or when an enum
    /// value is outside the supported range.
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
    /// <seealso cref="TweenProperty"/>
    public static Variant InterpolateValue(
        Variant initialValue,
        Variant deltaValue,
        double elapsedTime,
        double duration,
        TransitionType transType,
        EaseType easeType)
    {
        ValidateFiniteNonNegative(elapsedTime, nameof(elapsedTime));
        ValidateFiniteNonNegative(duration, nameof(duration));
        EnsureTransition(transType);
        EnsureEase(easeType);

        var linearWeight = duration <= Epsilon ? 1d : Math.Clamp(elapsedTime / duration, 0d, 1d);
        var weight = (float)ApplyEase(linearWeight, transType, easeType);
        return initialValue.VariantType switch
        {
            Variant.Type.Int when deltaValue.VariantType == Variant.Type.Int =>
                Variant.From(initialValue.AsInt64() + (long)Math.Round(deltaValue.AsInt64() * weight)),
            Variant.Type.Float when deltaValue.VariantType is Variant.Type.Float or Variant.Type.Int =>
                Variant.From(initialValue.AsDouble() + (GetNumericDelta(deltaValue) * weight)),
            Variant.Type.Vector2 when deltaValue.VariantType == Variant.Type.Vector2 =>
                Variant.From(initialValue.AsVector2() + (deltaValue.AsVector2() * weight)),
            Variant.Type.Color when deltaValue.VariantType == Variant.Type.Color =>
                Variant.From(initialValue.AsColor() + (deltaValue.AsColor() * weight)),
            _ => initialValue
        };
    }

    internal void Process(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0d)
        {
            return;
        }

        Step(delta, ignoreRunningState: false);
    }

    internal void ReportCallbackFailure(Callable callback, Exception exception)
    {
        var node = callback.GetObject() as Node ?? _boundNode;
        var tree = node?.GetTree() ?? _tree;
        tree?.ReportUserCodeException(
            node,
            callback.GetMethod(),
            exception,
            RuntimeUserCodeFailureKind.LifecycleCallback);
    }

    internal static void ValidateDelay(double delay, string parameterName)
    {
        ValidateFiniteNonNegative(delay, parameterName);
    }

    internal static void ValidateTransition(TransitionType transition)
    {
        EnsureTransition(transition);
    }

    internal static void ValidateEase(EaseType ease)
    {
        EnsureEase(ease);
    }

    internal static bool TryGetMemberPath(object target, string[] names, int index, out Variant value)
    {
        value = default;
        if (index == names.Length - 1)
        {
            if (!TryGetMember(target, names[index], out _, out var memberValue))
            {
                return false;
            }

            try
            {
                value = Variant.CreateFrom(memberValue);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return TryGetMember(target, names[index], out _, out var nestedValue) &&
            nestedValue is not null &&
            TryGetMemberPath(nestedValue, names, index + 1, out value);
    }

    internal static bool TrySetMemberPath(object target, string[] names, int index, Variant value)
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

    internal static bool TryCreateDelta(Variant initialValue, Variant finalValue, out Variant deltaValue)
    {
        deltaValue = default;
        if (initialValue.VariantType != finalValue.VariantType)
        {
            return false;
        }

        switch (initialValue.VariantType)
        {
            case Variant.Type.Int:
                deltaValue = Variant.From(finalValue.AsInt64() - initialValue.AsInt64());
                return true;
            case Variant.Type.Float:
                deltaValue = Variant.From(finalValue.AsDouble() - initialValue.AsDouble());
                return true;
            case Variant.Type.Vector2:
                deltaValue = Variant.From(finalValue.AsVector2() - initialValue.AsVector2());
                return true;
            case Variant.Type.Color:
                deltaValue = Variant.From(finalValue.AsColor() - initialValue.AsColor());
                return true;
            default:
                return false;
        }
    }

    private void Step(double delta, bool ignoreRunningState)
    {
        if (!_valid ||
            (!ignoreRunningState && !_running) ||
            !HasProcessableBinding() ||
            _currentIndex >= _tweeners.Count)
        {
            return;
        }

        var remaining = delta * _speedScale;
        _totalElapsedTime += remaining;

        while (_valid && _currentIndex < _tweeners.Count)
        {
            var result = _tweeners[_currentIndex].Step(remaining);
            if (!result.Completed)
            {
                return;
            }

            EmitSignal(StepFinishedSignal, (long)_currentIndex);
            _currentIndex++;
            remaining = result.RemainingDelta;

            if (_currentIndex >= _tweeners.Count)
            {
                Invalidate(emitFinished: true, clearTweeners: false);
                return;
            }

            if (remaining <= Epsilon)
            {
                return;
            }
        }
    }

    private void EnsureValid()
    {
        ThrowIfFreed();
        if (!_valid)
        {
            throw new InvalidOperationException("Tween is not valid. Create tweens through SceneTree.CreateTween or Node.CreateTween.");
        }
    }

    private bool HasProcessableBinding()
    {
        return _boundNode is null || (ElectronObject.IsInstanceValid(_boundNode) && _boundNode.IsInsideTree());
    }

    private void Invalidate(bool emitFinished, bool clearTweeners)
    {
        if (!_valid)
        {
            return;
        }

        _valid = false;
        _running = false;
        _tree?.UnregisterTween(this);
        _tree = null;
        if (clearTweeners)
        {
            _tweeners.Clear();
        }

        if (emitFinished)
        {
            EmitSignal(FinishedSignal);
        }
    }

    private static string[] GetPropertyNames(NodePath property)
    {
        var nodeNames = property.GetNodeNames();
        var subnameCount = property.GetSubnameCount();
        if (subnameCount == 0)
        {
            return nodeNames;
        }

        var names = new string[nodeNames.Length + subnameCount];
        Array.Copy(nodeNames, names, nodeNames.Length);
        for (var index = 0; index < subnameCount; index++)
        {
            names[nodeNames.Length + index] = property.GetSubname(index);
        }

        return names;
    }

    private static void ValidateFiniteNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
        }
    }

    private static void EnsureTransition(TransitionType transition)
    {
        if (!Enum.IsDefined(transition))
        {
            throw new ArgumentOutOfRangeException(nameof(transition), transition, "Transition type is not supported.");
        }
    }

    private static void EnsureEase(EaseType ease)
    {
        if (!Enum.IsDefined(ease))
        {
            throw new ArgumentOutOfRangeException(nameof(ease), ease, "Ease type is not supported.");
        }
    }

    private static double ApplyEase(double time, TransitionType transition, EaseType ease)
    {
        var clamped = Math.Clamp(time, 0d, 1d);
        return ease switch
        {
            EaseType.In => ApplyTransition(clamped, transition),
            EaseType.Out => 1d - ApplyTransition(1d - clamped, transition),
            EaseType.InOut => clamped < 0.5d
                ? ApplyTransition(clamped * 2d, transition) * 0.5d
                : 1d - (ApplyTransition((1d - clamped) * 2d, transition) * 0.5d),
            EaseType.OutIn => clamped < 0.5d
                ? (1d - ApplyTransition(1d - (clamped * 2d), transition)) * 0.5d
                : 0.5d + (ApplyTransition((clamped * 2d) - 1d, transition) * 0.5d),
            _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, "Ease type is not supported.")
        };
    }

    private static double ApplyTransition(double time, TransitionType transition)
    {
        return transition switch
        {
            TransitionType.Linear => time,
            TransitionType.Sine => 1d - Math.Cos(time * Math.PI * 0.5d),
            TransitionType.Quint => Math.Pow(time, 5d),
            TransitionType.Quart => Math.Pow(time, 4d),
            TransitionType.Quad => time * time,
            TransitionType.Expo => time <= 0d ? 0d : Math.Pow(2d, 10d * (time - 1d)),
            TransitionType.Elastic => ApplyElasticIn(time),
            TransitionType.Cubic => time * time * time,
            TransitionType.Circ => 1d - Math.Sqrt(Math.Max(0d, 1d - (time * time))),
            TransitionType.Bounce => 1d - ApplyBounceOut(1d - time),
            TransitionType.Back => ((1.70158d + 1d) * time * time * time) - (1.70158d * time * time),
            TransitionType.Spring => time * time * (3d - (2d * time)),
            _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, "Transition type is not supported.")
        };
    }

    private static double ApplyElasticIn(double time)
    {
        if (time <= 0d || time >= 1d)
        {
            return time;
        }

        const double Period = (2d * Math.PI) / 3d;
        return -Math.Pow(2d, (10d * time) - 10d) * Math.Sin(((time * 10d) - 10.75d) * Period);
    }

    private static double ApplyBounceOut(double time)
    {
        const double N1 = 7.5625d;
        const double D1 = 2.75d;
        if (time < 1d / D1)
        {
            return N1 * time * time;
        }

        if (time < 2d / D1)
        {
            var shifted = time - (1.5d / D1);
            return (N1 * shifted * shifted) + 0.75d;
        }

        if (time < 2.5d / D1)
        {
            var shifted = time - (2.25d / D1);
            return (N1 * shifted * shifted) + 0.9375d;
        }

        var finalShifted = time - (2.625d / D1);
        return (N1 * finalShifted * finalShifted) + 0.984375d;
    }

    private static double GetNumericDelta(Variant deltaValue)
    {
        return deltaValue.VariantType == Variant.Type.Float ? deltaValue.AsDouble() : deltaValue.AsInt64();
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
        Justification = "Tween property tweeners inspect public runtime target members by name. This is the current script-facing property path baseline until the metadata registry owns writable delegates.")]
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

        var boxed = value.Obj;
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

        if (targetType == typeof(NodePath))
        {
            converted = value.VariantType == Variant.Type.NodePath
                ? value.AsNodePath()
                : new NodePath(value.Obj?.ToString() ?? string.Empty);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Represents one step in a <see cref="Tween"/> sequence.
/// </summary>
///
/// <remarks>
/// This is the public base type returned through concrete tweener classes.
/// Electron2D 0.1-preview executes tweeners sequentially and exposes concrete
/// configuration methods only on the derived tweener types that need them.
/// </remarks>
///
/// <threadsafety>
/// Tweener instances are not synchronized. Configure them on the main scene
/// thread before or during tween processing.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="PropertyTweener"/>
/// <seealso cref="CallbackTweener"/>
/// <seealso cref="IntervalTweener"/>
public abstract class Tweener : RefCounted
{
    private double _delay;
    private double _delayElapsed;
    private bool _completed;

    internal Tweener(Tween owner)
    {
        Owner = owner;
    }

    internal Tween Owner { get; }

    internal TweenStepResult Step(double delta)
    {
        if (_completed)
        {
            return TweenStepResult.Complete(delta);
        }

        if (_delayElapsed + Tween.Epsilon < _delay)
        {
            var neededDelay = _delay - _delayElapsed;
            var consumedDelay = Math.Min(delta, neededDelay);
            _delayElapsed += consumedDelay;
            delta -= consumedDelay;
            if (_delayElapsed + Tween.Epsilon < _delay || delta <= Tween.Epsilon)
            {
                return TweenStepResult.Incomplete();
            }
        }

        var result = StepCore(delta);
        if (result.Completed)
        {
            _completed = true;
        }

        return result;
    }

    internal void Reset()
    {
        _delayElapsed = 0d;
        _completed = false;
        ResetCore();
    }

    internal void SetDelayCore(double delay)
    {
        Tween.ValidateDelay(delay, nameof(delay));
        _delay = delay;
    }

    internal abstract TweenStepResult StepCore(double delta);

    internal virtual void ResetCore()
    {
    }
}

/// <summary>
/// Tweens a public property or field on an object.
/// </summary>
///
/// <remarks>
/// A property tweener captures the initial value when it first starts, computes
/// a supported value delta and writes eased values until its duration
/// completes.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Configure and process it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Tween.TweenProperty"/>
public sealed class PropertyTweener : Tweener
{
    private readonly ElectronObject _target;
    private readonly string[] _propertyNames;
    private readonly Variant _finalValue;
    private readonly double _duration;
    private Tween.TransitionType _transition;
    private Tween.EaseType _ease;
    private Variant _initialValue;
    private Variant _deltaValue;
    private double _elapsed;
    private bool _started;
    private bool _supported;

    internal PropertyTweener(
        Tween owner,
        ElectronObject target,
        string[] propertyNames,
        Variant finalValue,
        double duration,
        Tween.TransitionType transition,
        Tween.EaseType ease)
        : base(owner)
    {
        _target = target;
        _propertyNames = propertyNames;
        _finalValue = finalValue;
        _duration = duration;
        _transition = transition;
        _ease = ease;
    }

    /// <summary>
    /// Sets a delay before this property tweener starts.
    /// </summary>
    ///
    /// <param name="delay">The finite non-negative delay in seconds.</param>
    ///
    /// <returns>This property tweener.</returns>
    ///
    /// <remarks>
    /// Delay time is consumed before the initial property value is captured.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="delay"/> is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="PropertyTweener" />
    ///
    public PropertyTweener SetDelay(double delay)
    {
        SetDelayCore(delay);
        return this;
    }

    /// <summary>
    /// Sets the transition curve for this property tweener.
    /// </summary>
    ///
    /// <param name="trans">The transition curve to use.</param>
    ///
    /// <returns>This property tweener.</returns>
    ///
    /// <remarks>
    /// This setting overrides the default transition copied from the owner
    /// tween when the property tweener was created.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="trans"/> is outside the supported enum range.
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
    /// <seealso cref="SetEase"/>
    public PropertyTweener SetTrans(Tween.TransitionType trans)
    {
        Tween.ValidateTransition(trans);
        _transition = trans;
        return this;
    }

    /// <summary>
    /// Sets the ease mode for this property tweener.
    /// </summary>
    ///
    /// <param name="ease">The ease mode to use.</param>
    ///
    /// <returns>This property tweener.</returns>
    ///
    /// <remarks>
    /// This setting overrides the default ease mode copied from the owner tween
    /// when the property tweener was created.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ease"/> is outside the supported enum range.
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
    /// <seealso cref="SetTrans"/>
    public PropertyTweener SetEase(Tween.EaseType ease)
    {
        Tween.ValidateEase(ease);
        _ease = ease;
        return this;
    }

    internal override TweenStepResult StepCore(double delta)
    {
        if (!StartIfNeeded())
        {
            return TweenStepResult.Complete(delta);
        }

        if (_duration <= 0d)
        {
            Tween.TrySetMemberPath(_target, _propertyNames, 0, _finalValue);
            return TweenStepResult.Complete(delta);
        }

        var remainingDuration = _duration - _elapsed;
        var consumed = Math.Min(delta, remainingDuration);
        _elapsed += consumed;

        var value = _supported
            ? Tween.InterpolateValue(_initialValue, _deltaValue, _elapsed, _duration, _transition, _ease)
            : _initialValue;
        Tween.TrySetMemberPath(_target, _propertyNames, 0, value);

        if (_elapsed + Tween.Epsilon < _duration)
        {
            return TweenStepResult.Incomplete();
        }

        Tween.TrySetMemberPath(_target, _propertyNames, 0, _finalValue);
        return TweenStepResult.Complete(delta - consumed);
    }

    internal override void ResetCore()
    {
        _elapsed = 0d;
    }

    private bool StartIfNeeded()
    {
        if (_started)
        {
            return ElectronObject.IsInstanceValid(_target);
        }

        _started = true;
        if (!ElectronObject.IsInstanceValid(_target) ||
            !Tween.TryGetMemberPath(_target, _propertyNames, 0, out _initialValue))
        {
            return false;
        }

        _supported = Tween.TryCreateDelta(_initialValue, _finalValue, out _deltaValue);
        return true;
    }
}

/// <summary>
/// Calls a <see cref="Callable"/> when a tween sequence reaches this step.
/// </summary>
///
/// <remarks>
/// The callback is invoked once per tween run. Calling <see cref="Tween.Stop"/>
/// rewinds callback tweeners so they can be invoked again after
/// <see cref="Tween.Play"/>.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Configure and process it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Tween.TweenCallback"/>
public sealed class CallbackTweener : Tweener
{
    private readonly Callable _callback;
    private bool _called;

    internal CallbackTweener(Tween owner, Callable callback)
        : base(owner)
    {
        _callback = callback;
    }

    /// <summary>
    /// Sets a delay before this callback tweener runs.
    /// </summary>
    ///
    /// <param name="delay">The finite non-negative delay in seconds.</param>
    ///
    /// <returns>This callback tweener.</returns>
    ///
    /// <remarks>
    /// The callback is invoked after the delay has elapsed and the sequence
    /// reaches this step.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="delay"/> is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="CallbackTweener" />
    ///
    public CallbackTweener SetDelay(double delay)
    {
        SetDelayCore(delay);
        return this;
    }

    internal override TweenStepResult StepCore(double delta)
    {
        if (!_called)
        {
            _called = true;
            if (_callback.TryCall(Array.Empty<object?>(), out _, out var exception) != Error.Ok &&
                exception is not null)
            {
                Owner.ReportCallbackFailure(_callback, exception);
            }
        }

        return TweenStepResult.Complete(delta);
    }

    internal override void ResetCore()
    {
        _called = false;
    }
}

/// <summary>
/// Consumes time inside a tween sequence without changing objects.
/// </summary>
///
/// <remarks>
/// Interval tweeners are created with <see cref="Tween.TweenInterval"/> and
/// complete after their configured duration has elapsed.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Process it on the main scene thread through
/// its owner tween.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Tween.TweenInterval"/>
public sealed class IntervalTweener : Tweener
{
    private readonly double _duration;
    private double _elapsed;

    internal IntervalTweener(Tween owner, double duration)
        : base(owner)
    {
        _duration = duration;
    }

    internal override TweenStepResult StepCore(double delta)
    {
        if (_duration <= 0d)
        {
            return TweenStepResult.Complete(delta);
        }

        var remainingDuration = _duration - _elapsed;
        var consumed = Math.Min(delta, remainingDuration);
        _elapsed += consumed;
        return _elapsed + Tween.Epsilon < _duration
            ? TweenStepResult.Incomplete()
            : TweenStepResult.Complete(delta - consumed);
    }

    internal override void ResetCore()
    {
        _elapsed = 0d;
    }
}

internal readonly record struct TweenStepResult(bool Completed, double RemainingDelta)
{
    public static TweenStepResult Complete(double remainingDelta)
    {
        return new TweenStepResult(true, Math.Max(0d, remainingDelta));
    }

    public static TweenStepResult Incomplete()
    {
        return new TweenStepResult(false, 0d);
    }
}
