namespace Electron2D;

public sealed partial class AudioStreamRandomizer
{
    internal AudioStreamPlayback InstantiatePreparedPlayback() => new PreparedPlayback(this);
    private sealed class PreparedPlayback : AudioStreamPlayback
    {
        private readonly AudioStreamRandomizer _source;
        private readonly Dictionary<AudioStream, AudioStreamPlayback> _children = new(ReferenceEqualityComparer.Instance);
        private AudioStreamPlayback? _selected;
        private float _pitch = 1, _gain = 1;
        private bool _active;
        private FAudioStreamVoice? _owner;
        internal PreparedPlayback(AudioStreamRandomizer source)
        {
            _source = source;
            try { for (var i = 0; i < source.StreamsCount; i++) if (source.GetStream(i) is { } stream && !_children.ContainsKey(stream)) { var child = stream.InstantiatePlayback(); if (ReferenceEquals(child, this) || child.SceneOwner is not null || child.CompositeOwner is not null) throw new InvalidOperationException("Prepared randomizer factories must return independent state."); child.CompositeOwner = this; _children.Add(stream, child); } }
            catch { foreach (var child in _children.Values) { child.CompositeOwner = null; child.Dispose(); } throw; }
        }
        internal override bool RequiresAudioOwner { get { foreach (var child in _children.Values) if (child.RequiresAudioOwner) return true; return false; } }
        internal override void PrepareQueuedControls() { foreach (var child in _children.Values) child.PrepareQueuedControls(); }
        internal override void UpdateNativeOwner(FAudioStreamVoice? owner, bool enabled = true) { _owner = owner; _selected?.UpdateNativeOwner(owner, enabled && _active); }
        private void StartCore(double position, bool queued)
        {
            if (_selected is not null) { if (queued) _selected.StopQueued(); else _selected.Stop(); }
            var stream = _source.SelectPrepared(out _pitch, out _gain); _active = true;
            if (stream is null) { _selected = null; return; }
            if (!_children.TryGetValue(stream, out _selected)) throw new InvalidOperationException("Randomizer graph changed; rebuild prepared animation caches.");
            _selected.UpdateNativeOwner(_owner); if (queued) _selected.StartQueued(position); else _selected.Start(position);
        }
        internal override void StartQueued(double position) => StartCore(position, true);
        internal override void StopQueued() { _active = false; _selected?.StopQueued(); }
        protected override void OnStart(double position) => StartCore(position, false);
        protected override void OnStop() { _active = false; _selected?.Stop(); }
        protected override bool OnIsPlaying() => _active && _selected?.IsPlaying() == true;
        protected override double OnGetPlaybackPosition() => _selected?.GetPlaybackPosition() ?? 0;
        protected override int OnGetLoopCount() => _selected?.GetLoopCount() ?? 0;
        protected override void OnSeek(double time) => _selected?.Seek(time);
        protected override int OnMix(Span<Vector2> output, float rateScale) { if (!_active || _selected is null) { output.Clear(); return output.Length; } var count = _selected.MixInto(output, rateScale * _pitch); for (var i = 0; i < count; i++) output[i] *= _gain; return count; }
        protected override void Dispose(bool disposing) { try { if (disposing) foreach (var child in _children.Values) { child.CompositeOwner = null; child.Dispose(); } } finally { base.Dispose(disposing); } }
    }
}
