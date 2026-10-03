namespace Electron2D;

/// <summary>Associates a finite sample stream with an independent native playback request.</summary>
/// <remarks>AudioStreamPlayback owns its association, and scene queries return borrowed state. Parameters
/// are typed: seconds, positive pitch ratio, four stereo-pair gains and an exact bus name. Preparing/starting
/// native state requires the audio owner. Getters copy the gain vector; scalar controls use prepared storage.</remarks>
public sealed class AudioSamplePlayback : ElectronObject
{
    private readonly AudioStream _stream;
    private double _offset;
    private float _pitch = 1;
    private string _bus = "Master";
    private Vector2[] _volume = [Vector2.One, Vector2.One, Vector2.One, Vector2.One];
    internal FAudioSampleVoice? Native;
    internal AudioStreamPlayback? Owner;
    /// <summary>Creates an inactive sample request for a borrowed finite stream.</summary>
    /// <param name="stream">Live sample-capable resource.</param>
    /// <exception cref="NotSupportedException">The stream has no finite sample representation.</exception>
    public AudioSamplePlayback(AudioStream stream) { ArgumentNullException.ThrowIfNull(stream); if (!stream.CanBeSampled()) throw new NotSupportedException("This stream cannot prepare sample playback."); _stream = stream; }
    /// <summary>Gets the borrowed originating stream.</summary>
    public AudioStream Stream { get { ThrowIfDisposed(); return _stream; } }
    /// <summary>Gets or sets the finite requested start offset in seconds.</summary>
    /// <value>Zero initially; a prepared voice seeks/restarts at the assigned seconds.</value>
    public double Offset { get { ThrowIfDisposed(); return _offset; } set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); var server = AudioServer.Instance; server.Lock(); try { ThrowIfDisposed(); Native?.Play(value); _offset = value; } finally { server.Unlock(); } } }
    /// <summary>Gets or sets the finite positive sample pitch ratio.</summary>
    /// <value>One initially; a prepared native voice enforces its hardware range.</value>
    public float PitchScale
    {
        get { ThrowIfDisposed(); return _pitch; }
        set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); var server = AudioServer.Instance; server.Lock(); try { ThrowIfDisposed(); var old = _pitch; _pitch = value; try { Native?.RefreshPitch(); } catch { _pitch = old; throw; } } finally { server.Unlock(); } }
    }
    /// <summary>Gets or sets four copied nonnegative stereo-pair linear gains.</summary>
    /// <value>Unity for every channel initially; front, center/low-frequency and two rear pairs.</value>
    public Vector2[] VolumeVector
    {
        get { ThrowIfDisposed(); return (Vector2[])_volume.Clone(); }
        set { ArgumentNullException.ThrowIfNull(value); if (value.Length != 4) throw new ArgumentException("Four stereo pairs are required.", nameof(value)); foreach (var v in value) if (!v.IsFinite() || v.X < 0 || v.Y < 0) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (Vector2[])value.Clone(); var server = AudioServer.Instance; server.Lock(); try { ThrowIfDisposed(); var old = _volume; _volume = copy; try { Native?.RefreshMatrix(); } catch { _volume = old; throw; } } finally { server.Unlock(); } }
    }
    /// <summary>Gets or sets the requested bus name.</summary>
    /// <value>Master initially; missing names route as Master.</value>
    public string Bus
    {
        get { ThrowIfDisposed(); return _bus; }
        set { ArgumentNullException.ThrowIfNull(value); var server = AudioServer.Instance; server.Lock(); try { ThrowIfDisposed(); var old = _bus; _bus = value; try { Native?.RefreshBus(); } catch { _bus = old; throw; } } finally { server.Unlock(); } }
    }
    internal void SetOffset(double value) => _offset = value;
    internal Vector2 Gain(int pair) => _volume[pair];
    internal void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_stream.IsDisposed, _stream); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (Owner is not null) throw new InvalidOperationException("Associated sample playback is borrowed from its owner."); if (Native is not null) AudioServer.Instance.Check(); base.ValidateDisposal(); }
}
