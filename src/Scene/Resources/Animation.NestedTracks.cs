namespace Electron2D;

public sealed partial class Animation
{
    /// <summary>Adds a timeline controlling a relative AnimationPlayer by named clip keys.</summary>
    /// <param name="atPosition">Insertion index, or minus one to append.</param>
    /// <returns>The inserted track index.</returns>
    /// <exception cref="ObjectDisposedException">The animation is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The insertion index is invalid.</exception>
    public int AddAnimationTrack(int atPosition = -1) => InsertTrack(new AnimationNestedTrack(), atPosition);
    private AnimationNestedTrack Nested(int track) => Get(track) as AnimationNestedTrack ?? throw new InvalidOperationException("Operation requires an animation track.");
    /// <summary>Inserts or replaces a named child-animation key; [stop] stops controlled playback.</summary>
    /// <param name="track">An existing animation track.</param><param name="time">Finite seconds.</param><param name="animation">The exact nonnull clip name or stop sentinel.</param>
    /// <returns>The sorted key index.</returns>
    /// <exception cref="ObjectDisposedException">The animation is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or time is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public int AnimationTrackInsertKey(int track, double time, string animation) { _ = Nested(track); return TrackInsertKey(track, time, animation); }
    /// <summary>Returns the exact child-animation name at a key.</summary>
    /// <param name="track">An existing animation track.</param><param name="key">An existing key index.</param><returns>The clip name or stop sentinel.</returns>
    /// <exception cref="ObjectDisposedException">The animation is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public string AnimationTrackGetKeyAnimation(int track, int key) => Nested(track).Values[key];
    /// <summary>Replaces the exact child-animation name at a key.</summary>
    /// <param name="track">An existing animation track.</param><param name="key">An existing key index.</param><param name="animation">The nonnull clip name or stop sentinel.</param>
    /// <exception cref="ObjectDisposedException">The animation is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs.</exception>
    public void AnimationTrackSetKeyAnimation(int track, int key, string animation) { _ = Nested(track); TrackSetKeyValue(track, key, animation); }
}
internal sealed class AnimationNestedTrack : AnimationTypedTrack<string>
{
    internal AnimationNestedTrack() : base(null) { }
    internal override Animation.TrackType Kind => Animation.TrackType.Animation;
    internal override string PropertyName => "";
    internal override string Normalize(string value) { ArgumentNullException.ThrowIfNull(value); return value; }
    internal override void ValidateInterpolation(Animation.InterpolationType mode) { }
    internal override AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null) { var copy = new AnimationNestedTrack(); CopyTo(copy, deep, duplicate); return copy; }
    internal override AnimationBinding? Bind(Node root) => (NodePath.Length == 0 ? root : root.GetNodeOrNull(NodePath)) is AnimationPlayer player ? new Binding(this, player) : null;
    private sealed class Binding(AnimationNestedTrack track, AnimationPlayer player) : AnimationBinding
    {
        private bool _playing;
        private long _controlRevision;
        private readonly SceneTree? _tree = player.Tree;
        internal override void AttachBlend(AnimationMixer mixer) => mixer.RegisterNested(this);
        internal override void AddWeight(double weight, int pass) { }
        internal override void SetRest() { }
        internal override AnimationCaptureValue? Capture(Animation animation, int index) => null;
        private bool OwnsPlayback => _playing && !player.IsDisposed && !player.IsQueuedForDeletion && ReferenceEquals(player.Tree, _tree) && _controlRevision == player.PlaybackRevision;
        internal override void StopPlayback() { var owned = OwnsPlayback; _playing = false; if (owned) player.Stop(true); }
        private void Start(string name)
        { _playing = true; _controlRevision = player.PlaybackRevision + 1; player.Play(name); }
        private int Range(double from, double to, bool reverse)
        {
            var index = track.Times.BinarySearch(to);
            if (reverse) { if (index < 0) index = ~index; return index < track.Times.Count && track.Times[index] < from ? index : -1; }
            if (index < 0) index = ~index - 1;
            return index >= 0 && track.Times[index] > from ? index : -1;
        }
        private int LastCrossed(AnimationMixFrame frame, int index)
        {
            var clip = frame.Animation; var cursor = frame.Previous!.Value; var found = frame.IncludeStart ? clip.TrackFindKey(index, cursor, Animation.FindMode.Exact, true) : -1;
            var remaining = frame.Movement; var start = frame.Start; var end = frame.End < 0 ? clip.Length : frame.End;
            if (end <= start) return found;
            cursor = Math.Clamp(cursor, start, end);
            while (remaining != 0)
            {
                var reverse = remaining < 0; var endpoint = reverse ? start : end; var travel = Math.Min(Math.Abs(remaining), Math.Abs(endpoint - cursor)); var next = cursor + (reverse ? -travel : travel);
                var key = Range(cursor, next, reverse); if (key >= 0) found = key;
                var rest = remaining + (reverse ? travel : -travel); if (travel > 0 && rest == remaining) throw new InvalidOperationException("Nested timeline delta cannot make representable progress."); remaining = rest; cursor = next;
                if (cursor != endpoint || clip.LoopMode == SpriteFrames.LoopMode.None) break;
                if (clip.LoopMode == SpriteFrames.LoopMode.PingPong) remaining = -remaining;
                else { cursor = reverse ? end : start; key = clip.TrackFindKey(index, cursor, Animation.FindMode.Exact, true); if (key >= 0) found = key; }
            }
            return found;
        }
        internal override void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation)
        {
            if (!track.Enabled || track.Times.Count == 0 || Math.Abs(weight) < 1e-5 || player.IsDisposed || player.IsQueuedForDeletion || !ReferenceEquals(player.Tree, mixer.Tree)) return;
            var seek = !frame.Previous.HasValue || frame.IncludeStart;
            var key = seek ? frame.Animation.TrackFindKey(index, frame.Time, Animation.FindMode.Nearest, true) : LastCrossed(frame, index);
            if (key < 0) return;
            var name = track.Values[key];
            if (name == "[stop]" || !player.HasAnimation(name)) { if (!seek && OwnsPlayback) { _playing = false; player.Stop(); } return; }
            if (ReferenceEquals(player, mixer) || player.IsEvaluating) throw new InvalidOperationException("Nested animation cannot control an evaluating player.");
            var revision = frame.Animation.ChangeRevision;
            if (!seek) { Start(name); return; }
            var animation = player.GetAnimation(name); var offset = frame.Time - track.Times[key]; var length = animation.Length;
            if (animation.LoopMode == SpriteFrames.LoopMode.Linear) offset = Mathf.PosMod(offset, length);
            else if (animation.LoopMode == SpriteFrames.LoopMode.PingPong) { var phase = Mathf.PosMod(offset, length * 2); offset = phase <= length ? phase : length * 2 - phase; }
            else { if (!frame.ExternalSeeking && offset >= length) return; offset = Math.Clamp(offset, 0, length); }
            var owned = OwnsPlayback;
            if (frame.UpdateOnly || frame.ExternalSeeking && !player.IsPlaying())
            {
                var expected = player.PlaybackRevision + 1; if (owned) _controlRevision = expected;
                player.AssignedAnimation = name;
                if (!mixer.IsBindingCurrent(frame.Animation, generation) || frame.Animation.IsDisposed || revision != frame.Animation.ChangeRevision || player.IsDisposed || expected != player.PlaybackRevision) return;
                if (owned) _controlRevision = player.PlaybackRevision + 1;
                player.Seek(offset, true, frame.UpdateOnly);
            }
            else
            {
                Start(name);
                if (!mixer.IsBindingCurrent(frame.Animation, generation) || frame.Animation.IsDisposed || revision != frame.Animation.ChangeRevision || player.IsDisposed || !_playing || _controlRevision != player.PlaybackRevision) return;
                _controlRevision = player.PlaybackRevision + 1; player.Seek(offset, false, frame.UpdateOnly);
            }
        }
        internal override void Apply(Animation animation, int index, double time, bool backward, double? previous, AnimationMixer mixer, long generation) => Mix(new(animation, time, backward, previous, 1, Movement: previous.HasValue ? time - previous.Value : 0, ExternalSeeking: mixer.MethodExternalSeeking, UpdateOnly: mixer.MethodUpdateOnly), index, 1, mixer, generation);
    }
}
