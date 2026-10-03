using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace Electron2D;

/// <summary>Owns copied PCM, IMA ADPCM or QOA audio data with sample-rate and loop metadata.</summary>
/// <remarks>Data/metadata are synchronized independently of scene ownership. Changes affect borrowed playback
/// through resource snapshots; decoders prepare their immutable PCM outside repeated mixing.</remarks>
public sealed partial class AudioStreamWAV : AudioStream
{
    /// <summary>Identifies the internal sample representation.</summary>
    public enum Format
    {
        /// <summary>Signed eight-bit PCM.</summary>
        PCM8 = 0,
        /// <summary>Little-endian signed sixteen-bit PCM.</summary>
        PCM16 = 1,
        /// <summary>Interleaved channel-byte IMA ADPCM, low nibble first.</summary>
        IMAADPCM = 2,
        /// <summary>Quite OK Audio file data.</summary>
        QOA = 3
    }
    private readonly object _gate = new();
    private byte[] _data = [];
    private Format _format;
    private AudioLoopMode _loop;
    private int _mixRate = 44100, _begin, _end;
    private bool _stereo;
    private long _version;
    private PCM? _pcm;
    private bool _prepared;
    private Exception? _decodeError;
    private Dictionary<string, string> _tags = new(StringComparer.Ordinal);
    internal sealed record PCM(float[] Samples, int Channels, int Rate, Format Format, AudioLoopMode Loop, int Begin, int End, long Version);
    /// <summary>Creates empty mono eight-bit data at 44100 Hz with no loop.</summary>
    public AudioStreamWAV() { }
    /// <summary>Gets or sets SampleFormat sample metadata.</summary>
    /// <value>PCM8 initially. Undefined formats reject.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Format SampleFormat
    {
        get { lock (_gate) { ThrowIfDisposed(); return _format; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value is < Format.PCM8 or > Format.QOA) throw new ArgumentOutOfRangeException(nameof(value)); if (_format == value) return; _format = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets Loop sample metadata.</summary>
    /// <value>Disabled initially. Undefined modes reject.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AudioLoopMode Loop
    {
        get { lock (_gate) { ThrowIfDisposed(); return _loop; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value is < AudioLoopMode.Disabled or > AudioLoopMode.Backward) throw new ArgumentOutOfRangeException(nameof(value)); if (_loop == value) return; _loop = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets MixRate sample metadata.</summary>
    /// <value>44100 initially. Zero rejects; other signed values retain their metadata identity.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int MixRate
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mixRate; } }
        set { lock (_gate) { ThrowIfDisposed(); if (value == 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_mixRate == value) return; _mixRate = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets LoopBegin sample metadata.</summary>
    /// <value>Zero initially; loop sample index. Validity is checked when consumed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int LoopBegin
    {
        get { lock (_gate) { ThrowIfDisposed(); return _begin; } }
        set { lock (_gate) { ThrowIfDisposed(); if (_begin == value) return; _begin = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets LoopEnd sample metadata.</summary>
    /// <value>Zero initially; loop boundary sample index. Validity is checked when consumed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int LoopEnd
    {
        get { lock (_gate) { ThrowIfDisposed(); return _end; } }
        set { lock (_gate) { ThrowIfDisposed(); if (_end == value) return; _end = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets Stereo sample metadata.</summary>
    /// <value>False initially; true uses interleaved left/right data.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode/rate is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Stereo
    {
        get { lock (_gate) { ThrowIfDisposed(); return _stereo; } }
        set { lock (_gate) { ThrowIfDisposed(); if (_stereo == value) return; _stereo = value; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets copied encoded audio bytes.</summary>
    /// <value>Empty initially; setters/getters never share caller-owned array storage.</value>
    /// <exception cref="ArgumentNullException">The input is null.</exception>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    public byte[] Data
    {
        get { lock (_gate) { ThrowIfDisposed(); return (byte[])_data.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = (byte[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _data = copy; _version++; _pcm = null; RefreshPreparedPCM(); } }
    }
    /// <summary>Gets or sets copied textual RIFF metadata.</summary>
    /// <value>An empty dictionary initially. Keys and values must be nonnull.</value>
    /// <exception cref="ArgumentNullException">The map or a key/value is null.</exception>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    public Dictionary<string, string> Tags
    {
        get { lock (_gate) { ThrowIfDisposed(); return new(_tags, StringComparer.Ordinal); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = new Dictionary<string, string>(StringComparer.Ordinal); foreach (var pair in value) { ArgumentNullException.ThrowIfNull(pair.Key); ArgumentNullException.ThrowIfNull(pair.Value); copy.Add(pair.Key, pair.Value); } lock (_gate) { ThrowIfDisposed(); _tags = copy; } }
    }
    /// <inheritdoc />
    protected override bool OnIsMonophonic() => false;
    /// <inheritdoc />
    protected override Dictionary<string, string> OnGetTags() => Tags;
    internal long Version { get { lock (_gate) { ThrowIfDisposed(); return _version; } } }
    internal PCM PreparePCM()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_pcm is not null) return _pcm; if (_decodeError is { } failure) throw new FormatException("Prepared audio data is invalid.", failure); _prepared = true;
            var channels = _stereo ? 2 : 1; var decoded = AudioPCMCodec.Decode(_data, _format, channels);
            return _pcm = new(decoded, channels, _mixRate, _format, _loop, _begin, _end, _version);
        }
    }
    private void RefreshPreparedPCM()
    {
        _decodeError = null; if (!_prepared) return;
        try { PreparePCM(); } catch (Exception error) { _decodeError = error; }
    }
    /// <inheritdoc />
    protected override double OnGetLength()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); long length = _format switch { Format.PCM8 => _data.Length, Format.PCM16 => _data.Length / 2, Format.IMAADPCM => (long)_data.Length * 2, _ => _data.Length < 12 ? 0 : (long)BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(4)) * _data[8] };
            return (length / (_stereo ? 2 : 1)) / (double)_mixRate;
        }
    }
    /// <inheritdoc />
    public override bool CanBeSampled() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    public override AudioSample GenerateSample() { var pcm = PreparePCM(); return new(this, pcm.Samples, pcm.Channels, pcm.Rate, pcm.Loop, pcm.Begin, pcm.End); }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback() => new WAVPlayback(this);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamWAV();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var other = (AudioStreamWAV)target; lock (_gate) { ThrowIfDisposed(); lock (other._gate) { other._data = (byte[])_data.Clone(); other._format = _format; other._loop = _loop; other._mixRate = _mixRate; other._begin = _begin; other._end = _end; other._stereo = _stereo; other._tags = new(_tags, StringComparer.Ordinal); other._version++; other._pcm = null; other._decodeError = null; other.RefreshPreparedPCM(); } }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _data = []; _pcm = null; _tags.Clear(); } base.Dispose(disposing); }
    private sealed class WAVPlayback(AudioStreamWAV stream) : AudioStreamPlaybackResampled
    {
        private PCM? _pcm;
        private long _offset;
        private int _sign = 1;
        private bool _active;
        protected override void OnStart(double fromPosition) { _pcm = stream.PreparePCM(); if (_pcm.Format == Format.IMAADPCM) _offset = 0; else OnSeek(fromPosition); _sign = 1; _active = true; BeginResample(); }
        protected override void OnStop() => _active = false;
        protected override void Dispose(bool disposing) { _active = false; _pcm = null; base.Dispose(disposing); }
        protected override bool OnIsPlaying() { _ = stream.Version; return _active; }
        protected override double OnGetPlaybackPosition() => _offset / (double)stream.MixRate;
        protected override void OnSeek(double time)
        {
            if (stream.SampleFormat == Format.IMAADPCM) return;
            var maximum = stream.GetLength(); time = time < 0 ? 0 : time >= maximum ? maximum - .001 : time;
            _offset = (long)(time * stream.MixRate);
        }
        protected override float OnGetStreamSamplingRate() => stream.MixRate;
        protected override int OnMixResampled(Span<Vector2> buffer)
        {
            if (_pcm is null || _pcm.Version != stream.Version) _pcm = stream.PreparePCM();
            var data = _pcm; var length = data.Samples.Length / data.Channels;
            var loop = data.Format == Format.IMAADPCM && data.Loop != AudioLoopMode.Disabled ? AudioLoopMode.Forward : data.Loop;
            if (loop != AudioLoopMode.Disabled && (data.Begin < 0 || data.End <= data.Begin || data.End >= length)) throw new InvalidOperationException("Audio loop bounds exceed available samples.");
            if (loop == AudioLoopMode.Backward) _sign = -1;
            var mixed = 0;
            while (mixed < buffer.Length && _active && length > 0)
            {
                if (_sign < 0 && loop != AudioLoopMode.Disabled && _offset < data.Begin)
                {
                    if (loop == AudioLoopMode.PingPong) { _offset = data.Begin + (data.Begin - _offset); _sign = 1; }
                    else _offset = data.End - (data.Begin - _offset);
                }
                else if (_sign > 0 && loop != AudioLoopMode.Disabled && _offset >= data.End)
                {
                    if (loop == AudioLoopMode.PingPong) { _offset = data.End - (_offset - data.End); _sign = -1; }
                    else _offset = data.Format == Format.IMAADPCM ? data.Begin : data.Begin + (_offset - data.End);
                }
                if (_offset < 0 || _offset >= length) { _active = false; break; }
                var limit = _sign < 0 ? loop == AudioLoopMode.Disabled ? 0 : data.Begin : loop == AudioLoopMode.Disabled ? length - 1 : data.End;
                var amount = (int)Math.Min(buffer.Length - mixed, (limit - _offset) / _sign + 1);
                if (amount <= 0) { _active = false; break; }
                for (var i = 0; i < amount; i++)
                {
                    if (_offset < 0 || _offset >= length) throw new InvalidOperationException("Audio loop traversal exceeds sample storage.");
                    var index = (int)_offset * data.Channels; var left = data.Samples[index]; buffer[mixed++] = new(left, data.Channels == 2 ? data.Samples[index + 1] : left); _offset += _sign;
                }
            }
            buffer[mixed..].Clear(); return mixed;
        }
    }
}
