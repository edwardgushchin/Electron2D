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
/// Displays and advances a texture-frame animation from a
/// <see cref="SpriteFrames"/> resource.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="AnimatedSprite2D"/> is a 2D canvas node for simple frame-based
/// animations. It reads frame textures from <see cref="SpriteFrames"/>, updates
/// playback state during <see cref="Node._Process"/>, and submits the current
/// frame to the canvas renderer.
/// </para>
/// <para>
/// The node does not inherit from <see cref="Sprite2D"/> because its public API
/// intentionally omits direct texture and region editing properties.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nodes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="SpriteFrames"/>
/// <seealso cref="Node2D"/>
public class AnimatedSprite2D : Node2D
{
    private StringName animation = "default";
    private int frame;
    private float frameProgress;
    private float customSpeed = 1f;
    private int pingpongDirection = 1;
    private bool playing;

    /// <summary>
    /// Creates a new animated sprite node.
    /// </summary>
    ///
    /// <remarks>
    /// The node starts on the <c>default</c> animation, frame <c>0</c>, with
    /// playback stopped.
    /// </remarks>
    ///
    /// <threadsafety>
    /// Construction is safe on any thread when the node is not shared until
    /// after construction completes.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    public AnimatedSprite2D()
    {
    }

    /// <summary>
    /// Gets or sets the current animation name.
    /// </summary>
    ///
    /// <value>
    /// The animation name inside <see cref="SpriteFrames"/>. The default is
    /// <c>default</c>.
    /// </value>
    ///
    /// <remarks>
    /// Changing this property resets <see cref="Frame"/> and
    /// <see cref="FrameProgress"/> to the beginning of the animation.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    public StringName Animation
    {
        get
        {
            ThrowIfFreed();
            return animation;
        }
        set
        {
            ThrowIfFreed();
            if (animation == value)
            {
                return;
            }

            animation = value;
            frame = 0;
            frameProgress = 0f;
            pingpongDirection = 1;
        }
    }

