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
/// Plays an <see cref="AudioStream" /> without positional attenuation.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="AudioStreamPlayer" /> is a scene node for user-interface sounds,
/// menu sounds, music and other playback that should not change with 2D
/// position.
/// </para>
/// <para>
/// The node owns playback state and asks <see cref="AudioServer" /> to create
/// internal voices. Internal voice handles are not part of the public API.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate it on the main scene
/// thread that owns the node.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="AudioStream" />
/// <seealso cref="AudioStreamPlayer2D" />
/// <seealso cref="AudioServer" />
public class AudioStreamPlayer : Node, ISceneTreeLifecycleHandler
{
    private const string FinishedSignal = "finished";
    private readonly AudioStreamPlaybackController playback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamPlayer" /> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new player has no stream assigned, targets the <c>Master</c> bus and
    /// registers the <c>finished</c> signal.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Stream" />
    public AudioStreamPlayer()
    {
        playback = new AudioStreamPlaybackController(finished: () => EmitSignal(FinishedSignal));
        AddUserSignal(FinishedSignal);
    }

    /// <summary>
    /// Gets or sets the stream resource played by this node.
    /// </summary>
    ///
    /// <value>
    /// The stream resource to play, or <c>null</c> when no stream is assigned.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Setting this property stops all voices currently owned by this player.
    /// Calling <see cref="Play(float)" /> while this property is <c>null</c>
    /// does not create a voice.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play(float)" />
    /// <seealso cref="Stop" />
    public AudioStream? Stream
    {
        get
        {
            ThrowIfFreed();
            return playback.Stream;
        }
        set
        {
            ThrowIfFreed();
            playback.Stream = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this node starts playback when it enters a scene tree.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to call <see cref="Play(float)" /> on enter-tree; otherwise
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Autoplay only starts when <see cref="Stream" /> is assigned. Removing the
    /// node from the tree stops voices owned by this player.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play(float)" />
    public bool Autoplay
    {
        get
        {
            ThrowIfFreed();
            return playback.Autoplay;
        }
        set
        {
            ThrowIfFreed();
            playback.Autoplay = value;
        }
    }

    /// <summary>
    /// Gets or sets the target audio bus name.
    /// </summary>
    ///
    /// <value>
    /// The requested bus name. The default value is <c>Master</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="AudioServer" /> resolves this name when playback starts. If
    /// the name does not match an existing bus, the voice falls back to
    /// <c>Master</c>.
    /// </para>
    /// <para>
    /// Assigning an empty <see cref="StringName" /> restores <c>Master</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AudioServer.GetBusName(int)" />
    /// <seealso cref="AudioServer.SetBusName(int, string)" />
    /// <seealso cref="AudioServer.SetBusSend(int, StringName)" />
    public StringName Bus
    {
        get
        {
            ThrowIfFreed();
            return playback.Bus;
        }
        set
        {
            ThrowIfFreed();
            playback.Bus = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of simultaneous voices owned by this node.
    /// </summary>
    ///
    /// <value>
    /// The maximum polyphony count. The default value is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// When the limit is reached, the next <see cref="Play(float)" /> call stops
    /// the oldest voice owned by this player before starting a new one.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than <c>1</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play(float)" />
    public int MaxPolyphony
    {
        get
        {
            ThrowIfFreed();
            return playback.MaxPolyphony;
        }
        set
        {
            ThrowIfFreed();
            playback.MaxPolyphony = value;
        }
    }

    /// <summary>
    /// Gets or sets the playback pitch scale.
    /// </summary>
    ///
    /// <value>
    /// A positive finite multiplier applied to stream pitch and tempo.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>1</c> plays the stream at its original pitch. A value of
    /// <c>2</c> doubles pitch and tempo; <c>0.5</c> halves them.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when the value is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to <c>0</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="VolumeDb" />
    public float PitchScale
    {
        get
        {
            ThrowIfFreed();
            return playback.PitchScale;
        }
        set
        {
            ThrowIfFreed();
            playback.PitchScale = value;
        }
    }

    /// <summary>
    /// Gets or sets whether this player is currently requested to play.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when playback is active or paused; otherwise <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Setting this property to <c>true</c> calls <see cref="Play(float)" />.
    /// Setting it to <c>false</c> calls <see cref="Stop" />.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="StreamPaused" />
    public bool Playing
    {
        get
        {
            ThrowIfFreed();
            return playback.Playing;
        }
        set
        {
            ThrowIfFreed();
            playback.Playing = value;
        }
    }

    /// <summary>
    /// Gets or sets whether playback is paused.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when playback should pause and resume later; otherwise
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Pausing stops active voices but keeps <see cref="Playing" /> true.
    /// Clearing this property resumes from the stored playback position when a
    /// stream is assigned.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Playing" />
    /// <seealso cref="GetPlaybackPosition" />
    public bool StreamPaused
    {
        get
        {
            ThrowIfFreed();
            return playback.StreamPaused;
        }
        set
        {
            ThrowIfFreed();
            playback.StreamPaused = value;
        }
    }

    /// <summary>
    /// Gets or sets the playback volume in decibels.
    /// </summary>
    ///
    /// <value>
    /// A finite decibel offset applied to the stream.
    /// </value>
    ///
    /// <remarks>
    /// Use <see cref="VolumeLinear" /> when the caller works with slider-style
    /// linear gain values instead of decibels.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when the value is not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="VolumeLinear" />
    public float VolumeDb
    {
        get
        {
            ThrowIfFreed();
            return playback.VolumeDb;
        }
        set
        {
            ThrowIfFreed();
            playback.VolumeDb = value;
        }
    }

    /// <summary>
    /// Gets or sets the playback volume as linear gain.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative linear gain. The default value is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// Setting this property updates <see cref="VolumeDb" />. A value of
    /// <c>0</c> maps to a quiet floor used by the preview runtime.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when the value is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Access it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="VolumeDb" />
    public float VolumeLinear
    {
        get
        {
            ThrowIfFreed();
            return playback.VolumeLinear;
        }
        set
        {
            ThrowIfFreed();
            playback.VolumeLinear = value;
        }
    }

    internal IReadOnlyList<AudioVoiceHandle> ActiveVoices => playback.ActiveVoices;

    /// <summary>
    /// Gets the stored playback position.
    /// </summary>
    ///
    /// <returns>
    /// The stored playback position in seconds.
    /// </returns>
    ///
    /// <remarks>
    /// The preview runtime stores the last requested start or seek position.
    /// It does not expose backend clock advancement as public API.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Seek(float)" />
    public float GetPlaybackPosition()
    {
        ThrowIfFreed();
        return playback.GetPlaybackPosition();
    }

    /// <summary>
    /// Checks whether this player currently owns an active stream voice.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when at least one internal voice owned by this player is
    /// active; otherwise <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method returns <c>false</c> while <see cref="StreamPaused" /> is
    /// <c>true</c> because the preview runtime stops the active voice while
    /// preserving the resume position.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Playing" />
    public bool HasStreamPlayback()
    {
        ThrowIfFreed();
        return playback.HasStreamPlayback();
    }

    /// <summary>
    /// Starts playback from a position in the stream.
    /// </summary>
    ///
    /// <param name="fromPosition">
    /// The stream position, in seconds, where playback should begin.
    /// </param>
    ///
    /// <remarks>
    /// Calling this method creates a new voice when <see cref="Stream" /> is
    /// assigned. If <see cref="MaxPolyphony" /> is already reached, the oldest
    /// voice owned by this player is stopped first.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fromPosition" /> is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fromPosition" /> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Stop" />
    /// <seealso cref="Seek(float)" />
    public void Play(float fromPosition = 0f)
    {
        ThrowIfFreed();
        playback.Play(fromPosition);
    }

    /// <summary>
    /// Changes the stored playback position.
    /// </summary>
    ///
    /// <param name="toPosition">
    /// The stream position, in seconds, to seek to.
    /// </param>
    ///
    /// <remarks>
    /// If playback is active and not paused, this method restarts the active
    /// voice from <paramref name="toPosition" />. If playback is paused, the
    /// position is used when playback resumes.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="toPosition" /> is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="toPosition" /> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetPlaybackPosition" />
    public void Seek(float toPosition)
    {
        ThrowIfFreed();
        playback.Seek(toPosition);
    }

    /// <summary>
    /// Stops all voices owned by this player.
    /// </summary>
    ///
    /// <remarks>
    /// The stored playback position is reset to <c>0</c>. Calling
    /// <see cref="Stop" /> does not emit the <c>finished</c> signal.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Play(float)" />
    public void Stop()
    {
        ThrowIfFreed();
        playback.Stop();
    }

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
        if (playback.Autoplay)
        {
            playback.Play(0f);
        }
    }

    void ISceneTreeLifecycleHandler.OnProcess(double delta)
    {
        playback.OnProcess();
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
        playback.Stop();
    }

    /// <summary>
    /// Releases voices owned by this player before ElectronObject lifetime ends.
    /// </summary>
    ///
    /// <remarks>
    /// This override stops all active playback for this node, then delegates to
    /// the base node lifetime cleanup.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called by the ElectronObject lifetime
    /// owner on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Stop" />
    protected override void OnFree()
    {
        playback.Stop();
        base.OnFree();
    }
}
