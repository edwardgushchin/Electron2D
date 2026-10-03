namespace Electron2D;

/// <summary>Records stereo bus PCM into a caller-owned WAV stream while passing audio through.</summary>
/// <remarks>The current front stereo pair supplies the recording. A prepared capture ring separates
/// the native audio callback from a draining worker; encoding and result allocation happen on the caller thread.
/// Restarting clears the previous sample. Copies retain the output format, not captured data or threads.</remarks>
public sealed class AudioEffectRecord : AudioEffect
{
    private readonly object _gate = new();
    private AudioEffectRecordInstance? _current;
    private AudioStreamWAV.Format _format = AudioStreamWAV.Format.PCM16;

    /// <summary>Creates a recorder configured to produce sixteen-bit stereo PCM.</summary>
    public AudioEffectRecord() { }

    /// <summary>Gets or sets the captured stream's output encoding.</summary>
    /// <value>PCM16 by default; PCM8, IMAADPCM and QOA are also supported.</value>
    /// <exception cref="ArgumentOutOfRangeException">The format is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioStreamWAV.Format Format
    {
        get { lock (_gate) { ThrowIfDisposed(); return _format; } }
        set { if (value is < AudioStreamWAV.Format.PCM8 or > AudioStreamWAV.Format.QOA) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_format == value) return; _format = value; } EmitChanged(); }
    }

    /// <summary>Starts a new recording or stops the current one.</summary>
    /// <param name="record">True starts fresh and removes the previous sample; false stops and drains pending PCM.</param>
    /// <exception cref="InvalidOperationException">Start has no live prepared effect instance.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetRecordingActive(bool record)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (record)
            {
                if (_current is null || _current.IsDisposed) throw new InvalidOperationException("Instantiate or attach the recorder before starting.");
                _current.Start();
            }
            else _current?.Finish();
        }
    }

    /// <summary>Gets whether the current instance still accepts captured PCM.</summary>
    /// <returns>False before instantiation, after stop, after overflow or after output closure.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool IsRecordingActive() { lock (_gate) { ThrowIfDisposed(); return _current?.Recording ?? false; } }

    /// <summary>Encodes a snapshot of the captured stereo PCM into a new caller-owned WAV resource.</summary>
    /// <returns>Null before a sample has been captured; otherwise an independent nonlooping stream.</returns>
    /// <remarks>Can run while active without stopping. Encoding and copying allocate on the caller thread.
    /// A recording-buffer overrun or worker failure is reported rather than returning incomplete audio.</remarks>
    /// <exception cref="InvalidOperationException">Capture overran or the worker failed.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioStreamWAV? GetRecording()
    {
        float[] pcm; AudioStreamWAV.Format format; int rate;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_current is null) return null;
            pcm = _current.Snapshot(); format = _format; rate = _current.Rate;
        }
        if (pcm.Length == 0) return null;
        var encoded = AudioPCMCodec.Encode(pcm, format, 2, rate);
        return new AudioStreamWAV { SampleFormat = format, Stereo = true, MixRate = rate, Loop = AudioLoopMode.Disabled, Data = encoded };
    }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate()
    {
        var fresh = new AudioEffectRecordInstance(this);
        try
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (_current is null || _current.IsDisposed || !_current.IsAttached) SelectCurrent(fresh);
                return fresh;
            }
        }
        catch { fresh.Dispose(); throw; }
    }

    internal void SelectCurrent(AudioEffectRecordInstance fresh)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_current, fresh)) return;
            var previous = _current;
            if (previous?.Recording == true) fresh.Start();
            if (previous is { IsAttached: false }) previous.Finish();
            fresh.Previous = previous is { IsAttached: true } ? previous : null;
            _current = fresh;
        }
    }

    internal void Forget(AudioEffectRecordInstance instance)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_current, instance)) _current = instance.Previous is { IsDisposed: false } previous ? previous : null;
            else
                for (var current = _current; current is not null; current = current.Previous)
                    if (ReferenceEquals(current.Previous, instance)) { current.Previous = instance.Previous; break; }
        }
    }

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectRecord, AudioStreamWAV.Format>(nameof(Format), e => e.Format, (e, v) => e.Format = v, _ => AudioStreamWAV.Format.PCM16, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectRecord();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        lock (_gate) { ThrowIfDisposed(); ((AudioEffectRecord)target)._format = _format; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (_gate) for (var instance = _current; instance is not null; instance = instance.Previous) if (!instance.IsDisposed) instance.Finish();
        base.Dispose(disposing);
    }
}

