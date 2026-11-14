using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Electron2D;

/// <summary>Stores translations by source-text hash with compressed translated values.</summary>
/// <remarks>The generated catalog can be saved with <see cref="ResourceSaver"/> and loaded with <see cref="ResourceLoader"/>.
/// Generation is an explicit asset-authoring operation available in the shared engine assembly.
/// Contextual source entries are skipped; plural entries retain only their first form. Compressed values are decoded once and cached on first lookup.</remarks>
public sealed partial class OptimizedTranslation : Translation
{
    private Dictionary<Guid, CompressedMessage> _messagesByHash = [];

    /// <summary>Creates an empty optimized translation catalog.</summary>
    public OptimizedTranslation() { }

    /// <summary>Generates a compressed catalog from the first translated form of messages without contexts.</summary>
    /// <param name="source">The source catalog. Null or a catalog without eligible messages leaves this catalog unchanged.</param>
    /// <returns>True when at least one message was generated; otherwise false.</returns>
    /// <remarks>The source catalog may be edited or disposed after generation. This method allocates and is intended for asset authoring.</remarks>
    /// <exception cref="ObjectDisposedException">This catalog or the source catalog is disposed.</exception>
    /// <exception cref="InvalidDataException">The catalog exceeds its budget or two source keys share a hash.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">A translated value contains invalid Unicode.</exception>
    /// <exception cref="Exception">Source lookup or a change observer fails; a failed observer does not roll back generated content.</exception>
    public bool Generate(Translation? source)
    {
        ThrowIfDisposed();
        if (source is null) return false;

        var messages = new Dictionary<Guid, CompressedMessage>();
        var remaining = MaximumExpandedBytes;
        foreach (var key in source.GetMessageList())
        {
            if (key.Contains('\u0004')) continue;
            var value = source.GetMessage(key);
            var length = CatalogUTF8.GetByteCount(value);
            if (messages.Count == MaximumMessages || length > MaximumValueBytes || length > remaining)
                throw new InvalidDataException("Optimized translation exceeds its message or byte budget.");
            remaining -= length;
            if (!messages.TryAdd(HashKey(key), Compress(value, length)))
                throw new InvalidDataException("Optimized translation source keys share a hash.");
        }
        if (messages.Count == 0) return false;

        var locale = source.Locale;
        lock (CatalogGate)
        {
            SetCatalogLocale(locale);
            _messagesByHash = messages;
        }
        EmitChanged();
        return true;
    }

    /// <inheritdoc />
    public override int GetMessageCount() { ThrowIfDisposed(); return 0; }

    /// <inheritdoc />
    public override string[] GetMessageList() { ThrowIfDisposed(); return []; }

    /// <inheritdoc />
    public override string[] GetTranslatedMessageList()
    {
        CompressedMessage[] entries;
        lock (CatalogGate) { ThrowIfDisposed(); entries = [.. _messagesByHash.Values]; }
        return [.. entries.Select(Decode)];
    }

    /// <inheritdoc />
    protected override string? OnGetMessage(string source, string context)
    {
        CompressedMessage? entry;
        lock (CatalogGate)
        {
            ThrowIfDisposed();
            if (!_messagesByHash.TryGetValue(HashKey(source), out entry)) return string.Empty;
        }
        return Decode(entry);
    }

    /// <inheritdoc />
    protected override string? OnGetPluralMessage(string source, string plural, long count, string context) =>
        OnGetMessage(source, context);

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new OptimizedTranslation();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (OptimizedTranslation)target;
        CatalogCopy baseState;
        Dictionary<Guid, CompressedMessage> snapshot;
        lock (CatalogGate)
        {
            baseState = CaptureCatalogCopy();
            snapshot = new Dictionary<Guid, CompressedMessage>(_messagesByHash);
        }
        lock (copy.CatalogGate)
        {
            copy.RestoreCatalogCopy(baseState);
            copy._messagesByHash = snapshot;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (CatalogGate) _messagesByHash.Clear();
        base.Dispose(disposing);
    }

    private static Guid HashKey(string source)
    {
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(MemoryMarshal.AsBytes(source.AsSpan()), digest);
        return new Guid(digest[..16]);
    }

    private static CompressedMessage Compress(string value, int length)
    {
        var raw = CatalogUTF8.GetBytes(value);
        if (raw.Length == 0) return new CompressedMessage(raw, 0, value);
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        if (BrotliEncoder.TryCompress(raw, compressed, out var size, quality: 5, window: 22) && size < raw.Length)
        {
            Array.Resize(ref compressed, size);
            return new CompressedMessage(compressed, length, null);
        }
        return new CompressedMessage(raw, length, value);
    }

    private static string Decode(CompressedMessage entry)
    {
        var cached = Volatile.Read(ref entry.Decoded);
        if (cached is not null) return cached;
        var decoded = entry.Bytes.Length == entry.OriginalLength
            ? CatalogUTF8.GetString(entry.Bytes)
            : CatalogUTF8.GetString(Expand(entry.Bytes, entry.OriginalLength));
        return Interlocked.CompareExchange(ref entry.Decoded, decoded, null) ?? decoded;
    }

    private sealed class CompressedMessage(byte[] bytes, int originalLength, string? decoded)
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly int OriginalLength = originalLength;
        internal string? Decoded = decoded;
    }
}
