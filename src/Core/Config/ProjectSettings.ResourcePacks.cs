namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Mounts a PCK or ZIP file into the process resource namespace.</summary>
    /// <param name="pack">Physical ordinary, res:// or user:// pack path. Nested packs must first be copied out.</param>
    /// <param name="replaceFiles">Whether new entries replace existing packed entries. Removal records always remove an existing overlay.</param>
    /// <param name="offset">Nonnegative PCK header byte offset; zero also discovers appended PCK trailers and ZIP files.</param>
    /// <returns>True after atomic publication, or false if the pack cannot be read or is invalid or unsupported.</returns>
    /// <remarks>The default PCK decryption key is 32 zero bytes. Already cached resources and open streams keep their
    /// identities. Use ResourceLoader.CacheMode.Replace or Ignore to read a replacement. Mounted entries are read-only.
    /// Sparse bundles, binary delta patches and native executable section discovery require separate export support.</remarks>
    /// <exception cref="ArgumentException">The pack path is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Offset is negative.</exception>
    public static bool LoadResourcePack(string pack, bool replaceFiles = true, long offset = 0) =>
        ResourcePacks.Runtime.Load(pack, replaceFiles, offset, new byte[32]);

    /// <summary>Mounts a resource package using an explicit PCK decryption key.</summary>
    /// <param name="pack">Physical pack file path.</param>
    /// <param name="decryptionKey">Exactly 32 borrowed bytes, copied into mount ownership before return.</param>
    /// <param name="replaceFiles">Whether packed entries replace existing overlays.</param>
    /// <param name="offset">Nonnegative PCK header byte offset.</param>
    /// <returns>True after atomic publication; false for an unreadable, corrupt or unsupported pack or directory key mismatch.</returns>
    /// <remarks>An encrypted file checksum is checked when opened. MD5/CFB provide interoperability and corruption
    /// detection, not cryptographic authenticity. The caller must authenticate the pack independently when required.</remarks>
    /// <exception cref="ArgumentException">A path is invalid or the key is not 32 bytes.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Offset is negative.</exception>
    public static bool LoadResourcePack(string pack, ReadOnlySpan<byte> decryptionKey, bool replaceFiles = true, long offset = 0) =>
        ResourcePacks.Runtime.Load(pack, replaceFiles, offset, decryptionKey);
}
