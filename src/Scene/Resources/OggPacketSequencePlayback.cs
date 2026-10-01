namespace Electron2D;

/// <summary>Owns an independent captured packet-page cursor for Vorbis decoding.</summary>
/// <remarks>Created by OggPacketSequence. Packet/granule changes invalidate this cursor; disposing it releases
/// captured arrays without disposing the borrowed sequence. Packet access stays inside the decoder boundary.</remarks>
public sealed class OggPacketSequencePlayback : ElectronObject
{
    private readonly OggPacketSequence _source;
    private byte[][][] _pages;
    private long[] _granules;
    private readonly long _version;
    private int _page, _packet;
    internal OggPacketSequencePlayback(OggPacketSequence source)
    { _source = source; var snapshot = source.Capture(); _pages = snapshot.Pages; _granules = snapshot.Granules; _version = snapshot.Version; }
    internal (byte[] Data, long? Granule, bool EOS)? Peek()
    {
        ThrowIfDisposed(); if (_source.Version != _version) throw new InvalidOperationException("The packet sequence changed.");
        if (_pages.Length != _granules.Length) throw new FormatException("Packet pages and granules differ.");
        while (_page < _pages.Length && _packet == _pages[_page].Length) { _page++; _packet = 0; }
        if (_page == _pages.Length) return null;
        var last = _packet == _pages[_page].Length - 1;
        return (_pages[_page][_packet], last && _granules[_page] >= 0 ? _granules[_page] : null, last && _page == _pages.Length - 1);
    }
    internal void Advance() { if (Peek() is not null) _packet++; }
    internal long FinalGranule { get { ThrowIfDisposed(); if (_source.Version != _version) throw new InvalidOperationException("The packet sequence changed."); return _granules.Length == 0 ? -1 : _granules[^1]; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { _pages = []; _granules = []; base.Dispose(disposing); }
}
