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
/// Plays an <see cref="AudioStream" /> as a sound source in 2D space.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="AudioStreamPlayer2D" /> uses the node's <see cref="Node2D.GlobalPosition" />
/// to calculate distance attenuation and stereo panning before starting an
/// internal voice.
/// </para>
/// <para>
/// Electron2D 0.1-preview uses the origin of the 2D world as the listener
/// position. Dedicated listener nodes and area-based routing are future audio
/// tasks.
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
/// <seealso cref="AudioStreamPlayer" />
/// <seealso cref="Node2D" />
public class AudioStreamPlayer2D : Node2D, ISceneTreeLifecycleHandler
{
    private const string FinishedSignal = "finished";
    private readonly AudioStreamPlaybackController playback;
    private int areaMask;
    private float attenuation = 1f;
    private float maxDistance = 2000f;
    private float panningStrength = 1f;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamPlayer2D" /> class.
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
    public AudioStreamPlayer2D()
    {
        playback = new AudioStreamPlaybackController(ApplySpatialPlayback, () => EmitSignal(FinishedSignal));
        AddUserSignal(FinishedSignal);
    }

    /// <summary>
    /// Gets or sets which 2D area layers may affect this audio source.
    /// </summary>
    ///
    /// <value>
    /// A non-negative bit mask. The default value is <c>0</c>.
    /// </value>
    ///
    /// <remarks>
    /// Area-based audio routing is not active in 0.1-preview. The value is
    /// stored for scene data compatibility with the selected public API subset.
    /// </remarks>
    ///
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
    /// <seealso cref="AudioStreamPlayer2D" />
    public int AreaMask
    {
        get
        {
            ThrowIfFreed();
            return areaMask;
        }
        set
        {
            ThrowIfFreed();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Area mask must be non-negative.");
            }

            areaMask = value;
        }
    }

    /// <summary>
    /// Gets or sets the distance attenuation exponent.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative exponent. The default value is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// Higher values make the sound fade faster as distance approaches
    /// <see cref="MaxDistance" />.
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
    /// <seealso cref="MaxDistance" />
    public float Attenuation
    {
        get
        {
            ThrowIfFreed();
            return attenuation;
        }
        set
        {
            ThrowIfFreed();
            ValidateFiniteNonNegative(value, nameof(value), "Attenuation");
            attenuation = value;
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
    /// <see cref="AudioServer" /> resolves this name when playback starts. If
    /// the name does not match an existing bus, the voice falls back to
    /// <c>Master</c>.
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
    /// Gets or sets the maximum distance from which this source is still audible.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative distance in 2D units. The default value is
    /// <c>2000</c>.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>0</c> disables distance attenuation and panning for this
    /// preview implementation.
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
    /// <seealso cref="Attenuation" />
    /// <seealso cref="PanningStrength" />
    public float MaxDistance
    {
        get
        {
            ThrowIfFreed();
            return maxDistance;
        }
        set
        {
            ThrowIfFreed();
            ValidateFiniteNonNegative(value, nameof(value), "Max distance");
            maxDistance = value;
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
    /// Gets or sets the strength of left/right panning.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative panning multiplier. The default value is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// The computed pan is clamped to <c>-1</c> through <c>1</c> before being
    /// sent to the internal voice.
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
    /// <seealso cref="MaxDistance" />
    public float PanningStrength
    {
        get
        {
            ThrowIfFreed();
            return panningStrength;
        }
        set
        {
            ThrowIfFreed();
            ValidateFiniteNonNegative(value, nameof(value), "Panning strength");
            panningStrength = value;
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
    /// A value of <c>1</c> plays the stream at its original pitch.
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
    /// Gets or sets the stream resource played by this node.
    /// </summary>
    ///
    /// <value>
    /// The stream resource to play, or <c>null</c> when no stream is assigned.
    /// </value>
    ///
    /// <remarks>
    /// Setting this property stops all voices currently owned by this player.
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
    /// Gets or sets the playback volume in decibels before 2D attenuation.
    /// </summary>
    ///
    /// <value>
    /// A finite decibel offset applied to the stream.
    /// </value>
    ///
    /// <remarks>
    /// The value is combined with distance attenuation when <see cref="Play(float)" />
    /// starts a voice.
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
    /// Gets or sets the playback volume as linear gain before 2D attenuation.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative linear gain. The default value is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// Setting this property updates <see cref="VolumeDb" />.
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
    /// <c>true</c>.
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
    /// Calling this method creates a new 2D voice when <see cref="Stream" /> is
    /// assigned. The voice receives volume attenuation and pan calculated from
    /// the node position.
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
    /// voice from <paramref name="toPosition" />.
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
    /// Releases voices owned by this player before object lifetime ends.
    /// </summary>
    ///
    /// <remarks>
    /// This override stops all active playback for this node, then delegates to
    /// the base 2D node lifetime cleanup.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called by the object lifetime
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

    private AudioVoicePlayback ApplySpatialPlayback(AudioVoicePlayback basePlayback)
    {
        if (maxDistance <= 0f)
        {
            return new AudioVoicePlayback(
                VolumeDb: basePlayback.VolumeDb,
                PitchScale: basePlayback.PitchScale,
                Loop: basePlayback.Loop,
                StartPosition: basePlayback.StartPosition,
                Pan: 0f,
                Bus: basePlayback.Bus);
        }

        var relative = GlobalPosition - Vector2.Zero;
        var distance = relative.Length();
        var normalized = Mathf.Clamp(1f - (distance / maxDistance), 0f, 1f);
        var gain = attenuation == 0f ? (normalized > 0f ? 1f : 0f) : MathF.Pow(normalized, attenuation);
        var volumeDb = basePlayback.VolumeDb + LinearToDb(gain);
        var pan = Mathf.Clamp((relative.X / maxDistance) * panningStrength, -1f, 1f);

        return new AudioVoicePlayback(
            VolumeDb: volumeDb,
            PitchScale: basePlayback.PitchScale,
            Loop: basePlayback.Loop,
            StartPosition: basePlayback.StartPosition,
            Pan: pan,
            Bus: basePlayback.Bus);
    }

    private static float LinearToDb(float linear)
    {
        return linear <= 0f ? -80f : 20f * MathF.Log10(linear);
    }

    private static void ValidateFiniteNonNegative(float value, string parameterName, string label)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentException($"{label} must be finite.", parameterName);
        }

        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{label} must be non-negative.");
        }
    }
}
