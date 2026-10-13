namespace Electron2D;

/// <summary>Defines the common identity and options for image-file decoders.</summary>
/// <remarks>Implement <see cref="ImageFormatLoaderExtension"/> to register a caller-owned decoder.
/// Registration and file decoding are preparation operations and may allocate.</remarks>
public abstract class ImageFormatLoader : ElectronObject
{
    private protected ImageFormatLoader() { }

    /// <summary>Defines options passed to an image decoder.</summary>
    [Flags]
    public enum LoaderFlags
    {
        /// <summary>Uses the decoder's ordinary interpretation.</summary>
        None = 0,
        /// <summary>Requests linear pixels when the format supports color interpretation.</summary>
        ForceLinear = 1,
        /// <summary>Requests conversion of the format's colors.</summary>
        ConvertColors = 2,
    }
}
