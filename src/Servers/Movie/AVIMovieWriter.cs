using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class AVIMovieWriter : MovieWriter
{
    private FileAccess? _file;
    private readonly List<(uint ID, uint Offset, uint Size)> _index = [];
    private byte[] _audio = [];
    private Vector2i _size;
    private int _fps, _rate, _channels, _bits;
    private float _quality;
    private uint _frames, _samples, _videoBuffer, _audioBuffer;
    private long _dataEnd = 324;
    private bool _headerReady;
    protected override int OnGetAudioMixRate() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterMixRate);
    protected override AudioServer.SpeakerMode OnGetAudioSpeakerMode() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterSpeakerMode);
    protected override string[] OnGetSupportedExtensions() => ["avi"];
    protected override bool OnHandlesFile(string path) => System.IO.Path.GetExtension(path).Equals(".avi", StringComparison.OrdinalIgnoreCase);
    protected override void OnWriteBegin(Vector2i movieSize, int fps, string basePath)
    {
        // Stream rectangles have signed 16-bit coordinates; bitmap dimensions are signed 32-bit.
        if (movieSize.X is <= 0 or > short.MaxValue || movieSize.Y is <= 0 or > short.MaxValue)
            throw new NotSupportedException("AVI dimensions must fit a positive signed 16-bit stream rectangle.");
        _size = movieSize; _fps = fps;
        _rate = (int)AudioServer.GetMixRate(); _channels = 2 + (int)AudioServer.GetSpeakerMode() * 2;
        _bits = ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterAudioBitDepth);
        _quality = Math.Max(.01f, ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterVideoQuality));
        _audio = new byte[checked((int)(((long)_rate + fps - 1) / fps) * _channels * (_bits / 8))];
        _file = FileAccess.Open(basePath, FileAccessModeFlags.WriteRead);
        WriteHeader(_dataEnd); _headerReady = true;
    }
    private void WriteHeader(long end)
    {
        Span<byte> h = stackalloc byte[324]; h.Clear();
        "RIFF"u8.CopyTo(h); Put(h, 4, checked((uint)(end - 8))); "AVI LIST"u8.CopyTo(h[8..]); Put(h, 16, 292);
        "hdrlavih"u8.CopyTo(h[20..]); Put(h, 28, 56); Put(h, 32, (uint)(1000000 / _fps));
        Put(h, 44, 0x110); Put(h, 48, _frames); Put(h, 56, 2); Put(h, 60, Math.Max(_videoBuffer, _audioBuffer));
        Put(h, 64, (uint)_size.X); Put(h, 68, (uint)_size.Y);
        "LIST"u8.CopyTo(h[88..]); Put(h, 92, 116); "strlstrh"u8.CopyTo(h[96..]); Put(h, 104, 56);
        "vidsMJPG"u8.CopyTo(h[108..]); Put(h, 128, 1); Put(h, 132, (uint)_fps); Put(h, 140, _frames);
        Put(h, 144, _videoBuffer); Put(h, 148, uint.MaxValue);
        BinaryPrimitives.WriteInt16LittleEndian(h[160..], (short)_size.X); BinaryPrimitives.WriteInt16LittleEndian(h[162..], (short)_size.Y);
        "strf"u8.CopyTo(h[164..]); Put(h, 168, 40); Put(h, 172, 40); Put(h, 176, (uint)_size.X); Put(h, 180, (uint)_size.Y);
        BinaryPrimitives.WriteInt16LittleEndian(h[184..], 1); BinaryPrimitives.WriteInt16LittleEndian(h[186..], 24);
        "MJPG"u8.CopyTo(h[188..]); Put(h, 192, checked((uint)((((long)_size.X * 3 + 3) & ~3L) * _size.Y)));
        "LIST"u8.CopyTo(h[212..]); Put(h, 216, 92); "strlstrh"u8.CopyTo(h[220..]); Put(h, 228, 56);
        "auds"u8.CopyTo(h[232..]); var align = _channels * (_bits / 8);
        Put(h, 252, (uint)align); Put(h, 256, (uint)(_rate * align)); Put(h, 264, _samples); Put(h, 268, _audioBuffer);
        Put(h, 272, uint.MaxValue); Put(h, 276, (uint)align);
        "strf"u8.CopyTo(h[288..]); Put(h, 292, 16);
        BinaryPrimitives.WriteInt16LittleEndian(h[296..], 1); BinaryPrimitives.WriteInt16LittleEndian(h[298..], (short)_channels);
        Put(h, 300, (uint)_rate); Put(h, 304, (uint)(_rate * align));
        BinaryPrimitives.WriteInt16LittleEndian(h[308..], (short)align); BinaryPrimitives.WriteInt16LittleEndian(h[310..], (short)_bits);
        "LIST"u8.CopyTo(h[312..]); Put(h, 316, checked((uint)(_dataEnd - 320))); "movi"u8.CopyTo(h[320..]);
        var stream = _file!.BorrowStream; stream.Position = 0; stream.Write(h);
    }
    private static void Put(Span<byte> buffer, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], value);
    internal static long CheckFrameSize(long dataEnd, int indexEntries, int jpegBytes, int pcmBytes)
    {
        var next = checked(dataEnd + 16 + jpegBytes + (jpegBytes & 1) + pcmBytes);
        if (next + 8 + ((long)indexEntries + 2) * 16 - 8 > uint.MaxValue)
            throw new IOException("AVI exceeds the RIFF size limit, including its index.");
        return next;
    }
    protected override void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)
    {
        var jpeg = frameImage.SaveJPGToBuffer(_quality);
        var bytes = checked(audioFrameBlock.Length * (_bits / 8));
        var next = CheckFrameSize(_dataEnd, _index.Count, jpeg.Length, bytes);
        var samples = checked(_samples + (uint)(audioFrameBlock.Length / _channels));
        _index.EnsureCapacity(checked(_index.Count + 2));
        var audio = _audio.AsSpan(0, bytes);
        if (_bits == 32 && BitConverter.IsLittleEndian) MemoryMarshal.AsBytes(audioFrameBlock).CopyTo(audio);
        else for (var i = 0; i < audioFrameBlock.Length; i++)
                if (_bits == 16) BinaryPrimitives.WriteInt16LittleEndian(audio[(i * 2)..], (short)(audioFrameBlock[i] >> 16));
                else BinaryPrimitives.WriteInt32LittleEndian(audio[(i * 4)..], audioFrameBlock[i]);
        var stream = _file!.BorrowStream; stream.Position = _dataEnd;
        WriteChunk(stream, "00dc"u8, jpeg); WriteChunk(stream, "01wb"u8, audio);
        _index.Add((0x63643030, (uint)(_dataEnd - 320), (uint)jpeg.Length));
        _index.Add((0x62773130, (uint)(_dataEnd - 320 + 8 + jpeg.Length + (jpeg.Length & 1)), (uint)bytes));
        _dataEnd = next; _samples = samples; _frames++;
        _videoBuffer = Math.Max(_videoBuffer, (uint)jpeg.Length); _audioBuffer = Math.Max(_audioBuffer, (uint)bytes);
    }
    internal static void WriteChunk(Stream stream, ReadOnlySpan<byte> id, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[8]; id.CopyTo(header); Put(header, 4, (uint)data.Length);
        stream.Write(header); stream.Write(data); if ((data.Length & 1) != 0) stream.WriteByte(0);
    }
    protected override void OnWriteEnd()
    {
        if (_file is not { } file) return;
        try
        {
            if (!_headerReady) return;
            var stream = file.BorrowStream;
            // Discard a failed partial frame before finalizing the last complete frame pair.
            stream.SetLength(_dataEnd); stream.Position = _dataEnd;
            Span<byte> entry = stackalloc byte[16]; "idx1"u8.CopyTo(entry); Put(entry, 4, checked((uint)_index.Count * 16)); stream.Write(entry[..8]);
            foreach (var item in _index)
            { Put(entry, 0, item.ID); Put(entry, 4, 16); Put(entry, 8, item.Offset); Put(entry, 12, item.Size); stream.Write(entry); }
            WriteHeader(stream.Position);
        }
        finally { _file = null; file.Dispose(); }
    }
}
