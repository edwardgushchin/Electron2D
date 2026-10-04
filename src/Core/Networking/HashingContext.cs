using System.Security.Cryptography;
namespace Electron2D;

/// <summary>Selects the digest contract shared by hashing, signatures and HMAC operations.</summary>
public enum HashType
{
    /// <summary>MD5 produces 16 bytes; provided for existing digest contracts.</summary>
    MD5 = 0,
    /// <summary>SHA-1 produces 20 bytes.</summary>
    SHA1 = 1,
    /// <summary>SHA-256 produces 32 bytes.</summary>
    SHA256 = 2
}

/// <summary>Computes MD5, SHA-1 or SHA-256 incrementally over caller-owned chunks.</summary>
/// <remarks>Start prepares a native-backed BCL context. Update borrows its span and allocates no engine buffers.
/// Finish closes the current computation; a too-small destination preserves it. Operations/disposal require the
/// constructing thread. Snapshot results allocate; Start is preparation and native provider costs are external.</remarks>
public class HashingContext : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private CryptoDigestState _state;
    /// <summary>Creates an idle hashing context.</summary>
    public HashingContext() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Hashing requires its constructing thread."); }
    /// <summary>Starts a digest computation.</summary><param name="type">MD5, SHA1 or SHA256.</param>
    /// <exception cref="InvalidOperationException">A computation is already active.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The selector is invalid.</exception>
    public void Start(HashType type) { Check(); _state.Start(type, false, ReadOnlySpan<byte>.Empty); }
    /// <summary>Appends one nonempty chunk.</summary><param name="chunk">Bytes borrowed only during this call.</param>
    /// <exception cref="ArgumentException">The chunk is empty.</exception>
    /// <exception cref="InvalidOperationException">No computation is active.</exception>
    public void Update(ReadOnlySpan<byte> chunk) { Check(); _state.Require(); if (chunk.IsEmpty) throw new ArgumentException("Hash chunks must be nonempty.", nameof(chunk)); _state.Update(chunk); }
    /// <summary>Closes the current digest and returns copied bytes.</summary><returns>16, 20 or 32 bytes for the selected algorithm.</returns>
    public byte[] Finish() { Check(); var output = new byte[_state.Length]; Finish(output); return output; }
    /// <summary>Closes the current digest into caller storage.</summary><param name="destination">At least the selected digest length.</param><returns>Bytes written.</returns>
    /// <exception cref="ArgumentException">Storage is too small; the computation remains active.</exception>
    public int Finish(Span<byte> destination) { Check(); return _state.Finish(destination); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) _state.Dispose(); base.Dispose(disposing); }
}

/// <summary>Computes HMAC-SHA-1 or HMAC-SHA-256 over incremental message spans.</summary>
/// <remarks>Start prepares a native-backed context with a copied nonempty key. Update requires nonempty chunks.
/// Finish releases the computation before reuse. Calls/disposal require the constructing thread; prepared updates
/// and caller-span finalization reuse storage, while Start and snapshots are explicit allocating work.</remarks>
public class HMACContext : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private CryptoDigestState _state;
    /// <summary>Creates an idle HMAC context.</summary>
    public HMACContext() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("HMAC requires its constructing thread."); }
    /// <summary>Starts an HMAC computation with a copied key.</summary><param name="hashType">SHA1 or SHA256; MD5 is not part of this HMAC contract.</param><param name="key">Nonempty key borrowed during preparation.</param>
    /// <exception cref="InvalidOperationException">A computation is already active.</exception>
    /// <exception cref="NotSupportedException">MD5 is selected.</exception>
    /// <exception cref="ArgumentException">The key is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The selector is invalid.</exception>
    public void Start(HashType hashType, ReadOnlySpan<byte> key) { Check(); _state.Start(hashType, true, key); }
    /// <summary>Appends a nonempty message chunk.</summary><param name="data">Bytes borrowed only during the call.</param>
    /// <exception cref="ArgumentException">The chunk is empty.</exception>
    /// <exception cref="InvalidOperationException">No computation is active.</exception>
    public void Update(ReadOnlySpan<byte> data) { Check(); _state.Require(); if (data.IsEmpty) throw new ArgumentException("HMAC chunks must be nonempty.", nameof(data)); _state.Update(data); }
    /// <summary>Closes HMAC and returns copied authentication bytes.</summary><returns>20 or 32 bytes.</returns>
    public byte[] Finish() { Check(); var output = new byte[_state.Length]; Finish(output); return output; }
    /// <summary>Closes HMAC into caller storage.</summary><param name="destination">At least the selected digest length.</param><returns>Bytes written.</returns>
    /// <exception cref="ArgumentException">Storage is too small; the computation remains active.</exception>
    public int Finish(Span<byte> destination) { Check(); return _state.Finish(destination); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) _state.Dispose(); base.Dispose(disposing); }
}

internal struct CryptoDigestState
{
    private IncrementalHash? _hash;
    internal readonly int Length => Require().HashLengthInBytes;
    internal readonly IncrementalHash Require() => _hash ?? throw new InvalidOperationException("Start a digest computation first.");
    internal void Start(HashType type, bool hmac, ReadOnlySpan<byte> key)
    {
        if (_hash is not null) throw new InvalidOperationException("Finish the active digest before restarting.");
        if (hmac && key.IsEmpty) throw new ArgumentException("HMAC requires a nonempty key.", nameof(key));
        var algorithm = CryptoAlgorithms.Name(type); if (hmac && type == HashType.MD5) throw new NotSupportedException("HMAC supports SHA1 and SHA256.");
        _hash = hmac ? IncrementalHash.CreateHMAC(algorithm, key) : IncrementalHash.CreateHash(algorithm);
    }
    internal readonly void Update(ReadOnlySpan<byte> bytes) => Require().AppendData(bytes);
    internal int Finish(Span<byte> destination)
    {
        var hash = Require(); if (destination.Length < hash.HashLengthInBytes) throw new ArgumentException("Digest storage is too small.", nameof(destination));
        try { return hash.GetHashAndReset(destination); } finally { Dispose(); }
    }
    internal void Dispose() { _hash?.Dispose(); _hash = null; }
}

internal static class CryptoAlgorithms
{
    internal static HashAlgorithmName Name(HashType type) => type switch { HashType.MD5 => HashAlgorithmName.MD5, HashType.SHA1 => HashAlgorithmName.SHA1, HashType.SHA256 => HashAlgorithmName.SHA256, _ => throw new ArgumentOutOfRangeException(nameof(type)) };
    internal static void ValidateDigest(HashType type, ReadOnlySpan<byte> hash) { _ = Name(type); var length = type == HashType.MD5 ? 16 : type == HashType.SHA1 ? 20 : 32; if (hash.Length != length) throw new ArgumentException("Digest length does not match its selected algorithm.", nameof(hash)); }
}