    /// <summary>
    /// Gets or sets the animation that starts automatically when the node
    /// becomes ready.
    /// </summary>
    ///
    /// <value>
    /// The animation name to pass to <see cref="Play"/> from
    /// <see cref="Node._Ready"/>, or an empty string to disable autoplay.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    public string Autoplay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the current frame is drawn centered on the node
    /// origin.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to center the frame; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRect"/>
    public bool Centered { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the current frame is flipped horizontally.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to flip the frame horizontally; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool FlipH { get; set; }

    /// <summary>
    /// Gets or sets whether the current frame is flipped vertically.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to flip the frame vertically; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool FlipV { get; set; }

    /// <summary>
    /// Gets or sets the current frame index.
    /// </summary>
    ///
    /// <value>
    /// The zero-based frame index for <see cref="Animation"/>.
    /// </value>
    ///
    /// <remarks>
    /// Setting this property resets <see cref="FrameProgress"/> to
    /// <c>0</c>. Use <see cref="SetFrameAndProgress"/> to preserve a progress
    /// value.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned frame is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FrameProgress"/>
    public int Frame
    {
        get
        {
            ThrowIfFreed();
            return frame;
        }
        set
        {
            ThrowIfFreed();
            SetFrameInternal(value, 0f);
        }
    }

    /// <summary>
    /// Gets or sets the normalized progress through the current frame.
    /// </summary>
    ///
    /// <value>
    /// A value from <c>0</c> to <c>1</c>. Forward playback moves toward
    /// <c>1</c>; reverse playback moves toward <c>0</c>.
    /// </value>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned progress is not finite or outside
    /// <c>0..1</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetFrameAndProgress"/>
    public float FrameProgress
    {
        get
        {
            ThrowIfFreed();
            return frameProgress;
        }
        set
        {
            ThrowIfFreed();
            ValidateProgress(value);
            frameProgress = value;
        }
    }

    /// <summary>
    /// Gets or sets the local drawing offset.
    /// </summary>
    ///
    /// <value>
    /// The offset applied before the centered or top-left frame rectangle is
    /// calculated.
    /// </value>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRect"/>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    ///
    /// <value>
    /// The multiplier applied to the custom speed from <see cref="Play"/>.
    /// Negative values play in reverse. A value of <c>0</c> keeps playback
    /// active but stops frame advancement.
    /// </value>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned speed is not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetPlayingSpeed"/>
    public float SpeedScale
    {
        get
        {
            ThrowIfFreed();
            return speedScale;
        }
        set
        {
            ThrowIfFreed();
            if (!Mathf.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "SpeedScale must be finite.");
            }

            speedScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the sprite-frame library used by this node.
    /// </summary>
    ///
    /// <value>
    /// The assigned <see cref="SpriteFrames"/> resource, or <c>null</c> when
    /// the node has no animation data.
    /// </value>
    ///
    /// <remarks>
    /// Replacing or mutating this resource affects the next process/submission
    /// pass without recreating the node.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SpriteFrames"/>
    public SpriteFrames? SpriteFrames { get; set; }

    private float speedScale = 1f;

    /// <summary>
    /// Gets the current playing speed.
    /// </summary>
    ///
    /// <returns>
    /// <see cref="SpeedScale"/> multiplied by the custom speed supplied to
    /// <see cref="Play"/> when playback is active; otherwise, <c>0</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="IsPlaying"/>
    public float GetPlayingSpeed()
    {
        ThrowIfFreed();
        return playing ? speedScale * customSpeed * pingpongDirection : 0f;
    }

    /// <summary>
    /// Gets the local rectangle occupied by the current frame.
    /// </summary>
    ///
    /// <returns>
    /// The frame rectangle in local coordinates, or an empty rectangle when no
    /// frame texture is available.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rect2 GetRect()
    {
        ThrowIfFreed();
        var size = GetDrawSize();
        var position = Centered ? Offset - (size / 2f) : Offset;
        return new Rect2(position, size);
    }

    /// <summary>
    /// Checks whether a local sprite position maps to an opaque pixel.
    /// </summary>
    ///
    /// <param name="position">The local position to query.</param>
    ///
    /// <returns>
    /// <c>true</c> if the current frame has an opaque pixel at
    /// <paramref name="position"/>; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRect"/>
    public bool IsPixelOpaque(Vector2 position)
    {
        ThrowIfFreed();
        var texture = GetCurrentTexture();
        if (texture is null)
        {
            return false;
        }

        var rect = GetRect();
        if (!rect.HasPoint(position))
        {
            return false;
        }

        var local = position - rect.Position;
        var x = (int)MathF.Floor(local.X);
        var y = (int)MathF.Floor(local.Y);
        return texture.IsPixelOpaque(x, y);
    }

    /// <summary>
    /// Gets whether playback is active.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when playback has been started and not paused or stopped;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method can return <c>true</c> even when <see cref="SpeedScale"/> or
    /// the custom speed from <see cref="Play"/> is <c>0</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    /// <seealso cref="Pause"/>
    public bool IsPlaying()
    {
        ThrowIfFreed();
        return playing;
    }

    /// <summary>
    /// Pauses playback while preserving frame position.
    /// </summary>
    ///
    /// <remarks>
    /// Calling <see cref="Play"/> without a new animation name resumes from the
    /// current frame and progress.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    /// <seealso cref="Stop"/>
    public void Pause()
    {
        ThrowIfFreed();
        playing = false;
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    ///
    /// <param name="name">
    /// The animation to play, or an empty value to use the current
    /// <see cref="Animation"/>.
    /// </param>
    /// <param name="customSpeed">
    /// The finite custom speed multiplier for this playback request.
    /// </param>
    /// <param name="fromEnd">
    /// <c>true</c> to begin at the last frame of the selected animation.
    /// </param>
    ///
    /// <remarks>
    /// If the selected animation is missing or has no frames, playback remains
    /// stopped.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="customSpeed"/> is not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="PlayBackwards"/>
    /// <seealso cref="Pause"/>
    public void Play(StringName name = default, float customSpeed = 1f, bool fromEnd = false)
    {
        ThrowIfFreed();
        if (!Mathf.IsFinite(customSpeed))
        {
            throw new ArgumentOutOfRangeException(nameof(customSpeed), customSpeed, "Custom speed must be finite.");
        }

        var requestedAnimation = name.IsEmpty() ? animation : name;
        if (requestedAnimation != animation)
        {
            Animation = requestedAnimation;
        }

        this.customSpeed = customSpeed;
        pingpongDirection = 1;

        if (!TryGetFrameCount(out var frameCount) || frameCount == 0)
        {
            playing = false;
            return;
        }

        if (fromEnd)
        {
            frame = frameCount - 1;
            frameProgress = 1f;
        }
        else if (frame >= frameCount)
        {
            frame = 0;
            frameProgress = 0f;
        }

        playing = true;
    }

    /// <summary>
    /// Starts reverse playback.
    /// </summary>
    ///
    /// <param name="name">
    /// The animation to play backwards, or an empty value to use the current
    /// <see cref="Animation"/>.
    /// </param>
    ///
    /// <remarks>
    /// This method is equivalent to calling <see cref="Play"/> with a custom
    /// speed of <c>-1</c> and <c>fromEnd</c> set to <c>true</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Play"/>
    public void PlayBackwards(StringName name = default)
    {
        Play(name, customSpeed: -1f, fromEnd: true);
    }

    /// <summary>
    /// Sets the current frame and progress together.
    /// </summary>
    ///
    /// <param name="frame">The non-negative frame index.</param>
    /// <param name="progress">The normalized progress from <c>0</c> to <c>1</c>.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frame"/> is negative or
    /// <paramref name="progress"/> is not finite or outside <c>0..1</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Frame"/>
    /// <seealso cref="FrameProgress"/>
    public void SetFrameAndProgress(int frame, float progress)
    {
        ThrowIfFreed();
        SetFrameInternal(frame, progress);
    }

    /// <summary>
    /// Stops playback and resets playback state.
    /// </summary>
    ///
    /// <remarks>
    /// This resets <see cref="Frame"/> and <see cref="FrameProgress"/> to
    /// <c>0</c>, and resets the custom speed used by <see cref="Play"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Pause"/>
    public void Stop()
    {
        ThrowIfFreed();
        playing = false;
        customSpeed = 1f;
        pingpongDirection = 1;
        frame = 0;
        frameProgress = 0f;
    }

    /// <summary>
    /// Starts autoplay when the node becomes ready.
    /// </summary>
    ///
    /// <remarks>
    /// If <see cref="Autoplay"/> is not empty, this callback calls
    /// <see cref="Play"/> with that animation name after the base ready
    /// callback has run.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread during scene-tree
    /// traversal.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Autoplay"/>
    /// <seealso cref="Play"/>
    public override void _Ready()
    {
        base._Ready();
        if (!string.IsNullOrEmpty(Autoplay))
        {
            Play(Autoplay);
        }
    }

    /// <summary>
    /// Advances playback during the process step.
    /// </summary>
    ///
    /// <param name="delta">The elapsed process time in seconds.</param>
    ///
    /// <remarks>
    /// The current frame is advanced before the scene draw traversal submits
    /// canvas commands for the frame.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread during scene-tree
    /// traversal.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Frame"/>
    /// <seealso cref="FrameProgress"/>
    public override void _Process(double delta)
    {
        base._Process(delta);
        Advance(delta);
    }

    internal Texture2D? GetCurrentTexture()
    {
        return SpriteFrames is not null && SpriteFrames.TryGetFrame(animation, frame, out var texture, out _)
            ? texture
            : null;
    }

    internal Rect2 GetSourceRect()
    {
        var texture = GetCurrentTexture();
        return texture is null ? new Rect2() : new Rect2(0f, 0f, texture.GetWidth(), texture.GetHeight());
    }

    private void Advance(double delta)
    {
        if (!playing || delta <= 0d || !double.IsFinite(delta) || SpriteFrames is null)
        {
            return;
        }

        var playingSpeed = GetPlayingSpeed();
        if (Mathf.IsZeroApprox(playingSpeed) || !TryGetFrameCount(out var frameCount) || frameCount == 0)
        {
            return;
        }

        var relativeUnits = (float)(delta * Math.Abs(playingSpeed) * SpriteFrames.GetAnimationSpeed(animation));
        var direction = playingSpeed >= 0f ? 1 : -1;
        while (relativeUnits > 0f && playing)
        {
            if (!SpriteFrames.TryGetFrame(animation, frame, out _, out var duration))
            {
                playing = false;
                return;
            }

            var remainingInFrame = direction > 0
                ? duration * (1f - frameProgress)
                : duration * frameProgress;

            if (remainingInFrame > relativeUnits)
            {
                frameProgress += direction * (relativeUnits / duration);
                frameProgress = Math.Clamp(frameProgress, 0f, 1f);
                return;
            }

            relativeUnits -= remainingInFrame;
            StepFrame(direction, frameCount);
        }
    }

    private void StepFrame(int direction, int frameCount)
    {
        if (frameCount <= 0)
        {
            playing = false;
            return;
        }

        var nextFrame = frame + direction;
        if (nextFrame >= 0 && nextFrame < frameCount)
        {
            frame = nextFrame;
            frameProgress = direction > 0 ? 0f : 1f;
            if (IsAtNonLoopingEnd(direction, frameCount))
            {
                playing = false;
            }

            return;
        }

        var loopMode = SpriteFrames?.GetAnimationLoopMode(animation) ?? SpriteFrames.LoopModeEnum.None;
        switch (loopMode)
        {
            case SpriteFrames.LoopModeEnum.Linear:
                frame = direction > 0 ? 0 : frameCount - 1;
                frameProgress = direction > 0 ? 0f : 1f;
                break;

            case SpriteFrames.LoopModeEnum.Pingpong:
                if (frameCount == 1)
                {
                    frame = 0;
                    frameProgress = direction > 0 ? 0f : 1f;
                    break;
                }

                pingpongDirection *= -1;
                frame = direction > 0 ? frameCount - 2 : 1;
                frameProgress = direction > 0 ? 1f : 0f;
                break;

            default:
                frame = direction > 0 ? frameCount - 1 : 0;
                frameProgress = direction > 0 ? 1f : 0f;
                playing = false;
                break;
        }
    }

    private bool IsAtNonLoopingEnd(int direction, int frameCount)
    {
        if (SpriteFrames?.GetAnimationLoopMode(animation) != SpriteFrames.LoopModeEnum.None)
        {
            return false;
        }

        return direction > 0 ? frame == frameCount - 1 : frame == 0;
    }

    private Vector2 GetDrawSize()
    {
        var texture = GetCurrentTexture();
        return texture is null ? Vector2.Zero : new Vector2(texture.GetWidth(), texture.GetHeight());
    }

    private bool TryGetFrameCount(out int frameCount)
    {
        frameCount = 0;
        if (SpriteFrames is null || animation.IsEmpty() || !SpriteFrames.HasAnimation(animation))
        {
            return false;
        }

        frameCount = SpriteFrames.GetFrameCount(animation);
        return true;
    }

    private void SetFrameInternal(int newFrame, float newProgress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newFrame);
        ValidateProgress(newProgress);
        frame = newFrame;
        frameProgress = newProgress;
    }

    private static void ValidateProgress(float progress)
    {
        if (!Mathf.IsFinite(progress) || progress < 0f || progress > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Frame progress must be finite and between 0 and 1.");
        }
    }
}
