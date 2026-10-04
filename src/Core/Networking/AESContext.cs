using System.Security.Cryptography;
namespace Electron2D;

/// <summary>Selects a raw AES encryption/decryption and chaining contract.</summary>
public enum AESMode
{
    /// <summary>Encrypts independent 16-byte blocks with ECB.</summary>
    ECBEncrypt = 0,
    /// <summary>Decrypts independent 16-byte blocks with ECB.</summary>
    ECBDecrypt = 1,
    /// <summary>Encrypts chained 16-byte blocks with CBC and a caller IV.</summary>
    CBCEncrypt = 2,
    /// <summary>Decrypts chained 16-byte blocks with CBC and a caller IV.</summary>
    CBCDecrypt = 3,
    /// <summary>Bounds the domain; cannot select an operation.</summary>
    Max = 4
}

/// <summary>Incrementally encrypts/decrypts complete AES-128/AES-256 ECB or CBC blocks without padding.</summary>
/// <remarks>Start prepares the BCL transform and copied key/IV. Updates require whole 16-byte blocks; no padding
/// or authentication is added. Caller-span updates and IV reads reuse storage. Finish clears engine buffers and
/// releases the transform/key schedule. Calls and disposal require the constructing thread.</remarks>
public class AESContext : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private Aes? _aes;
    private ICryptoTransform? _transform;
    private AESMode _mode = AESMode.Max;
    private readonly byte[] _iv = new byte[16], _input = new byte[16], _output = new byte[16];
    /// <summary>Creates an idle AES context with prepared block/IV buffers.</summary>
    public AESContext() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("AES requires its constructing thread."); }
    /// <summary>Starts a raw AES stream with a copied key and CBC IV.</summary><param name="mode">An ECB/CBC encrypt/decrypt selector.</param><param name="key">Exactly 16 or 32 bytes.</param><param name="iv">Exactly 16 bytes for CBC; ignored for ECB.</param>
    /// <exception cref="ArgumentException">Key/IV lengths do not match the mode.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">Finish has not closed the previous stream.</exception>
    public void Start(AESMode mode, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv = default)
    {
        Check(); if (_transform is not null) throw new InvalidOperationException("Finish the active AES context first."); if ((uint)mode >= 4) throw new ArgumentOutOfRangeException(nameof(mode));
        if (key.Length is not (16 or 32)) throw new ArgumentException("AES keys must contain 16 or 32 bytes.", nameof(key)); var cbc = mode is AESMode.CBCEncrypt or AESMode.CBCDecrypt; if (cbc && iv.Length != 16) throw new ArgumentException("CBC requires a 16-byte IV.", nameof(iv));
        var aes = Aes.Create(); var copiedKey = key.ToArray(); ICryptoTransform? transform = null;
        try { aes.Mode = cbc ? CipherMode.CBC : CipherMode.ECB; aes.Padding = PaddingMode.None; aes.Key = copiedKey; aes.IV = cbc ? iv.ToArray() : new byte[16]; transform = mode is AESMode.ECBEncrypt or AESMode.CBCEncrypt ? aes.CreateEncryptor() : aes.CreateDecryptor(); _aes = aes; _transform = transform; _mode = mode; if (cbc) iv.CopyTo(_iv); }
        catch { transform?.Dispose(); aes.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(copiedKey); }
    }
    /// <summary>Transforms complete blocks into a copied result.</summary><param name="source">A multiple of 16 bytes; empty is valid.</param><returns>Exactly the source length.</returns>
    public byte[] Update(ReadOnlySpan<byte> source) { Check(); Validate(source, source.Length); var result = new byte[source.Length]; Update(source, result); return result; }
    /// <summary>Transforms complete blocks into caller storage.</summary><param name="source">Whole blocks, borrowed during the call.</param><param name="destination">Enough storage; exact in-place overlap is supported.</param><returns>Bytes written, equal to source length.</returns>
    /// <exception cref="ArgumentException">Input is not block aligned, storage is too small or buffers partially overlap; stream state is preserved.</exception>
    /// <exception cref="InvalidOperationException">No AES stream is active.</exception>
    public int Update(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        Check(); Validate(source, destination.Length); if (source.Overlaps(destination, out var offset) && offset != 0) throw new ArgumentException("Only exact in-place buffer overlap is supported.", nameof(destination));
        try
        {
            for (var i = 0; i < source.Length; i += 16)
            {
                source.Slice(i, 16).CopyTo(_input); var count = _transform!.TransformBlock(_input, 0, 16, _output, 0); if (count != 16) throw new CryptographicException("AES transform returned an incomplete block.");
                _output.CopyTo(destination[i..]); if (_mode == AESMode.CBCEncrypt) _output.CopyTo(_iv, 0); else if (_mode == AESMode.CBCDecrypt) _input.CopyTo(_iv, 0);
            }
            return source.Length;
        }
        catch { FinishCore(); throw; }
        finally { CryptographicOperations.ZeroMemory(_input); CryptographicOperations.ZeroMemory(_output); }
    }
    private void Validate(ReadOnlySpan<byte> source, int available) { if (_transform is null) throw new InvalidOperationException("Start AES before updating."); if (source.Length % 16 != 0 || available < source.Length) throw new ArgumentException("AES input must be block aligned and output storage sufficient.", nameof(source)); }
    /// <summary>Gets a copied current CBC chaining IV.</summary><returns>16 bytes; encryption tracks last ciphertext output and decryption last ciphertext input.</returns>
    public byte[] GetIVState() { Check(); var result = new byte[16]; GetIVState(result); return result; }
    /// <summary>Copies the current CBC chaining IV.</summary><param name="destination">At least 16 bytes.</param><returns>16.</returns>
    /// <exception cref="InvalidOperationException">No CBC context is active.</exception>
    /// <exception cref="ArgumentException">Output storage is too small.</exception>
    public int GetIVState(Span<byte> destination) { Check(); if (_transform is null || _mode is not (AESMode.CBCEncrypt or AESMode.CBCDecrypt)) throw new InvalidOperationException("IV state requires active CBC."); if (destination.Length < 16) throw new ArgumentException("IV storage requires 16 bytes.", nameof(destination)); _iv.CopyTo(destination); return 16; }
    /// <summary>Releases the stream and clears IV/block/key storage; safe while idle.</summary>
    public void Finish() { Check(); FinishCore(); }
    private void FinishCore() { var transform = _transform; var aes = _aes; _transform = null; _aes = null; _mode = AESMode.Max; try { transform?.Dispose(); } finally { aes?.Dispose(); CryptographicOperations.ZeroMemory(_iv); CryptographicOperations.ZeroMemory(_input); CryptographicOperations.ZeroMemory(_output); } }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) FinishCore(); base.Dispose(disposing); }
}
