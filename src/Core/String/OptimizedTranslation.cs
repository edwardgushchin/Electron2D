using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Electron2D;

/// <summary>Stores translations by source-text hash with compressed translated values.</summary>
/// <remarks>The source texts are not retained. Generation is an asset-authoring operation; lookup works at runtime.
/// Contextual source entries are skipped; plural entries retain only their first form. Compressed values are decoded once and cached on first lookup.</remarks>
public sealed class OptimizedTranslation : Translation
{
    private readonly object _gate = new();
    private Dictionary<Guid, CompressedMessage> _messagesByHash = [];

    /// <summary>Creates an empty optimized translation catalog.</summary>
    public OptimizedTranslation() { }

    /// <summary>Generates a compressed catalog from singular messages without contexts.</summary>
    /// <param name="source">The source catalog. Null or a catalog without eligible messages leaves this catalog unchanged.</param>
    /// <returns>True when at least one message was generated; otherwise false.</returns>
    /// <remarks>The source catalog may be edited or disposed after generation. This method allocates and is intended for asset authoring.</remarks>
    /// <exception cref="ObjectDisposedException">This catalog or the source catalog is disposed.</exception>
    /// <exception cref="Exception">Source lookup or a change observer fails; a failed observer does not roll back generated content.</exception>
    public bool Generate(Translation? source)
    {
        ThrowIfDisposed();
        if (source is null) return false;

        var messages = new Dictionary<Guid, CompressedMessage>();
        foreach (var key in source.GetMessageList())
        {
            if (key.Contains('\u0004')) continue;
            messages[HashKey(key)] = Compress(source.GetMessage(key));
        }
        if (messages.Count == 0) return false;

        var locale = source.Locale;
        lock (_gate)
        {
            ThrowIfDisposed();
            _messagesByHash = messages;
        }
        Locale = locale;
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
        lock (_gate) { ThrowIfDisposed(); entries = [.. _messagesByHash.Values]; }
        return [.. entries.Select(Decode)];
    }

    /// <inheritdoc />
    protected override string? OnGetMessage(string source, string context)
    {
        CompressedMessage? entry;
        lock (_gate)
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
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        var copy = (OptimizedTranslation)target;
        Dictionary<Guid, CompressedMessage> snapshot;
        lock (_gate)
        {
            ThrowIfDisposed();
            snapshot = new Dictionary<Guid, CompressedMessage>(_messagesByHash);
        }
        lock (copy._gate) copy._messagesByHash = snapshot;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _messagesByHash.Clear();
        base.Dispose(disposing);
    }

    private static Guid HashKey(string source)
    {
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(MemoryMarshal.AsBytes(source.AsSpan()), digest);
        return new Guid(digest[..16]);
    }

    private static CompressedMessage Compress(string value)
    {
        var raw = Encoding.UTF8.GetBytes(value);
        if (raw.Length == 0) return new CompressedMessage(raw, 0, value);
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        if (BrotliEncoder.TryCompress(raw, compressed, out var size, quality: 5, window: 22) && size < raw.Length)
        {
            Array.Resize(ref compressed, size);
            return new CompressedMessage(compressed, raw.Length, null);
        }
        return new CompressedMessage(raw, raw.Length, value);
    }

    private static string Decode(CompressedMessage entry)
    {
        var cached = Volatile.Read(ref entry.Decoded);
        if (cached is not null) return cached;
        var raw = new byte[entry.OriginalLength];
        if (!BrotliDecoder.TryDecompress(entry.Bytes, raw, out var written) || written != raw.Length)
            throw new InvalidDataException("The optimized translation payload is invalid.");
        var decoded = Encoding.UTF8.GetString(raw);
        return Interlocked.CompareExchange(ref entry.Decoded, decoded, null) ?? decoded;
    }

    private sealed class CompressedMessage(byte[] bytes, int originalLength, string? decoded)
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly int OriginalLength = originalLength;
        internal string? Decoded = decoded;
    }
}
