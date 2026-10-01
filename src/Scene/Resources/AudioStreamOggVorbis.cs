namespace Electron2D;

/// <summary>Stores copied Vorbis packet audio and musical loop metadata.</summary>
/// <remarks>Data replacement validates/decodes before committing. Prepared PCM snapshots are immutable;
/// existing playback retains its captured samples. Consumers borrow this resource and own independent playbacks. Import prepares full mono/stereo PCM
/// outside mixing; long assets therefore require proportionate managed storage.</remarks>
public sealed class AudioStreamOggVorbis : AudioStream
{
    private readonly object _gate = new();
    private OggPacketSequence? _sequence;
    private Dictionary<string, string> _tags = new(StringComparer.Ordinal);
    private AudioDecodedPCM? _pcm;
    private readonly List<OggPacketSequence> _ownedSequences = [];
    private long _sequenceVersion;
    private double _bpm, _loopOffset;
    private int _beatCount, _barBeats = 4;
    private bool _loop;
    /// <summary>Creates empty data, disabled looping, zero tempo/beat count and four beats per bar.</summary>
    public AudioStreamOggVorbis() { }
    /// <summary>Gets or sets the borrowed encoded packet sequence.</summary>
    /// <value>Null initially. Non-null replacement validates/decodes headers and snapshots before committing.</value>
    /// <exception cref="FormatException">The packet sequence is not supported Vorbis data.</exception>
    /// <exception cref="ObjectDisposedException">This resource or its sequence is disposed.</exception>
    public OggPacketSequence? PacketSequence
    {
        get { lock (_gate) { ThrowIfDisposed(); return _sequence; } }
        set
        {
            AudioDecodedPCM? pcm = null; long version = 0; Dictionary<string, string> tags = new(StringComparer.Ordinal);
            if (value is not null)
            {
                try { (pcm, tags, version) = AudioFileDecoder.DecodeVorbis(value); }
                catch (Exception error) when (error is ArgumentException or InvalidDataException or EndOfStreamException or OverflowException or IndexOutOfRangeException) { throw new FormatException("Vorbis packet data is invalid.", error); }
                value.SamplingRate = pcm.Rate;
            }
            lock (_gate) { ThrowIfDisposed(); _sequence = value; _pcm = pcm; _tags = tags; _sequenceVersion = version; }
        }
    }
    /// <summary>Gets or sets copied textual Vorbis comments.</summary>
    /// <value>Empty initially. Imported names use lowercase keys; later authored names are retained literally.</value>
    /// <exception cref="ArgumentNullException">The dictionary or a key/value is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Dictionary<string, string> Tags
    {
        get { lock (_gate) { ThrowIfDisposed(); return new(_tags, StringComparer.Ordinal); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = new Dictionary<string, string>(StringComparer.Ordinal); foreach (var pair in value) { ArgumentNullException.ThrowIfNull(pair.Key); ArgumentNullException.ThrowIfNull(pair.Value); copy.Add(pair.Key, pair.Value); } lock (_gate) { ThrowIfDisposed(); _tags = copy; } }
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
    /// <value>At least two beats per bar; four initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned numeric value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int BarBeats
    {
        get { lock (_gate) { ThrowIfDisposed(); return _barBeats; } }
        set { if (value < 2) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _barBeats = value; } EmitChanged(); }
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
    internal AudioMusicLoop GetLoopSettings() { lock (_gate) { ThrowIfDisposed(); return new(_loop, _loopOffset, _bpm, _beatCount); } }
    /// <inheritdoc />
    protected override double OnGetLength() { lock (_gate) { ThrowIfDisposed(); return _sequence?.GetLength() ?? 0; } }
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
    protected override Dictionary<string, string> OnGetTags() => Tags;
    /// <inheritdoc />
    protected override PropertyDescriptor[] OnGetParameterList() => [AudioStreamPlayback.LoopingParameter];
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        lock (_gate) { ThrowIfDisposed(); if (_pcm is null) throw new InvalidOperationException("Vorbis data has not been initialized."); if (_sequence!.Version != _sequenceVersion) (_pcm, _tags, _sequenceVersion) = AudioFileDecoder.DecodeVorbis(_sequence); return new AudioStreamPlaybackOggVorbis(this, _pcm, _sequence, _sequenceVersion); }
    }
    /// <summary>Loads validated Vorbis audio from borrowed encoded bytes.</summary>
    /// <param name="streamData">Borrowed Vorbis file data.</param>
    /// <returns>A caller-owned independent audio resource.</returns>
    /// <exception cref="FormatException">The encoded data is invalid.</exception>
    public static AudioStreamOggVorbis LoadFromBuffer(ReadOnlySpan<byte> streamData)
    {
        var stream = new AudioStreamOggVorbis(); OggPacketSequence? sequence = null; try { sequence = OggVorbisImport.Read(streamData); stream.PacketSequence = sequence; stream._ownedSequences.Add(sequence); return stream; } catch { stream.Dispose(); sequence?.Dispose(); throw; }
    }
    /// <summary>Loads validated Vorbis audio through engine file access.</summary>
    /// <param name="path">Filesystem, resource or user path.</param>
    /// <returns>A caller-owned independent audio resource.</returns>
    public static AudioStreamOggVorbis LoadFromFile(string path) => LoadFromBuffer(FileAccess.GetFileAsBytes(path));
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamOggVorbis();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var other = (AudioStreamOggVorbis)target; OggPacketSequence? sequence; Dictionary<string, string> tags;
        lock (_gate) { ThrowIfDisposed(); sequence = _sequence; tags = new(_tags, StringComparer.Ordinal); other._bpm = _bpm; other._beatCount = _beatCount; other._barBeats = _barBeats; other._loop = _loop; other._loopOffset = _loopOffset; }
        other.PacketSequence = (OggPacketSequence?)duplicateSubresource(sequence); other.Tags = tags;
    }
    internal override void ReloadFrom(AudioStream source)
    {
        var other = (AudioStreamOggVorbis)source; var sequence = other.PacketSequence;
        if (sequence is null) throw new FormatException("Reloaded Vorbis has no packets.");
        var cloned = (OggPacketSequence)sequence.Duplicate();
        try
        {
            PacketSequence = cloned;
            lock (_gate) { _ownedSequences.Add(cloned); _bpm = other.BPM; _beatCount = other.BeatCount; _barBeats = other.BarBeats; _loop = other.Loop; _loopOffset = other.LoopOffset; }
        }
        catch { cloned.Dispose(); throw; }
        EmitChanged();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { if (disposing) foreach (var owned in _ownedSequences) owned.Dispose(); _ownedSequences.Clear(); _sequence = null; _pcm = null; _tags.Clear(); } base.Dispose(disposing); }
}
