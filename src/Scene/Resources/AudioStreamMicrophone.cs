namespace Electron2D;

/// <summary>Creates independent continuous playbacks of the selected recording device.</summary>
/// <remarks>Set ProjectSettings.AudioDriverEnableInput before Start or player Play. The resource owns no
/// device; AudioServer shares capture requests and bounded input history. Playbacks borrow this resource.
/// Playback Start, Stop and Dispose require the audio configuration owner; mixing/history reset serialize independently.
/// Interactive parents can prepare paused input on that owner and later schedule activation/release under the audio gate.
/// Input shortages produce silence without finishing playback. Direct monitoring can cause acoustic feedback.</remarks>
public sealed class AudioStreamMicrophone : AudioStream
{
    /// <summary>Creates a monophonic microphone resource with unknown duration.</summary>
    public AudioStreamMicrophone() { }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback() => new AudioStreamPlaybackMicrophone(this);
    /// <inheritdoc />
    protected override string OnGetStreamName() => "Microphone";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamMicrophone();
    /// <inheritdoc />
    /// <remarks>Only inherited resource metadata is copied; input devices and cursors belong to playbacks/server.</remarks>
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) => ThrowIfDisposed();
}

internal sealed class AudioStreamPlaybackMicrophone(AudioStreamMicrophone source) : AudioStreamPlaybackResampled
{
    internal override bool RequiresAudioOwner => true;
    private AudioInputDevice? _device;
    private long _cursor;
    private int _generation = -1;
    private bool _active;
    private bool _prepared;
    internal override void PrepareQueuedControls() { lock (AudioServer.Instance.StreamGate) { Check(); AudioServer.Instance.PrepareQueuedInput(reserve: !_prepared); _prepared = true; } }
    internal override void StartQueued(double time) { lock (AudioServer.Instance.StreamGate) { if (!_prepared) throw new InvalidOperationException("Prepare microphone controls on the audio owner."); StartCore(queued: true); } }
    internal override void StopQueued() { lock (AudioServer.Instance.StreamGate) StopCore(queued: true); }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(source.IsDisposed, source); }
    protected override void OnStart(double fromPosition)
    {
        AudioServer.Instance.Check();
        lock (AudioServer.Instance.StreamGate) StartCore(queued: false);
    }
    private void StartCore(bool queued)
    {
        lock (ResampleGate)
        {
            Check(); if (_active) return;
            _device = queued ? AudioServer.Instance.AcquireQueuedInput(this) : AudioServer.Instance.AcquireInput(this); _cursor = 0; _generation = -1; _active = true;
            try { BeginResample(); }
            catch (Exception error)
            {
                _active = false;
                try { if (queued) AudioServer.Instance.ReleaseQueuedInput(this); else AudioServer.Instance.ReleaseInput(this); }
                catch (Exception cleanup) { throw new AggregateException("Microphone start and cleanup failed.", error, cleanup); }
                throw;
            }
        }
    }
    protected override void OnStop()
    {
        AudioServer.Instance.Check(); lock (AudioServer.Instance.StreamGate) StopCore(queued: false);
    }
    private void StopCore(bool queued) { lock (ResampleGate) { if (!_active) return; try { if (queued) AudioServer.Instance.ReleaseQueuedInput(this); else AudioServer.Instance.ReleaseInput(this); } finally { _active = false; } } }
    protected override bool OnIsPlaying() { lock (ResampleGate) { Check(); return _active; } }
    protected override double OnGetPlaybackPosition() { Check(); return 0; }
    protected override void OnSeek(double time) => Check();
    protected override float OnGetStreamSamplingRate() => _device?.MixRate ?? 1;
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (ResampleGate)
        {
            Check(); if (!_active) { buffer.Clear(); return 0; }
            var current = AudioServer.Instance.CurrentInput;
            if (!ReferenceEquals(current, _device)) { _device = current; _generation = -1; _cursor = 0; BeginResample(); }
            return base.OnMix(buffer, rateScale);
        }
    }
    protected override int OnMixResampled(Span<Vector2> buffer)
    {
        lock (ResampleGate) { Check(); if (_active && _device is not null) _device.Mix(buffer, ref _cursor, ref _generation); else buffer.Clear(); return buffer.Length; }
    }
    internal void CloseInput() { lock (ResampleGate) { _active = false; _device = null; } }
    protected override void ValidateDisposal() { AudioServer.Instance.Check(); base.ValidateDisposal(); }
    protected override void Dispose(bool disposing) { try { OnStop(); } finally { try { if (_prepared) { AudioServer.Instance.ReleaseQueuedInputReservation(); _prepared = false; } } finally { _device = null; base.Dispose(disposing); } } }
}
