using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace Electron2D;

public sealed partial class OptimizedTranslation
{
    internal const string CatalogPropertyName = "_optimized_catalog";
    private const int CatalogVersion = 1;
    private const int MaximumMessages = 65536;
    private const int MaximumValueBytes = 1024 * 1024;
    private const int MaximumExpandedBytes = 32 * 1024 * 1024;
    private const int EntryHeaderBytes = 16 + sizeof(int) * 2;
    private static readonly UTF8Encoding CatalogUTF8 = new(false, true);

    internal byte[] SaveCatalog()
    {
        string locale;
        KeyValuePair<Guid, CompressedMessage>[] entries;
        lock (CatalogGate)
        {
            ThrowIfDisposed();
            locale = Locale;
            entries = [.. _messagesByHash];
        }

        using var stream = new StreamPeerBuffer();
        stream.Put32(CatalogVersion);
        ResourceArchiveStrings.Write(stream, locale);
        stream.Put32(entries.Length);
        foreach (var (hash, entry) in entries)
        {
            stream.PutData(hash.ToByteArray());
            stream.Put32(entry.OriginalLength);
            stream.Put32(entry.Bytes.Length);
            stream.PutData(entry.Bytes);
        }
        return stream.DataArray;
    }

    internal void LoadCatalog(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();
        if (data.Length < 12 || data.Length > 12 + 256 * 2 + MaximumMessages * EntryHeaderBytes + MaximumExpandedBytes)
            throw new InvalidDataException("Invalid optimized catalog size.");

        using var stream = new StreamPeerBuffer { DataArray = data };
        if (stream.Get32() != CatalogVersion)
            throw new InvalidDataException("Unknown optimized catalog version.");
        var localeLength = stream.Get32();
        if (localeLength < 0 || localeLength > 256 || localeLength > (stream.GetAvailableBytes() - sizeof(int)) / 2)
            throw new InvalidDataException("Invalid optimized catalog locale length.");
        var locale = new char[localeLength];
        for (var i = 0; i < locale.Length; i++) locale[i] = (char)stream.GetU16();
        var count = stream.Get32();
        if (count < 0 || count > MaximumMessages || count > stream.GetAvailableBytes() / EntryHeaderBytes)
            throw new InvalidDataException("Invalid optimized catalog message count.");

        var messages = new Dictionary<Guid, CompressedMessage>(count);
        var remaining = MaximumExpandedBytes;
        try
        {
            for (var i = 0; i < count; i++)
            {
                if (stream.GetAvailableBytes() < EntryHeaderBytes)
                    throw new InvalidDataException("Truncated optimized catalog entry.");
                var hash = new Guid(stream.GetData(16));
                var expandedLength = stream.Get32();
                var storedLength = stream.Get32();
                if (expandedLength < 0 || expandedLength > MaximumValueBytes || expandedLength > remaining
                    || storedLength < 0 || storedLength > expandedLength || storedLength > stream.GetAvailableBytes()
                    || (storedLength == 0 && expandedLength != 0))
                    throw new InvalidDataException("Invalid optimized catalog value size.");
                if (messages.ContainsKey(hash))
                    throw new InvalidDataException("Duplicate optimized catalog key.");

                remaining -= expandedLength;
                var bytes = stream.GetData(storedLength);
                var raw = storedLength == expandedLength ? bytes : Expand(bytes, expandedLength);
                _ = CatalogUTF8.GetCharCount(raw);
                messages.Add(hash, new CompressedMessage(bytes, expandedLength, expandedLength == 0 ? "" : null));
            }
            if (stream.GetAvailableBytes() != 0)
                throw new InvalidDataException("Trailing optimized catalog bytes.");
            lock (CatalogGate)
            {
                SetCatalogLocale(new string(locale));
                _messagesByHash = messages;
            }
        }
        catch (ArgumentException error)
        {
            throw new InvalidDataException("Invalid locale or text in optimized catalog.", error);
        }
        EmitChanged();
    }

    private static byte[] Expand(byte[] bytes, int length)
    {
        var raw = new byte[length];
        using var decoder = new BrotliDecoder();
        if (decoder.Decompress(bytes, raw, out var consumed, out var written) != OperationStatus.Done
            || consumed != bytes.Length || written != length)
            throw new InvalidDataException("Invalid optimized translation Brotli value.");
        return raw;
    }
}
