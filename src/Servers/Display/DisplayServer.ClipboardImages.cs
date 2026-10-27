using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private static readonly string[] ClipboardImageMIMETypes =
        ["image/png", "image/jpeg", "image/webp", "image/svg+xml", "image/bmp", "image/x-tga", "image/x-targa", "image/ktx", "image/x-exr"];

    /// <summary>Decodes an image from the ordinary system clipboard.</summary>
    /// <returns>An independent caller-owned image, or null when no supported image MIME type is offered.</returns>
    /// <remarks>Reads pixels or encoded image data, not file-manager paths. Chooses the first offered format in
    /// PNG, JPEG, WebP, SVG, BMP, TGA, KTX, EXR order. Invalid preferred data does not fall back to another format.
    /// The existing 64 MiB encoded-image and decoded-image limits apply. Wayland requires a focused client with a current clipboard offer.</remarks>
    /// <exception cref="InvalidOperationException">The service is unavailable or the call is off-owner.</exception>
    /// <exception cref="IOException">The offered native data cannot be retrieved.</exception>
    /// <exception cref="InvalidDataException">The preferred image data is invalid, empty or exceeds image limits.</exception>
    public static Image? ClipboardGetImage() => RequireService().ClipboardGetImageCore();

    /// <summary>Checks whether the ordinary clipboard's preferred supported image can be decoded.</summary>
    /// <returns>True for a valid image; false for absent, invalid or unavailable clipboard image data.</returns>
    /// <remarks>Performs the same cold read and decoding as ClipboardGetImage, disposing the temporary image.
    /// It does not promise that a later read will observe the same selection.</remarks>
    /// <exception cref="InvalidOperationException">The service is unavailable or the call is off-owner.</exception>
    public static bool ClipboardHasImage() => RequireService().ClipboardHasImageCore();

    private bool ClipboardHasImageCore()
    {
        try { using var image = ClipboardGetImageCore(); return image is not null; }
        catch (InvalidDataException) { return false; }
        catch (IOException) { return false; }
    }

    private unsafe Image? ClipboardGetImageCore()
    {
        EnsureOwner();
        foreach (var mime in ClipboardImageMIMETypes)
        {
            if (!SDL.HasClipboardData(mime)) continue;
            var data = SDL.GetClipboardData(mime, out var length);
            if (data == 0) throw new IOException("Cannot read clipboard image data: " + SDL.GetError());
            try
            {
                if (length == 0 || length > Image.MaximumEncodedBytes) throw new InvalidDataException("Clipboard images must contain between 1 byte and 64 MiB.");
                var bytes = new ReadOnlySpan<byte>((void*)data, (int)length);
                var image = new Image();
                try
                {
                    switch (mime)
                    {
                        case "image/png": image.LoadPNGFromBuffer(bytes); break;
                        case "image/jpeg": image.LoadJPGFromBuffer(bytes); break;
                        case "image/webp": image.LoadWebPFromBuffer(bytes); break;
                        case "image/svg+xml": image.LoadSVGFromBuffer(bytes); break;
                        case "image/bmp": image.LoadBMPFromBuffer(bytes); break;
                        case "image/x-tga":
                        case "image/x-targa": image.LoadTGAFromBuffer(bytes); break;
                        case "image/ktx": image.LoadKTXFromBuffer(bytes); break;
                        case "image/x-exr": image.LoadEXRFromBuffer(bytes); break;
                    }
                    return image;
                }
                catch { image.Dispose(); throw; }
            }
            finally { SDL.Free(data); }
        }
        return null;
    }
}
