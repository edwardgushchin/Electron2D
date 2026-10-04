namespace Electron2D;

public sealed partial class AudioStreamPlaybackPolyphonic
{
    private sealed class PreparedChild(AudioStreamPlayback playback) { internal readonly AudioStreamPlayback Playback = playback; internal bool Used; }
    private readonly Dictionary<AudioStream, PreparedChild[]> _prepared = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<AudioStream> _preparedRoots = new(ReferenceEqualityComparer.Instance);
    internal void PrepareStream(AudioStream stream, AudioServer.PlaybackType type, string bus)
    {
        var server = AudioServer.Service; server.LockCore();
        try
        {
            Check(); Idle(); Owner(); _source.ValidateChild(stream); stream.EnsurePlaybackOwner(); _ = Random.Shared.NextDouble();
            _busy = true; _source.EnterCall(0);
            try { PrepareStreamCore(stream, type, bus); _preparedRoots.Add(stream); } finally { AudioStream.ExitCall(); _busy = false; }
        }
        finally { server.UnlockCore(); }
    }
    private void PrepareStreamCore(AudioStream stream, AudioServer.PlaybackType type, string bus)
    {
        if (stream is AudioStreamRandomizer randomizer)
        {
            for (var i = 0; i < randomizer.StreamsCount; i++) if (randomizer.GetStream(i) is { } child) PrepareStreamCore(child, AudioServer.PlaybackType.Stream, bus);
            return;
        }
        if (_prepared.ContainsKey(stream)) return;
        var children = new PreparedChild[_voices.Length]; var count = 0;
        try
        {
            for (; count < children.Length; count++)
            {
                var preparing = AudioStream.PreparingAnimation; AudioStreamPlayback playback; AudioStream.PreparingAnimation = true; try { playback = stream.InstantiatePlayback(); } finally { AudioStream.PreparingAnimation = preparing; }
                children[count] = new(playback);
                if (ReferenceEquals(playback, this) || playback.SceneOwner is not null || playback.CompositeOwner is not null) throw new InvalidOperationException("Prepared factories must return independent owned playbacks.");
                for (var i = 0; i < count; i++) if (ReferenceEquals(children[i].Playback, playback)) throw new InvalidOperationException("Prepared slots cannot share playback state.");
                playback.CompositeOwner = this; playback.PrepareQueuedControls();
                var transport = type == AudioServer.PlaybackType.Default ? (ProjectSettings.GetWithOverride(ProjectSettings.AudioGeneralDefaultPlaybackType) == AudioDefaultPlaybackType.Sample ? AudioServer.PlaybackType.Sample : AudioServer.PlaybackType.Stream) : type;
                if (transport == AudioServer.PlaybackType.Sample && stream.CanBeSampled()) { var request = new AudioSamplePlayback(stream) { Bus = bus }; playback.SetSamplePlayback(request); request.Native = AudioServer.Service.PrepareSample(request); }
                playback.CompositeOwner = this;
            }
            _prepared.Add(stream, children);
        }
        catch { foreach (var child in children) if (child is not null && ReferenceEquals(child.Playback.CompositeOwner, this)) { child.Playback.CompositeOwner = null; child.Playback.Dispose(); } throw; }
    }
    private long PlayPrepared(AudioStream root, double offset, float volumeDB, float pitch)
    {
        if (!double.IsFinite(offset)) throw new ArgumentOutOfRangeException(nameof(offset)); var gain = Gain(volumeDB); Pitch(pitch); var stream = root; var variation = 1f; var pitchVariation = 1f;
        for (var depth = 0; stream is AudioStreamRandomizer randomizer; depth++) { if (depth >= 256) throw new InvalidOperationException("Random nesting exceeds prepared bounds."); stream = randomizer.SelectPrepared(out var p, out var g); if (stream is null) return InvalidID; pitch *= p; pitchVariation *= p; gain *= g; variation *= g; }
        if (!float.IsFinite(pitch) || !float.IsFinite(gain)) throw new ArithmeticException("Prepared voice gain/pitch exceeds finite storage.");
        if (!_prepared.TryGetValue(stream, out var prepared)) throw new InvalidOperationException("Audio source changed; rebuild prepared animation bindings before starting.");
        var index = -1;
        for (var i = 0; i < _voices.Length; i++) { var v = _voices[i]; if (v.Active && v.Playback?.GetSamplePlayback() is not null && !v.Playback.IsPlaying()) v.Active = false; if (!v.Active) { index = i; break; } }
        if (index < 0) return InvalidID; var voice = _voices[index]; var cleanup = Release(voice); if (cleanup is not null) throw cleanup;
        PreparedChild? entry = null; foreach (var item in prepared) if (!item.Used) { entry = item; break; }
        if (entry is null) return InvalidID;
        var playback = entry.Playback; entry.Used = true; voice.Prepared = entry; voice.Stream = root; voice.Playback = playback; voice.Offset = offset; voice.Gain = voice.PreviousGain = gain; voice.VariationGain = variation; voice.VariationPitch = pitchVariation; voice.Pitch = pitch; voice.Generation = _generation++; voice.Active = true; voice.Finishing = false; voice.Pending = playback.GetSamplePlayback() is null;
        try
        {
            playback.UpdateNativeOwner(_nativeOwner);
            if (playback.GetSamplePlayback() is { } sample) { sample.SetVoiceGain(gain); sample.PitchScale = pitch; playback.Start(offset); if ((!_active || !_nativeEnabled) && _nativeOwner is not null) sample.Native?.Pause(true); }
            return ((long)index << 32) | voice.Generation;
        }
        catch { Release(voice); throw; }
    }
    private Exception? DisposePrepared()
    {
        Exception? error = null;
        foreach (var bank in _prepared.Values) foreach (var child in bank) { child.Playback.CompositeOwner = null; try { child.Playback.Dispose(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); } }
        _prepared.Clear(); _preparedRoots.Clear(); return error;
    }
}
