using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class PNGMovieWriter : MovieWriter
{
    private FileAccess? _wav;
    private string _prefix = "";
    private long _frame, _bytes;
    private int _rate, _channels;
    protected override int OnGetAudioMixRate() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterMixRate);
    protected override AudioServer.SpeakerMode OnGetAudioSpeakerMode() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterSpeakerMode);
    protected override string[] OnGetSupportedExtensions() => ["png"];
    protected override bool OnHandlesFile(string path) => System.IO.Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase);
    protected override void OnWriteBegin(Vector2i movieSize, int fps, string basePath)
    {
        _prefix = basePath[..^4]; _frame = _bytes = 0;
        _rate = (int)AudioServer.GetMixRate(); _channels = 2 + (int)AudioServer.GetSpeakerMode() * 2;
        _wav = FileAccess.Open(_prefix + ".wav", FileAccessModeFlags.WriteRead);
        WriteHeader();
        var absolute = ProjectSettings.GlobalizePath(_prefix);
        var directory = System.IO.Path.GetDirectoryName(absolute)!;
        var prefix = System.IO.Path.GetFileName(absolute);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var name = System.IO.Path.GetFileName(file);
            if (name.Length == prefix.Length + 12 && name.StartsWith(prefix, StringComparison.Ordinal) &&
                name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && name.AsSpan(prefix.Length, 8).IndexOfAnyExceptInRange('0', '9') < 0)
                File.Delete(file);
        }
    }
    private void WriteHeader()
    {
        Span<byte> header = stackalloc byte[44]; header.Clear();
        "RIFF"u8.CopyTo(header); BinaryPrimitives.WriteUInt32LittleEndian(header[4..], checked((uint)_bytes + 36));
        "WAVEfmt "u8.CopyTo(header[8..]); BinaryPrimitives.WriteInt32LittleEndian(header[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(header[20..], 1); BinaryPrimitives.WriteInt16LittleEndian(header[22..], (short)_channels);
        BinaryPrimitives.WriteInt32LittleEndian(header[24..], _rate); BinaryPrimitives.WriteInt32LittleEndian(header[28..], _rate * _channels * 4);
        BinaryPrimitives.WriteInt16LittleEndian(header[32..], (short)(_channels * 4)); BinaryPrimitives.WriteInt16LittleEndian(header[34..], 32);
        "data"u8.CopyTo(header[36..]); BinaryPrimitives.WriteUInt32LittleEndian(header[40..], (uint)_bytes);
        var stream = _wav!.BorrowStream; stream.Position = 0; stream.Write(header);
    }
    protected override void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)
    {
        var count = checked(audioFrameBlock.Length * 4);
        if (_bytes + count > uint.MaxValue - 36) throw new IOException("PCM WAV exceeds the RIFF size limit.");
        frameImage.SavePNG(_prefix + _frame.ToString("D8", System.Globalization.CultureInfo.InvariantCulture) + ".png");
        if (BitConverter.IsLittleEndian) _wav!.BorrowStream.Write(MemoryMarshal.AsBytes(audioFrameBlock));
        else { Span<byte> sample = stackalloc byte[4]; foreach (var value in audioFrameBlock) { BinaryPrimitives.WriteInt32LittleEndian(sample, value); _wav!.BorrowStream.Write(sample); } }
        _bytes += count; _frame++;
    }
    protected override void OnWriteEnd()
    {
        if (_wav is not { } wav) return;
        try { WriteHeader(); } finally { _wav = null; wav.Dispose(); }
    }
}
