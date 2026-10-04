namespace Electron2D;

public partial class AnimationMixer
{
    private int _audioMaxPolyphony = 32;
    private readonly Dictionary<Node, AnimationAudioTransport> _audioTransports = new(ReferenceEqualityComparer.Instance);
    private readonly List<AnimationBinding> _audioBindings = [];
    /// <summary>Gets or sets per-receiver audio cue capacity captured at binding preparation.</summary>
    /// <value>Thirty-two initially; zero through 128.</value>
    /// <remarks>Mutation rebuilds prepared receiver transports. Zero selects intentionally silent cue playback. Receiver nodes and authored sources remain borrowed.</remarks>
    /// <exception cref="ObjectDisposedException">The mixer is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is outside the attached scene owner thread or cleanup fails.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Capacity is outside the supported bounds.</exception>
    public int AudioMaxPolyphony { get { ThrowIfDisposed(); return _audioMaxPolyphony; } set { EnsureAnimationMutable(); if (value is < 0 or > 128) throw new ArgumentOutOfRangeException(nameof(value)); if (_audioMaxPolyphony == value) return; _audioMaxPolyphony = value; InvalidateBindings(); } }
    internal AnimationAudioTransport GetAudioTransport(Node target) { if (_audioTransports.TryGetValue(target, out var existing)) return existing; var transport = new AnimationAudioTransport(target, AudioMaxPolyphony); _audioTransports.Add(target, transport); return transport; }
    internal void RegisterAudioBinding(AnimationBinding binding) => _audioBindings.Add(binding);
    private bool AudioBindingsCurrent() { foreach (var transport in _audioTransports.Values) if (!transport.IsCurrent) return false; return true; }
    private void BeginAudioFrame() { foreach (var transport in _audioTransports.Values) transport.TotalWeight = 0; foreach (var binding in _audioBindings) binding.BeginFrame(); }
    private void FinishAudioFrame() { foreach (var binding in _audioBindings) binding.FinishFrame(this); }
    internal void StopAudioPlayback()
    {
        foreach (var binding in _audioBindings) binding.StopPlayback();
        foreach (var transport in _audioTransports.Values) transport.PausePlayback();
    }
    private void ClearAudioBindings()
    {
        List<Exception>? errors = null;
        foreach (var transport in _audioTransports.Values) try { transport.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        _audioTransports.Clear(); _audioBindings.Clear(); ThrowCollected("Audio animation cleanup failed.", errors);
    }
    private static readonly PropertyDescriptor[] AudioProperties = [new PropertyDescriptor<AnimationMixer, int>(nameof(AudioMaxPolyphony), n => n.AudioMaxPolyphony, (n, v) => n.AudioMaxPolyphony = v, _ => 32)];
}
internal sealed class AnimationAudioTransport : IDisposable
{
    private readonly Node _target;
    private readonly AudioStreamPlayer _player;
    private readonly AudioStreamPolyphonic _source;
    private readonly AudioStreamPlaybackPolyphonic _playback;
    private readonly FAudioStreamVoice _voice;
    private readonly Dictionary<AudioStream, long> _revisions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AudioStreamRandomizer, ulong> _randomShapes = new(ReferenceEqualityComparer.Instance);
    internal bool IsCurrent { get { if (_target.IsDisposed || !_player.HasAnimationVoice(_voice)) return false; foreach (var shape in _randomShapes) if (shape.Key.PreparedShape() != shape.Value) return false; foreach (var source in _revisions) if (source.Key.IsDisposed || source.Key.ChangeRevision != source.Value) return false; return true; } }
    internal double TotalWeight;
    internal AnimationAudioTransport(Node target, int polyphony)
    {
        _target = target; _player = target is AudioStreamPlayer player ? player : ((AudioStreamEmitter)target).AnimationPlayer; _source = new() { Polyphony = polyphony }; _playback = (AudioStreamPlaybackPolyphonic)_source.InstantiatePlayback();
        try { _voice = _player.PrepareAnimationVoice(_playback); } catch { _playback.Dispose(); _source.Dispose(); throw; }
    }
    internal void PausePlayback() { if (!_player.IsDisposed && _player.HasAnimationVoice(_voice)) _player.StopAnimationVoice(_voice); }
    internal void Prepare(AudioStream stream) { _playback.PrepareStream(stream, _player.PlaybackType, _player.Bus); var pending = new Stack<AudioStream>(); var seen = new HashSet<AudioStream>(ReferenceEqualityComparer.Instance); pending.Push(stream); lock (AudioStream.GraphGate) while (pending.TryPop(out var source)) if (seen.Add(source)) { _revisions[source] = source.ChangeRevision; if (source is AudioStreamRandomizer randomizer) _randomShapes[randomizer] = randomizer.PreparedShape(); source.AppendChildren(pending); } }
    internal long Play(AudioStream stream, double offset, float volumeDB)
    {
        if (_target.IsDisposed || _target.IsQueuedForDeletion || !_player.HasAnimationVoice(_voice)) return AudioStreamPlaybackPolyphonic.InvalidID;
        if (_target is AudioStreamEmitter emitter) emitter.RefreshAnimationSpatial(); _player.StartAnimationVoice(_source, _voice); return _playback.PlayStream(stream, offset, volumeDB, playbackType: _player.PlaybackType, bus: _player.Bus);
    }
    internal bool Playing(long id) => !_playback.IsDisposed && _playback.IsStreamPlaying(id);
    internal void Stop(long id) { if (!_playback.IsDisposed) _playback.StopStream(id); }
    internal void SetGain(long id, float db) { if (!_playback.IsDisposed) _playback.SetStreamVolume(id, db); }
    public void Dispose()
    {
        try { if (!_player.IsDisposed) { if (_player.HasAnimationVoice(_voice)) _player.ReleaseAnimationVoice(_voice); _player.ClearAnimationSource(_source); } }
        finally { if (!_playback.IsDisposed && _playback.SceneOwner is null) _playback.Dispose(); _source.Dispose(); }
    }
}
