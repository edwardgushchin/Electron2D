namespace Electron2D;

/// <summary>Stores copied Vorbis packet pages and their terminal granule positions.</summary>
/// <remarks>Packet and granule changes invalidate captured packet readers. Sampling-rate metadata is independent.
/// Setters accept incomplete authored state; decoding checks page/granule/header consistency when consumed.</remarks>
public sealed class OggPacketSequence : Resource
{
    private readonly object _gate = new();
    private byte[][][] _pages = [];
    private long[] _granules = [];
    private float _rate;
    private long _version;
    /// <summary>Creates empty packet/granule arrays and zero sampling-rate metadata.</summary>
    public OggPacketSequence() { }
    /// <summary>Gets or sets copied pages of copied encoded packet bytes.</summary>
    /// <value>Empty initially. Every array layer is independent of caller storage.</value>
    /// <exception cref="ArgumentNullException">An array layer is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public byte[][][] PacketData
    {
        get { lock (_gate) { ThrowIfDisposed(); return CopyPages(_pages); } }
        set { var copy = CopyPages(value); lock (_gate) { ThrowIfDisposed(); _pages = copy; _version++; } }
    }
    /// <summary>Gets or sets copied per-page signed terminal granules.</summary>
    /// <value>Empty initially; setters retain literal positions and invalidate captured readers.</value>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public long[] GranulePositions
    {
        get { lock (_gate) { ThrowIfDisposed(); return (long[])_granules.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = (long[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _granules = copy; _version++; } }
    }
    /// <summary>Gets or sets literal sampling-rate metadata.</summary>
    /// <value>Zero initially; changing only this metadata does not invalidate packet readers.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float SamplingRate { get { lock (_gate) { ThrowIfDisposed(); return _rate; } } set { lock (_gate) { ThrowIfDisposed(); _rate = value; } } }
    /// <summary>Divides the nonnegative final granule by the stored sampling rate.</summary>
    /// <returns>Zero for absent/negative final granules; otherwise literal floating-point division, including zero-rate infinity.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public double GetLength() { lock (_gate) { ThrowIfDisposed(); return _granules.Length == 0 || _granules[^1] < 0 ? 0 : _granules[^1] / (double)_rate; } }
    /// <summary>Creates independent caller-owned packet reading state.</summary>
    /// <returns>A version-bound decoder cursor borrowing this sequence.</returns>
    /// <exception cref="ObjectDisposedException">The sequence is disposed.</exception>
    public OggPacketSequencePlayback InstantiatePlayback() { ThrowIfDisposed(); return new(this); }
    internal (byte[][][] Pages, long[] Granules, float Rate, long Version) Capture() { lock (_gate) { ThrowIfDisposed(); return (_pages, _granules, _rate, _version); } }
    internal long Version { get { lock (_gate) { ThrowIfDisposed(); return _version; } } }
    private static byte[][][] CopyPages(byte[][][] pages)
    {
        ArgumentNullException.ThrowIfNull(pages); var result = new byte[pages.Length][][];
        for (var i = 0; i < pages.Length; i++) { ArgumentNullException.ThrowIfNull(pages[i]); result[i] = new byte[pages[i].Length][]; for (var j = 0; j < pages[i].Length; j++) { ArgumentNullException.ThrowIfNull(pages[i][j]); result[i][j] = (byte[])pages[i][j].Clone(); } }
        return result;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new OggPacketSequence();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var snapshot = Capture(); var other = (OggPacketSequence)target; other.PacketData = snapshot.Pages; other.GranulePositions = snapshot.Granules; other.SamplingRate = snapshot.Rate;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _pages = []; _granules = []; _version++; } base.Dispose(disposing); }
}
