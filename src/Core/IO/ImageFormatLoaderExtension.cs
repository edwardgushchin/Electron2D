namespace Electron2D;

/// <summary>Extends file-based image loading with a caller-owned decoder.</summary>
/// <remarks>Register during initialization and retain the decoder until pending loads complete.
/// Hooks execute on the loading thread, including resource workers, and may run concurrently.
/// Removal affects subsequent snapshots; an already captured load may still invoke the decoder.
/// Do not dispose it while loads are in flight. No scene or native-render access is granted.</remarks>
public abstract class ImageFormatLoaderExtension : ImageFormatLoader
{
    /// <summary>Constructs a managed image decoder.</summary>
    protected ImageFormatLoaderExtension() { }

    /// <summary>Returns the filename extensions recognized by this decoder.</summary>
    /// <returns>Extensions without dots. The engine copies and normalizes them when queried.</returns>
    protected abstract string[] GetRecognizedExtensions();

    /// <summary>Decodes a borrowed file into a temporary image.</summary>
    /// <param name="image">Borrowed empty destination. Do not dispose or retain it.</param>
    /// <param name="fileAccess">Borrowed read-only file at position zero. Do not dispose or retain it.</param>
    /// <param name="flags">Requested format-specific color options.</param>
    /// <param name="scale">Requested decoder scale; ordinary file loads pass one.</param>
    /// <returns>True after producing a nonempty image; false when the contents are unrecognized.</returns>
    /// <remarks>Throw a typed exception for malformed recognized data or another failure. False permits
    /// the next decoder for this extension. The file is rewound and discarded image state is not published.
    /// The caller's image changes once only after success. Flags and scale are interpreted by the decoder.</remarks>
    protected abstract bool LoadImage(Image image, FileAccess fileAccess, LoaderFlags flags, float scale);

    /// <summary>Registers this decoder after the built-in image decoders.</summary>
    /// <remarks>Repeated registration is idempotent. At most 64 custom decoders may be registered.</remarks>
    /// <exception cref="ObjectDisposedException">This decoder is disposed.</exception>
    /// <exception cref="InvalidOperationException">The registration capacity is exhausted.</exception>
    public void AddFormatLoader()
    {
        ThrowIfDisposed();
        Image.AddFormatLoader(this);
    }

    /// <summary>Removes this decoder from subsequent loading and discovery snapshots.</summary>
    /// <remarks>Removal is idempotent and does not wait for loads already in flight.</remarks>
    public void RemoveFormatLoader() => Image.RemoveFormatLoader(this);

    internal string[] Extensions()
    {
        ThrowIfDisposed();
        var extensions = GetRecognizedExtensions() ?? throw new InvalidDataException("The decoder returned null extensions.");
        if (extensions.Length > 256) throw new InvalidDataException("A decoder may expose at most 256 extensions.");
        var result = new string[extensions.Length];
        for (var i = 0; i < extensions.Length; i++)
        {
            var extension = extensions[i];
            if (string.IsNullOrEmpty(extension) || extension.Length > 64 ||
                extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_' && c != '-'))
                throw new InvalidDataException("Decoder extensions must be simple filename suffixes without dots.");
            result[i] = extension.ToLowerInvariant();
        }
        return result;
    }

    internal bool Decode(Image image, FileAccess file, LoaderFlags flags, float scale)
    {
        ThrowIfDisposed();
        return LoadImage(image, file, flags, scale);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) RemoveFormatLoader();
        base.Dispose(disposing);
    }
}
