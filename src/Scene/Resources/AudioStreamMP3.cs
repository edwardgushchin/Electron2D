namespace Electron2D;

/// <summary>Stores copied MPEG Layer III audio and musical loop metadata.</summary>
/// <remarks>Data replacement validates/decodes before committing. Prepared PCM snapshots are immutable;
/// existing playback retains its captured samples. Consumers borrow this resource and own independent playbacks. Import prepares full mono/stereo PCM
/// outside mixing; long assets therefore require proportionate managed storage.</remarks>
public sealed class AudioStreamMP3 : AudioStream
{
    private readonly object _gate = new();
    private byte[] _data = [];
    private AudioDecodedPCM? _pcm;
    private double _bpm, _loopOffset;
    private int _beatCount, _barBeats = 4;
    private bool _loop;
    /// <summary>Creates empty data, disabled looping, zero tempo/beat count and four beats per bar.</summary>
    public AudioStreamMP3() { }
    /// <summary>Gets or sets copied MPEG Layer III file bytes.</summary>
    /// <value>Empty initially; invalid replacement throws before changing resource state.</value>
    /// <exception cref="FormatException">The bytes are not supported finite mono/stereo MPEG audio.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public byte[] Data
    {
        get { lock (_gate) { ThrowIfDisposed(); return (byte[])_data.Clone(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var copy = (byte[])value.Clone(); AudioDecodedPCM pcm;
            try { pcm = AudioFileDecoder.DecodeMP3(copy); }
            catch (Exception error) when (error is ArgumentException or InvalidDataException or EndOfStreamException or OverflowException or IndexOutOfRangeException) { throw new FormatException("MPEG audio data is invalid.", error); }
            lock (_gate) { ThrowIfDisposed(); _data = copy; _pcm = pcm; }
        }
    }
    /// <summary>Gets or sets BPM metadata.</summary>
    /// <value>Nonnegative finite beats per minute.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned numeric value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public double BPM
    {
        get { lock (_gate) { ThrowIfDisposed(); return _bpm; } }
        set { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _bpm = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets BeatCount metadata.</summary>
    /// <value>Nonnegative total musical beats.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned numeric value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int BeatCount
    {
        get { lock (_gate) { ThrowIfDisposed(); return _beatCount; } }
        set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _beatCount = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets BarBeats metadata.</summary>
    /// <value>Nonnegative beats per bar; four initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned numeric value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int BarBeats
    {
        get { lock (_gate) { ThrowIfDisposed(); return _barBeats; } }
        set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _barBeats = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets Loop metadata.</summary>
    /// <value>False initially; enabled EOF or beat loops restart at LoopOffset.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Loop
    {
        get { lock (_gate) { ThrowIfDisposed(); return _loop; } }
        set { lock (_gate) { ThrowIfDisposed(); _loop = value; } }
    }
    /// <summary>Gets or sets LoopOffset metadata.</summary>
    /// <value>Finite seconds; zero initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned numeric value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public double LoopOffset
    {
        get { lock (_gate) { ThrowIfDisposed(); return _loopOffset; } }
        set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _loopOffset = value; } }
    }
    private AudioMusicLoop GetLoopSettings() { lock (_gate) { ThrowIfDisposed(); return new(_loop, _loopOffset, _bpm, _beatCount); } }
    /// <inheritdoc />
    protected override double OnGetLength() { lock (_gate) { ThrowIfDisposed(); return _pcm is null ? 0 : _pcm.Samples.Length / (double)(_pcm.Channels * _pcm.Rate); } }
    /// <inheritdoc />
    protected override bool OnIsMonophonic() => false;
    /// <inheritdoc />
    protected override bool OnHasLoop() => Loop;
    /// <inheritdoc />
    protected override double OnGetBPM() => BPM;
    /// <inheritdoc />
    protected override int OnGetBeatCount() => BeatCount;
    /// <inheritdoc />
    protected override int OnGetBarBeats() => BarBeats;
    /// <inheritdoc />
    protected override PropertyDescriptor[] OnGetParameterList() => [AudioStreamPlayback.LoopingParameter];
    /// <inheritdoc />
    public override bool CanBeSampled() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    public override AudioSample GenerateSample()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_pcm is null) throw new InvalidOperationException("Initialize compressed audio before sampling.");

            var frames = _pcm.Samples.Length / _pcm.Channels;
            var begin = (int)Math.Clamp(_loopOffset * _pcm.Rate, 0, frames);
            if (_loop && begin == frames) throw new InvalidOperationException("The loop offset leaves no sample frames.");
            return new(this, _pcm.Samples, _pcm.Channels, _pcm.Rate, _loop ? AudioLoopMode.Forward : AudioLoopMode.Disabled, begin, frames);
        }
    }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        lock (_gate) { ThrowIfDisposed(); if (_pcm is null) throw new InvalidOperationException("MPEG data has not been initialized."); return new Playback(this, _pcm); }
    }
    /// <summary>Loads validated MPEG audio from borrowed encoded bytes.</summary>
    /// <param name="streamData">Borrowed MPEG file data.</param>
    /// <returns>A caller-owned independent audio resource.</returns>
    /// <exception cref="FormatException">The encoded data is invalid.</exception>
    public static AudioStreamMP3 LoadFromBuffer(ReadOnlySpan<byte> streamData)
    {
        var stream = new AudioStreamMP3(); try { stream.Data = streamData.ToArray(); return stream; } catch { stream.Dispose(); throw; }
    }
    /// <summary>Loads validated MPEG audio through engine file access.</summary>
    /// <param name="path">Filesystem, resource or user path.</param>
    /// <returns>A caller-owned independent audio resource.</returns>
    public static AudioStreamMP3 LoadFromFile(string path) => LoadFromBuffer(FileAccess.GetFileAsBytes(path));
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamMP3();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var other = (AudioStreamMP3)target; lock (_gate) { ThrowIfDisposed(); lock (other._gate) { other._data = (byte[])_data.Clone(); other._pcm = _pcm; other._bpm = _bpm; other._beatCount = _beatCount; other._barBeats = _barBeats; other._loop = _loop; other._loopOffset = _loopOffset; } }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _data = []; _pcm = null; } base.Dispose(disposing); }
    private sealed class Playback(AudioStreamMP3 source, AudioDecodedPCM pcm) : AudioStreamPlaybackResampled
    {
        private readonly AudioFileCursor _cursor = new(source, pcm, source.GetLoopSettings);
        protected override void OnStart(double fromPosition) { _cursor.Start(fromPosition); BeginResample(); }
        protected override void OnStop() => _cursor.Stop();
        protected override bool OnIsPlaying() => _cursor.Active;
        protected override int OnGetLoopCount() => _cursor.Loops;
        protected override double OnGetPlaybackPosition() => _cursor.Position;
        protected override void OnSeek(double time) => _cursor.Seek(time);
        protected override float OnGetStreamSamplingRate() => _cursor.Rate;
        protected override int OnMixResampled(Span<Vector2> buffer) => _cursor.Mix(buffer, LoopingOverride);
    }
}