internal sealed class AudioEffectRecordInstance : AudioEffectInstance
{
    private readonly AudioEffectRecord _source;
    private readonly AudioEffectCapture _capture;
    private readonly object _control = new(), _drainGate = new();
    private readonly List<float> _recordingData = [];
    private Thread? _worker;
    private volatile bool _recording;
    private volatile Exception? _failure;
    private long _discardedBaseline;
    internal AudioEffectRecordInstance? Previous;

    internal int Rate { get; }
    internal bool Recording { get { lock (_control) return _recording && !IsDisposed; } }

    internal AudioEffectRecordInstance(AudioEffectRecord source)
    {
        _source = source;
        Rate = checked((int)AudioServer.Instance.GetMixRate());
        if (Rate <= 0 || Rate > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared recording storage.");
        _capture = new AudioEffectCapture { BufferLength = 1.5f };
        try { using var prepared = _capture.Instantiate(); }
        catch { _capture.Dispose(); throw; }
    }

    internal void Start()
    {
        lock (_control)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(AudioEffectRecordInstance));
            Finish();
            lock (_drainGate) _recordingData.Clear();
            _capture.ClearBuffer();
            _failure = null;
            _discardedBaseline = _capture.GetDiscardedFrames();
            _recording = true;
            _worker = new Thread(DrainLoop) { IsBackground = true, Name = "Electron2D audio recorder" };
            try { _worker.Start(); }
            catch { _recording = false; _worker = null; throw; }
        }
    }

    internal void Finish()
    {
        lock (_control)
        {
            _recording = false;
            var worker = _worker; _worker = null;
            if (worker is not null && worker != Thread.CurrentThread) worker.Join();
            try { Drain(); }
            catch (Exception error) { _failure ??= error; }
        }
    }

    internal float[] Snapshot()
    {
        Drain();
        lock (_drainGate)
        {
            if (_failure is { } failure) throw new InvalidOperationException("Audio recording is incomplete.", failure);
            return _recordingData.ToArray();
        }
    }

    private void DrainLoop()
    {
        try
        {
            while (_recording) { Drain(); Thread.Sleep(1); }
            Drain();
        }
        catch (Exception error) { _failure = error; _recording = false; }
    }

    private void Drain()
    {
        lock (_drainGate)
        {
            if (_capture.IsDisposed) return;
            for (var available = _capture.GetFramesAvailable(); available > 0; available = _capture.GetFramesAvailable())
            {
                var frames = _capture.GetBuffer(Math.Min(available, 4096));
                if (_recordingData.Count > Array.MaxLength - frames.Length * 2) throw new OverflowException("Recorded PCM exceeds maximum array storage.");
                foreach (var frame in frames) { _recordingData.Add(frame.X); _recordingData.Add(frame.Y); }
            }
        }
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;
    internal override void OnAttached(int pair) { if (pair == 0) _source.SelectCurrent(this); }
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        ObjectDisposedException.ThrowIf(_source.IsDisposed, _source);
        source.CopyTo(destination);
        lock (_control)
        {
            if (!_recording) return;
            _capture.Capture(source);
            if (_capture.GetDiscardedFrames() > _discardedBaseline)
            {
                _failure = new InvalidOperationException("The recording worker could not drain the prepared PCM ring before it filled.");
                _recording = false;
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Finish(); _source.Forget(this); _capture.Dispose();
        base.Dispose(disposing);
    }
}
