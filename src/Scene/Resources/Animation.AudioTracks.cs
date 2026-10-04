namespace Electron2D;

/// <summary>Borrowed audio source and finite start/end trim offsets in seconds.</summary>
public readonly struct AnimationAudioKey
{
    /// <summary>Creates typed audio key data, normalized when accepted by a track.</summary>
    /// <param name="stream">Borrowed source or null for an empty cue.</param><param name="startOffset">Finite start trim.</param><param name="endOffset">Finite end trim.</param>
    public AnimationAudioKey(AudioStream? stream, double startOffset = 0, double endOffset = 0) { Stream = stream; StartOffset = startOffset; EndOffset = endOffset; }
    /// <summary>Gets the borrowed source.</summary><value>The supplied resource or null.</value>
    public AudioStream? Stream { get; init; }
    /// <summary>Gets the start trim.</summary><value>Nonnegative seconds after track normalization.</value>
    public double StartOffset { get; init; }
    /// <summary>Gets the end trim.</summary><value>Nonnegative seconds after track normalization.</value>
    public double EndOffset { get; init; }
}
public sealed partial class Animation
{
    /// <summary>Adds a node-only audio cue track targeting a player or spatial emitter.</summary>
    /// <param name="atPosition">Insertion index or minus one to append.</param><returns>The inserted index.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    public int AddAudioTrack(int atPosition = -1) => InsertTrack(new AnimationAudioTrack(), atPosition);
    private AnimationAudioTrack Audio(int track) => Get(track) as AnimationAudioTrack ?? throw new InvalidOperationException("Operation requires an audio track.");
    /// <summary>Inserts or replaces a typed audio key, clamping negative trims to zero.</summary>
    /// <param name="track">Existing audio track.</param><param name="time">Finite seconds.</param><param name="stream">Borrowed source or null.</param><param name="startOffset">Finite start trim.</param><param name="endOffset">Finite end trim.</param><returns>Sorted key index.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public int AudioTrackInsertKey(int track, double time, AudioStream? stream, double startOffset = 0, double endOffset = 0) { _ = Audio(track); return TrackInsertKey(track, time, new AnimationAudioKey(stream, startOffset, endOffset)); }
    /// <summary>Returns a key's borrowed source.</summary><param name="track">Track index.</param><param name="key">Key index.</param><returns>Source or null.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public AudioStream? AudioTrackGetKeyStream(int track, int key) => Audio(track).Values[key].Stream;
    /// <summary>Returns start trim seconds.</summary><param name="track">Track index.</param><param name="key">Key index.</param><returns>Nonnegative seconds.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public double AudioTrackGetKeyStartOffset(int track, int key) => Audio(track).Values[key].StartOffset;
    /// <summary>Returns end trim seconds.</summary><param name="track">Track index.</param><param name="key">Key index.</param><returns>Nonnegative seconds.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public double AudioTrackGetKeyEndOffset(int track, int key) => Audio(track).Values[key].EndOffset;
    /// <summary>Replaces a key's borrowed source.</summary><param name="track">Track index.</param><param name="key">Key index.</param><param name="stream">Source or null.</param>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public void AudioTrackSetKeyStream(int track, int key, AudioStream? stream) => TrackSetKeyValue(track, key, Audio(track).Values[key] with { Stream = stream });
    /// <summary>Replaces finite start trim, clamping negative values.</summary><param name="track">Track index.</param><param name="key">Key index.</param><param name="offset">Finite seconds.</param>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public void AudioTrackSetKeyStartOffset(int track, int key, double offset) => TrackSetKeyValue(track, key, Audio(track).Values[key] with { StartOffset = offset });
    /// <summary>Replaces finite end trim, clamping negative values.</summary><param name="track">Track index.</param><param name="key">Key index.</param><param name="offset">Finite seconds.</param>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public void AudioTrackSetKeyEndOffset(int track, int key, double offset) => TrackSetKeyValue(track, key, Audio(track).Values[key] with { EndOffset = offset });
    /// <summary>Returns whether clip weight affects cue volume.</summary><param name="track">Audio track index.</param><returns>True initially.</returns>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public bool AudioTrackIsUseBlend(int track) => Audio(track).UseBlend;
    /// <summary>Sets whether clip weight affects cue volume.</summary><param name="track">Audio track index.</param><param name="enable">Whether to scale gain.</param>
    /// <exception cref="ObjectDisposedException">The animation or a borrowed source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public void AudioTrackSetUseBlend(int track, bool enable) { Audio(track).UseBlend = enable; EmitChanged(); }
}
internal sealed class AnimationAudioTrack : AnimationTypedTrack<AnimationAudioKey>
{
    internal bool UseBlend = true;
    internal AnimationAudioTrack() : base(null) { }
    internal override Animation.TrackType Kind => Animation.TrackType.Audio;
    internal override string PropertyName => "";
    internal override AnimationAudioKey Normalize(AnimationAudioKey value) { Animation.Finite(value.StartOffset); Animation.Finite(value.EndOffset); if (value.Stream is { } stream) ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); return value with { StartOffset = Math.Max(0, value.StartOffset), EndOffset = Math.Max(0, value.EndOffset) }; }
    internal override void ValidateInterpolation(Animation.InterpolationType mode) { }
    internal override AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null) { var copy = new AnimationAudioTrack { UseBlend = UseBlend }; CopyTo(copy, false, null); if (deep) for (var i = 0; i < copy.Values.Count; i++) copy.Values[i] = copy.Values[i] with { Stream = (AudioStream?)duplicate!(copy.Values[i].Stream) }; return copy; }
    internal override AnimationBinding? Bind(Node root)
    { var target = NodePath.Length == 0 ? root : root.GetNodeOrNull(NodePath); return target is AudioStreamPlayer or AudioStreamEmitter ? new Binding(this, target) : null; }
    private sealed class Binding(AnimationAudioTrack track, Node target) : AnimationBinding
    {
        private struct Cue { internal long ID; internal int Key; internal double Elapsed, Duration, Start; internal bool Active; }
        private AnimationAudioTransport? _transport;
        private Cue[] _cues = [];
        private double _weight;
        private AnimationMixFrame _frame;
        private bool _seen;
        internal override void AttachBlend(AnimationMixer mixer)
        {
            _transport = mixer.GetAudioTransport(target); _cues = new Cue[mixer.AudioMaxPolyphony];
            foreach (var key in track.Values) if (key.Stream is { } stream) _transport.Prepare(stream);
            mixer.RegisterAudioBinding(this);
        }
        internal override void BeginFrame() { _weight = 0; _seen = false; }
        internal override void AddWeight(double weight, int pass) { _weight += weight; _transport!.TotalWeight += weight; }
        internal override void SetRest() { }
        internal override AnimationCaptureValue? Capture(Animation animation, int index) => null;
        internal override void StopPlayback() { if (_transport is null) return; foreach (ref var cue in _cues.AsSpan()) if (cue.Active) { _transport.Stop(cue.ID); cue.Active = false; } }
        internal override void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation)
        {
            if (_transport is null || !track.Enabled || target.IsDisposed || target.IsQueuedForDeletion || !ReferenceEquals(target.Tree, mixer.Tree)) return;
            _frame = frame; _seen = true;
            foreach (ref var cue in _cues.AsSpan()) if (cue.Active) cue.Elapsed += Math.Abs(frame.Movement);
            if (frame.UpdateOnly || track.Times.Count == 0) return;
            var seek = !frame.Previous.HasValue || frame.IncludeStart;
            var keyIndex = seek ? frame.Animation.TrackFindKey(index, frame.Time, Animation.FindMode.Nearest, true) : track.FindLastCrossed(frame, index);
            if (keyIndex < 0) return;
            if (seek) foreach (ref var cue in _cues.AsSpan()) if (cue.Active && cue.Key == keyIndex) { _transport.Stop(cue.ID); cue.Active = false; }
            var key = track.Values[keyIndex]; if (key.Stream is null) return;
            var offset = key.StartOffset + (seek ? Math.Max(0, frame.Time - track.Times[keyIndex]) : 0);
            var cueWeight = track.UseBlend ? Math.Max(0, _weight) : 1; if (track.UseBlend && !mixer.Deterministic && _transport.TotalWeight > 1e-5) cueWeight /= _transport.TotalWeight;
            var id = _transport.Play(key.Stream, offset, (float)Mathf.LinearToDB(cueWeight)); if (id == AudioStreamPlaybackPolyphonic.InvalidID) return;
            var length = key.Stream.GetLength(); var duration = length > 0 && key.EndOffset > 1e-5 ? Math.Max(0, length - offset - key.EndOffset) : double.PositiveInfinity;
            for (var i = 0; i < _cues.Length; i++) if (!_cues[i].Active || !_transport.Playing(_cues[i].ID)) { _cues[i] = new() { ID = id, Key = keyIndex, Duration = duration, Start = frame.Time, Active = true }; return; }
            _transport.Stop(id);
        }
        internal override void FinishFrame(AnimationMixer mixer)
        {
            if (_transport is null) return;
            var weight = track.UseBlend ? Math.Max(0, _weight) : 1; if (track.UseBlend && !mixer.Deterministic && _transport.TotalWeight > 1e-5) weight /= _transport.TotalWeight;
            var db = (float)Mathf.LinearToDB(weight);
            foreach (ref var cue in _cues.AsSpan()) if (cue.Active)
                {
                    if (!_transport.Playing(cue.ID) || cue.Duration <= 0 || cue.Elapsed > cue.Duration || _seen && _frame.Animation.LoopMode == SpriteFrames.LoopMode.None && (_frame.Backward ? _frame.Time > cue.Start : _frame.Time < cue.Start) || !_seen && !track.Enabled) { _transport.Stop(cue.ID); cue.Active = false; }
                    else _transport.SetGain(cue.ID, db);
                }
        }
        internal override void Apply(Animation animation, int index, double time, bool backward, double? previous, AnimationMixer mixer, long generation) { AddWeight(1, 0); Mix(new(animation, time, backward, previous, 1, Movement: previous.HasValue ? time - previous.Value : 0, ExternalSeeking: mixer.MethodExternalSeeking, UpdateOnly: mixer.MethodUpdateOnly), index, 1, mixer, generation); }
    }
}
