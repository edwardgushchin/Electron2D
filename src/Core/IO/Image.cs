using System.Buffers.Binary;

namespace Electron2D;

/// <summary>Stores portable 2D pixel data and provides common in-memory image processing operations.</summary>
/// <remarks>
/// Image state is held in a managed byte buffer. Reads and writes are serialized per image, mutations commit atomically,
/// and <see cref="Resource.Changed"/> is raised synchronously after a successful commit. File codecs, VRAM compression,
/// textures, and renderer handles belong to separate integration layers and are not part of this type.
/// </remarks>
public sealed partial class Image : Resource
{
    /// <summary>Gets the maximum accepted image width.</summary>
    public const int MaxWidth = 1 << 24;

    /// <summary>Gets the maximum accepted image height.</summary>
    public const int MaxHeight = 1 << 24;

    private const long MaxPixels = 268_435_456;

    private static readonly IReadOnlyList<PropertyDescriptor> ImageProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Image, int>(nameof(Width), image => image.Width),
        new PropertyDescriptor<Image, int>(nameof(Height), image => image.Height),
        new PropertyDescriptor<Image, Vector2I>(nameof(Size), image => image.Size),
        new PropertyDescriptor<Image, Format>(nameof(PixelFormat), image => image.PixelFormat),
        new PropertyDescriptor<Image, bool>(nameof(HasMipmaps), image => image.HasMipmaps),
        new PropertyDescriptor<Image, int>(nameof(DataSize), image => image.DataSize),
    ]);

    private readonly object _mutationGate = new();
    private readonly object _stateGate = new();
    private byte[] _data = [];
    private int _width;
    private int _height;
    private Format _format = Format.L8;
    private bool _hasMipmaps;

    /// <summary>Defines the byte layout used by an image.</summary>
    public enum Format
    {
        /// <summary>One normalized 8-bit luminance component.</summary>
        L8 = 0,
        /// <summary>Normalized 8-bit luminance and alpha components.</summary>
        La8 = 1,
        /// <summary>One normalized 8-bit red component.</summary>
        R8 = 2,
        /// <summary>Normalized 8-bit red and green components.</summary>
        Rg8 = 3,
        /// <summary>Normalized 8-bit red, green, and blue components.</summary>
        Rgb8 = 4,
        /// <summary>Normalized 8-bit red, green, blue, and alpha components.</summary>
        Rgba8 = 5,
        /// <summary>Packed normalized 4-bit red, green, blue, and alpha components.</summary>
        Rgba4444 = 6,
        /// <summary>Packed normalized 5-bit red, 6-bit green, and 5-bit blue components.</summary>
        Rgb565 = 7,
        /// <summary>One 32-bit floating-point red component.</summary>
        Rf = 8,
        /// <summary>32-bit floating-point red and green components.</summary>
        Rgf = 9,
        /// <summary>32-bit floating-point red, green, and blue components.</summary>
        Rgbf = 10,
        /// <summary>32-bit floating-point red, green, blue, and alpha components.</summary>
        Rgbaf = 11,
        /// <summary>One 16-bit floating-point red component.</summary>
        Rh = 12,
        /// <summary>16-bit floating-point red and green components.</summary>
        Rgh = 13,
        /// <summary>16-bit floating-point red, green, and blue components.</summary>
        Rgbh = 14,
        /// <summary>16-bit floating-point red, green, blue, and alpha components.</summary>
        Rgbah = 15,
        /// <summary>Packed positive RGB values with three 9-bit mantissas and a shared 5-bit exponent.</summary>
        Rgbe9995 = 16,
        /// <summary>BC1/DXT1 block-compressed RGB data.</summary>
        Dxt1 = 17,
        /// <summary>BC2/DXT3 block-compressed RGBA data.</summary>
        Dxt3 = 18,
        /// <summary>BC3/DXT5 block-compressed RGBA data.</summary>
        Dxt5 = 19,
        /// <summary>BC4 block-compressed red data.</summary>
        RgtcR = 20,
        /// <summary>BC5 block-compressed red and green data.</summary>
        RgtcRg = 21,
        /// <summary>BC7 block-compressed RGBA data.</summary>
        BptcRgba = 22,
        /// <summary>Signed BC6 block-compressed RGB floating-point data.</summary>
        BptcRgbf = 23,
        /// <summary>Unsigned BC6 block-compressed RGB floating-point data.</summary>
        BptcRgbfu = 24,
        /// <summary>ETC1 block-compressed RGB data.</summary>
        Etc = 25,
        /// <summary>Unsigned ETC2 block-compressed red data.</summary>
        Etc2R11 = 26,
        /// <summary>Signed ETC2 block-compressed red data.</summary>
        Etc2R11S = 27,
        /// <summary>Unsigned ETC2 block-compressed red and green data.</summary>
        Etc2Rg11 = 28,
        /// <summary>Signed ETC2 block-compressed red and green data.</summary>
        Etc2Rg11S = 29,
        /// <summary>ETC2 block-compressed RGB data.</summary>
        Etc2Rgb8 = 30,
        /// <summary>ETC2 block-compressed RGBA data.</summary>
        Etc2Rgba8 = 31,
        /// <summary>ETC2 block-compressed RGB data with one-bit alpha.</summary>
        Etc2Rgb8A1 = 32,
        /// <summary>ETC2 RGBA data interpreted as red and green channels.</summary>
        Etc2RaAsRg = 33,
        /// <summary>BC3 RGBA data interpreted as red and green channels.</summary>
        Dxt5RaAsRg = 34,
        /// <summary>ASTC data using 4-by-4 pixel blocks.</summary>
        Astc4X4 = 35,
        /// <summary>HDR ASTC data using 4-by-4 pixel blocks.</summary>
        Astc4X4Hdr = 36,
        /// <summary>ASTC data using 8-by-8 pixel blocks.</summary>
        Astc8X8 = 37,
        /// <summary>HDR ASTC data using 8-by-8 pixel blocks.</summary>
        Astc8X8Hdr = 38,
        /// <summary>One normalized unsigned 16-bit red component.</summary>
        R16 = 39,
        /// <summary>Normalized unsigned 16-bit red and green components.</summary>
        Rg16 = 40,
        /// <summary>Normalized unsigned 16-bit red, green, and blue components.</summary>
        Rgb16 = 41,
        /// <summary>Normalized unsigned 16-bit red, green, blue, and alpha components.</summary>
        Rgba16 = 42,
        /// <summary>One unsigned 16-bit integer red component.</summary>
        R16I = 43,
        /// <summary>Unsigned 16-bit integer red and green components.</summary>
        Rg16I = 44,
        /// <summary>Unsigned 16-bit integer red, green, and blue components.</summary>
        Rgb16I = 45,
        /// <summary>Unsigned 16-bit integer red, green, blue, and alpha components.</summary>
        Rgba16I = 46,
        /// <summary>Marks the number of defined storage formats and is not a valid image format.</summary>
        Max = 47,
    }

    /// <summary>Defines the reconstruction filter used while resizing an image.</summary>
    public enum Interpolation
    {
        /// <summary>Uses the nearest source pixel and preserves hard pixel edges.</summary>
        Nearest = 0,
        /// <summary>Linearly blends the nearest four source pixels.</summary>
        Bilinear = 1,
        /// <summary>Uses a bicubic reconstruction over sixteen neighboring samples.</summary>
        Cubic = 2,
        /// <summary>Linearly blends bilinear samples from two mip levels.</summary>
        Trilinear = 3,
        /// <summary>Uses a radius-three Lanczos reconstruction filter.</summary>
        Lanczos = 4,
    }

    /// <summary>Describes how an image uses alpha values.</summary>
    public enum AlphaMode
    {
        /// <summary>The image has no transparent pixels.</summary>
        None = 0,
        /// <summary>Every transparent pixel is either fully transparent or fully opaque.</summary>
        Bit = 1,
        /// <summary>The image contains fractional alpha.</summary>
        Blend = 2,
    }

    /// <summary>Describes the smallest meaningful set of color channels in an image.</summary>
    public enum UsedChannels
    {
        /// <summary>Only luminance is required.</summary>
        Luminance = 0,
        /// <summary>Luminance and alpha are required.</summary>
        LuminanceAlpha = 1,
        /// <summary>Only red is required.</summary>
        Red = 2,
        /// <summary>Red and green are required.</summary>
        RedGreen = 3,
        /// <summary>Red, green, and blue are required.</summary>
        Rgb = 4,
        /// <summary>Red, green, blue, and alpha are required.</summary>
        Rgba = 5,
    }

    /// <summary>Provides semantic source information used while detecting active channels.</summary>
    public enum CompressSource
    {
        /// <summary>Treats the image as ordinary color or data.</summary>
        Generic = 0,
        /// <summary>Treats the image as nonlinear sRGB color.</summary>
        Srgb = 1,
        /// <summary>Treats the image as a tangent-space normal map whose red and green channels are sufficient.</summary>
        Normal = 2,
        /// <summary>Marks the number of source modes and is not a valid source.</summary>
        Max = 3,
    }

    /// <summary>Identifies a block-compression family used by a future texture-compression backend.</summary>
    public enum CompressMode
    {
        /// <summary>Selects the S3TC/BC1-BC5 family.</summary>
        S3tc = 0,
        /// <summary>Selects the ETC1 family.</summary>
        Etc = 1,
        /// <summary>Selects the ETC2/EAC family.</summary>
        Etc2 = 2,
        /// <summary>Selects the BPTC/BC6-BC7 family.</summary>
        Bptc = 3,
        /// <summary>Selects the ASTC family.</summary>
        Astc = 4,
        /// <summary>Marks the number of compression families and is not a valid mode.</summary>
        Max = 5,
    }

    /// <summary>Defines the pixel-block footprint requested from a future ASTC encoder.</summary>
    public enum AstcFormat
    {
        /// <summary>Uses 4-by-4 pixel blocks for higher quality and larger storage.</summary>
        Format4X4 = 0,
        /// <summary>Uses 8-by-8 pixel blocks for smaller storage and lower quality.</summary>
        Format8X8 = 1,
    }

    /// <summary>Initializes an empty image with no pixel data.</summary>
    public Image()
    {
    }

    /// <summary>Gets the image width in pixels, or zero for an empty image.</summary>
    /// <value>A value from zero through <see cref="MaxWidth"/>.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public int Width => Read(state => state.Width);

    /// <summary>Gets the image height in pixels, or zero for an empty image.</summary>
    /// <value>A value from zero through <see cref="MaxHeight"/>.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public int Height => Read(state => state.Height);

    /// <summary>Gets the image dimensions in pixels.</summary>
    /// <value>Zero for an empty image; otherwise the current positive width and height.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public Vector2I Size => Read(state => new Vector2I(state.Width, state.Height));

    /// <summary>Gets the raw pixel storage format.</summary>
    /// <value><see cref="Format.L8"/> for a newly constructed empty image; otherwise the configured format.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public Format PixelFormat => Read(state => state.Format);

    /// <summary>Gets whether lower-resolution mip levels follow the base level in the raw buffer.</summary>
    /// <value><see langword="true"/> only when at least one complete mip level is stored.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public bool HasMipmaps => Read(state => state.HasMipmaps);

    /// <summary>Gets the number of stored mip levels excluding the base level.</summary>
    /// <value>Zero when mipmaps are absent or the image is empty.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public int MipmapCount => Read(state => GetRequiredMipmapCount(state.Width, state.Height, state.HasMipmaps));

    /// <summary>Gets the number of bytes in the complete raw image buffer.</summary>
    /// <value>Zero for an empty image; otherwise the base level and every mip level in bytes.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public int DataSize => Read(state => state.Data.Length);

    /// <summary>Gets whether the current format uses GPU block compression.</summary>
    /// <value><see langword="true"/> for the DXT, RGTC, BPTC, ETC, ETC2, or ASTC format families.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public bool IsCompressed => Read(state => IsCompressedFormat(state.Format));

    /// <summary>Gets whether the image has no dimensions and no pixel data.</summary>
    /// <value><see langword="true"/> only for the canonical zero-by-zero empty state.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public bool IsEmpty => Read(state => state.Data.Length == 0);

    /// <summary>Creates a zero-filled image of the requested dimensions and format.</summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="useMipmaps">Whether to allocate the complete mip chain.</param>
    /// <param name="format">The raw storage format.</param>
    /// <returns>A new independent image.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or format is invalid, the pixel limit is exceeded, or the required buffer cannot be represented by a managed array.</exception>
    public static Image CreateEmpty(int width, int height, bool useMipmaps, Format format)
    {
        var length = GetRequiredDataSize(width, height, format, useMipmaps);
        var image = new Image();
        image.SetStateWithoutNotification(new State(width, height, format, useMipmaps && GetRequiredMipmapCount(width, height, true) != 0, new byte[length]));
        return image;
    }

    /// <summary>Creates a zero-filled image of the requested dimensions and format.</summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="useMipmaps">Whether to allocate the complete mip chain.</param>
    /// <param name="format">The raw storage format.</param>
    /// <returns>A new independent image.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or format is invalid, the pixel limit is exceeded, or the required buffer cannot be represented by a managed array.</exception>
    [Obsolete("Use CreateEmpty instead.")]
    public static Image Create(int width, int height, bool useMipmaps, Format format) =>
        CreateEmpty(width, height, useMipmaps, format);

    /// <summary>Creates an image by copying a complete raw base-level and optional mipmap buffer.</summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="useMipmaps">Whether <paramref name="data"/> includes the complete mip chain.</param>
    /// <param name="format">The raw storage format.</param>
    /// <param name="data">The exact raw byte sequence to copy.</param>
    /// <returns>A new independent image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The byte count does not exactly match the declared image.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or format is invalid or exceeds the supported bounds.</exception>
    public static Image CreateFromData(int width, int height, bool useMipmaps, Format format, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var expected = GetRequiredDataSize(width, height, format, useMipmaps);
        if (data.Length != expected)
            throw new ArgumentException($"Image data contains {data.Length} bytes; {expected} bytes are required.", nameof(data));

        var image = new Image();
        image.SetStateWithoutNotification(new State(width, height, format, useMipmaps && GetRequiredMipmapCount(width, height, true) != 0, (byte[])data.Clone()));
        return image;
    }

    /// <summary>Replaces the complete image state by copying a raw base-level and optional mipmap buffer.</summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="useMipmaps">Whether <paramref name="data"/> includes the complete mip chain.</param>
    /// <param name="format">The raw storage format.</param>
    /// <param name="data">The exact raw byte sequence to copy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The byte count does not exactly match the declared image.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or format is invalid or exceeds the supported bounds.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the new state commits.</exception>
    public void SetData(int width, int height, bool useMipmaps, Format format, byte[] data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);
        var expected = GetRequiredDataSize(width, height, format, useMipmaps);
        if (data.Length != expected)
            throw new ArgumentException($"Image data contains {data.Length} bytes; {expected} bytes are required.", nameof(data));

        Commit(new State(width, height, format, useMipmaps && GetRequiredMipmapCount(width, height, true) != 0, (byte[])data.Clone()));
    }

    /// <summary>Returns an independent copy of the complete raw image buffer.</summary>
    /// <returns>The base level followed by all mip levels, or an empty array for an empty image.</returns>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public byte[] GetData() => Read(state => (byte[])state.Data.Clone());

    /// <summary>Returns the byte offset of a base or mip level in the raw buffer.</summary>
    /// <param name="mipmap">Zero for the base image, one for the first mip level, and so on.</param>
    /// <returns>The zero-based byte offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mipmap"/> does not identify a stored level.</exception>
    /// <exception cref="InvalidOperationException">The image is empty.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public int GetMipmapOffset(int mipmap)
    {
        var state = Snapshot();
        if (state.Data.Length == 0)
            throw new InvalidOperationException("An empty image has no mip levels.");

        var count = GetRequiredMipmapCount(state.Width, state.Height, state.HasMipmaps);
        if ((uint)mipmap > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(mipmap));

        return GetLevel(state, mipmap).Offset;
    }

    /// <summary>Copies all image state from another image while preserving this resource's identity.</summary>
    /// <param name="source">The live source image.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Either image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the copied state commits.</exception>
    public void CopyFrom(Image source)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source))
            return;

        Commit(source.Snapshot());
    }

    /// <summary>Gets the color stored at a base-level pixel coordinate.</summary>
    /// <param name="x">The zero-based horizontal coordinate.</param>
    /// <param name="y">The zero-based vertical coordinate.</param>
    /// <returns>The decoded color.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate lies outside the base image.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public Color GetPixel(int x, int y)
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            var state = CaptureState(copyData: false);
            ValidatePixelAccess(state, x, y);
            return ReadColor(_data, ((y * _width) + x) * GetBytesPerPixel(_format), _format);
        }
    }

    /// <summary>Gets the color stored at a base-level pixel coordinate.</summary>
    /// <param name="point">The zero-based pixel coordinate.</param>
    /// <returns>The decoded color.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The coordinate lies outside the base image.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public Color GetPixel(Vector2I point) => GetPixel(point.X, point.Y);

    /// <summary>Stores a color at a base-level pixel coordinate.</summary>
    /// <remarks>Existing mip levels are not regenerated; call <see cref="GenerateMipmaps(bool)"/> after a batch of pixel edits when they must reflect the base level.</remarks>
    /// <param name="x">The zero-based horizontal coordinate.</param>
    /// <param name="y">The zero-based vertical coordinate.</param>
    /// <param name="color">The color to encode in the current format.</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate lies outside the base image.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the pixel commits.</exception>
    public void SetPixel(int x, int y, Color color)
    {
        ThrowIfDisposed();
        lock (_mutationGate)
        {
            lock (_stateGate)
            {
                ThrowIfDisposed();
                var state = CaptureState(copyData: false);
                ValidatePixelAccess(state, x, y);
                WriteColor(_data, ((y * _width) + x) * GetBytesPerPixel(_format), _format, color);
            }
        }

        EmitChanged();
    }

    /// <summary>Stores a color at a base-level pixel coordinate.</summary>
    /// <remarks>Existing mip levels are not regenerated; call <see cref="GenerateMipmaps(bool)"/> after a batch of pixel edits when they must reflect the base level.</remarks>
    /// <param name="point">The zero-based pixel coordinate.</param>
    /// <param name="color">The color to encode in the current format.</param>
    /// <exception cref="ArgumentOutOfRangeException">The coordinate lies outside the base image.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the pixel commits.</exception>
    public void SetPixel(Vector2I point, Color color) => SetPixel(point.X, point.Y, color);

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Image();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ArgumentNullException.ThrowIfNull(target);
        var image = (Image)target;
        var state = Snapshot();
        if (state.Data.Length == 0)
            image.SetStateWithoutNotification(state);
        else
            image.SetData(state.Width, state.Height, state.HasMipmaps, state.Format, state.Data);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ImageProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_mutationGate)
            {
                lock (_stateGate)
                {
                    _data = [];
                    _width = 0;
                    _height = 0;
                    _format = Format.L8;
                    _hasMipmaps = false;
                }
            }
        }

        base.Dispose(disposing);
    }

    private T Read<T>(Func<State, T> reader)
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            return reader(CaptureState(copyData: false));
        }
    }

    private State Snapshot(bool copyData = true)
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            return CaptureState(copyData);
        }
    }

    private State CaptureState(bool copyData) =>
        new(_width, _height, _format, _hasMipmaps, copyData ? (byte[])_data.Clone() : _data);

    private void Commit(State state)
    {
        ThrowIfDisposed();
        lock (_mutationGate)
            SetStateWithoutNotification(state);
        EmitChanged();
    }

    private void SetStateWithoutNotification(State state)
    {
        lock (_stateGate)
        {
            ThrowIfDisposed();
            _width = state.Width;
            _height = state.Height;
            _format = state.Format;
            _hasMipmaps = state.HasMipmaps;
            _data = state.Data;
        }
    }

    private static void ValidatePixelAccess(State state, int x, int y)
    {
        if (state.Data.Length == 0)
            throw new InvalidOperationException("An empty image has no pixels.");
        if (IsCompressedFormat(state.Format))
            throw new InvalidOperationException("Individual pixels cannot be accessed in a block-compressed image.");
        if ((uint)x >= (uint)state.Width)
            throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)state.Height)
            throw new ArgumentOutOfRangeException(nameof(y));
    }

    private static int GetRequiredDataSize(int width, int height, Format format, bool mipmaps)
    {
        ValidateDimensions(width, height);
        ValidateFormat(format);

        long size = 0;
        var levelWidth = width;
        var levelHeight = height;
        while (true)
        {
            size = checked(size + GetLevelByteCount(levelWidth, levelHeight, format));
            if (!mipmaps || (levelWidth == 1 && levelHeight == 1))
                break;
            levelWidth = Math.Max(1, levelWidth / 2);
            levelHeight = Math.Max(1, levelHeight / 2);
        }

        if (size > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width), "The image buffer exceeds the managed array limit.");
        return (int)size;
    }

    private static int GetRequiredMipmapCount(int width, int height, bool mipmaps)
    {
        if (!mipmaps || width == 0 || height == 0)
            return 0;

        var count = 0;
        while (width != 1 || height != 1)
        {
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            count++;
        }

        return count;
    }

    private static Level GetLevel(State state, int index)
    {
        var offset = 0;
        var width = state.Width;
        var height = state.Height;
        for (var level = 0; level < index; level++)
        {
            offset = checked(offset + GetLevelByteCount(width, height, state.Format));
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }

        return new Level(offset, GetLevelByteCount(width, height, state.Format), width, height);
    }

    private static int GetLevelByteCount(int width, int height, Format format)
    {
        if (!IsCompressedFormat(format))
            return CheckedByteCount((long)width * height * GetBytesPerPixel(format));

        var (blockWidth, blockHeight, blockBytes) = GetCompressedBlock(format);
        var columns = Math.Max(1, (width + blockWidth - 1) / blockWidth);
        var rows = Math.Max(1, (height + blockHeight - 1) / blockHeight);
        return CheckedByteCount((long)columns * rows * blockBytes);
    }

    private static int CheckedByteCount(long count) => count > int.MaxValue
        ? throw new ArgumentOutOfRangeException(nameof(count), "The image buffer exceeds the managed array limit.")
        : (int)count;

    private static int GetBytesPerPixel(Format format) => format switch
    {
        Format.L8 or Format.R8 => 1,
        Format.La8 or Format.Rg8 or Format.Rgba4444 or Format.Rgb565 or Format.Rh or Format.R16 or Format.R16I => 2,
        Format.Rgb8 => 3,
        Format.Rgba8 or Format.Rf or Format.Rgbe9995 or Format.Rgh or Format.Rg16 or Format.Rg16I => 4,
        Format.Rgbh or Format.Rgb16 or Format.Rgb16I => 6,
        Format.Rgf or Format.Rgbah or Format.Rgba16 or Format.Rgba16I => 8,
        Format.Rgbf => 12,
        Format.Rgbaf => 16,
        _ => throw new InvalidOperationException("A block-compressed format has no fixed bytes-per-pixel value."),
    };

    private static (int Width, int Height, int Bytes) GetCompressedBlock(Format format) => format switch
    {
        Format.Dxt1 or Format.RgtcR or Format.Etc or Format.Etc2R11 or Format.Etc2R11S or
            Format.Etc2Rgb8 or Format.Etc2Rgb8A1 => (4, 4, 8),
        Format.Dxt3 or Format.Dxt5 or Format.RgtcRg or Format.BptcRgba or Format.BptcRgbf or
            Format.BptcRgbfu or Format.Etc2Rg11 or Format.Etc2Rg11S or Format.Etc2Rgba8 or
            Format.Etc2RaAsRg or Format.Dxt5RaAsRg or Format.Astc4X4 or Format.Astc4X4Hdr => (4, 4, 16),
        Format.Astc8X8 or Format.Astc8X8Hdr => (8, 8, 16),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The format is not block-compressed."),
    };

    private static bool IsCompressedFormat(Format format) => format is >= Format.Dxt1 and <= Format.Astc8X8Hdr;

    private static void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || width > MaxWidth)
            throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be between 1 and {MaxWidth} pixels.");
        if (height <= 0 || height > MaxHeight)
            throw new ArgumentOutOfRangeException(nameof(height), height, $"Height must be between 1 and {MaxHeight} pixels.");
        if ((long)width * height > MaxPixels)
            throw new ArgumentOutOfRangeException(nameof(width), "The image exceeds the maximum supported pixel count.");
    }

    private static void ValidateFormat(Format format)
    {
        if (format < Format.L8 || format >= Format.Max)
            throw new ArgumentOutOfRangeException(nameof(format));
    }

    private static Color ReadColor(byte[] data, int offset, Format format)
    {
        var r = 0f;
        var g = 0f;
        var b = 0f;
        var a = 1f;
        switch (format)
        {
            case Format.L8:
                r = g = b = data[offset] / 255f;
                break;
            case Format.La8:
                r = g = b = data[offset] / 255f;
                a = data[offset + 1] / 255f;
                break;
            case Format.R8:
                r = data[offset] / 255f;
                break;
            case Format.Rg8:
                r = data[offset] / 255f;
                g = data[offset + 1] / 255f;
                break;
            case Format.Rgb8:
                r = data[offset] / 255f;
                g = data[offset + 1] / 255f;
                b = data[offset + 2] / 255f;
                break;
            case Format.Rgba8:
                r = data[offset] / 255f;
                g = data[offset + 1] / 255f;
                b = data[offset + 2] / 255f;
                a = data[offset + 3] / 255f;
                break;
            case Format.Rgba4444:
                {
                    var packed = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
                    r = ((packed >> 12) & 0xf) / 15f;
                    g = ((packed >> 8) & 0xf) / 15f;
                    b = ((packed >> 4) & 0xf) / 15f;
                    a = (packed & 0xf) / 15f;
                    break;
                }
            case Format.Rgb565:
                {
                    var packed = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
                    r = ((packed >> 11) & 0x1f) / 31f;
                    g = ((packed >> 5) & 0x3f) / 63f;
                    b = (packed & 0x1f) / 31f;
                    break;
                }
            case Format.Rf:
            case Format.Rgf:
            case Format.Rgbf:
            case Format.Rgbaf:
                r = ReadSingle(data, offset);
                if (format >= Format.Rgf) g = ReadSingle(data, offset + 4);
                if (format >= Format.Rgbf) b = ReadSingle(data, offset + 8);
                if (format == Format.Rgbaf) a = ReadSingle(data, offset + 12);
                break;
            case Format.Rh:
            case Format.Rgh:
            case Format.Rgbh:
            case Format.Rgbah:
                r = ReadHalf(data, offset);
                if (format >= Format.Rgh) g = ReadHalf(data, offset + 2);
                if (format >= Format.Rgbh) b = ReadHalf(data, offset + 4);
                if (format == Format.Rgbah) a = ReadHalf(data, offset + 6);
                break;
            case Format.Rgbe9995:
                return Color.FromRgbe9995(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)));
            case Format.R16:
            case Format.Rg16:
            case Format.Rgb16:
            case Format.Rgba16:
                r = ReadUInt16(data, offset) / 65535f;
                if (format >= Format.Rg16) g = ReadUInt16(data, offset + 2) / 65535f;
                if (format >= Format.Rgb16) b = ReadUInt16(data, offset + 4) / 65535f;
                if (format == Format.Rgba16) a = ReadUInt16(data, offset + 6) / 65535f;
                break;
            case Format.R16I:
            case Format.Rg16I:
            case Format.Rgb16I:
            case Format.Rgba16I:
                r = ReadUInt16(data, offset);
                if (format >= Format.Rg16I) g = ReadUInt16(data, offset + 2);
                if (format >= Format.Rgb16I) b = ReadUInt16(data, offset + 4);
                if (format == Format.Rgba16I) a = ReadUInt16(data, offset + 6);
                break;
            default:
                throw new InvalidOperationException("Block-compressed pixels must be decompressed before access.");
        }

        return new Color(r, g, b, a);
    }

    private static void WriteColor(byte[] data, int offset, Format format, Color color)
    {
        switch (format)
        {
            case Format.L8:
                data[offset] = ToByte(Math.Max(color.R, Math.Max(color.G, color.B)));
                return;
            case Format.La8:
                data[offset] = ToByte(Math.Max(color.R, Math.Max(color.G, color.B)));
                data[offset + 1] = ToByte(color.A);
                return;
            case Format.R8:
                data[offset] = ToByte(color.R);
                return;
            case Format.Rg8:
                data[offset] = ToByte(color.R);
                data[offset + 1] = ToByte(color.G);
                return;
            case Format.Rgb8:
                data[offset] = ToByte(color.R);
                data[offset + 1] = ToByte(color.G);
                data[offset + 2] = ToByte(color.B);
                return;
            case Format.Rgba8:
                data[offset] = ToByte(color.R);
                data[offset + 1] = ToByte(color.G);
                data[offset + 2] = ToByte(color.B);
                data[offset + 3] = ToByte(color.A);
                return;
            case Format.Rgba4444:
                {
                    var packed = (ushort)((ToNibble(color.R) << 12) | (ToNibble(color.G) << 8) |
                        (ToNibble(color.B) << 4) | ToNibble(color.A));
                    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), packed);
                    return;
                }
            case Format.Rgb565:
                {
                    var packed = (ushort)((ToBits(color.R, 31) << 11) | (ToBits(color.G, 63) << 5) | ToBits(color.B, 31));
                    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), packed);
                    return;
                }
            case Format.Rf:
            case Format.Rgf:
            case Format.Rgbf:
            case Format.Rgbaf:
                WriteSingle(data, offset, color.R);
                if (format >= Format.Rgf) WriteSingle(data, offset + 4, color.G);
                if (format >= Format.Rgbf) WriteSingle(data, offset + 8, color.B);
                if (format == Format.Rgbaf) WriteSingle(data, offset + 12, color.A);
                return;
            case Format.Rh:
            case Format.Rgh:
            case Format.Rgbh:
            case Format.Rgbah:
                WriteHalf(data, offset, color.R);
                if (format >= Format.Rgh) WriteHalf(data, offset + 2, color.G);
                if (format >= Format.Rgbh) WriteHalf(data, offset + 4, color.B);
                if (format == Format.Rgbah) WriteHalf(data, offset + 6, color.A);
                return;
            case Format.Rgbe9995:
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), ToRgbe9995(color));
                return;
            case Format.R16:
            case Format.Rg16:
            case Format.Rgb16:
            case Format.Rgba16:
                WriteUInt16(data, offset, ToUShortNormalized(color.R));
                if (format >= Format.Rg16) WriteUInt16(data, offset + 2, ToUShortNormalized(color.G));
                if (format >= Format.Rgb16) WriteUInt16(data, offset + 4, ToUShortNormalized(color.B));
                if (format == Format.Rgba16) WriteUInt16(data, offset + 6, ToUShortNormalized(color.A));
                return;
            case Format.R16I:
            case Format.Rg16I:
            case Format.Rgb16I:
            case Format.Rgba16I:
                WriteUInt16(data, offset, ToUShortInteger(color.R));
                if (format >= Format.Rg16I) WriteUInt16(data, offset + 2, ToUShortInteger(color.G));
                if (format >= Format.Rgb16I) WriteUInt16(data, offset + 4, ToUShortInteger(color.B));
                if (format == Format.Rgba16I) WriteUInt16(data, offset + 6, ToUShortInteger(color.A));
                return;
            default:
                throw new InvalidOperationException("Block-compressed pixels must be decompressed before access.");
        }
    }

    private static ushort ReadUInt16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));

    private static void WriteUInt16(byte[] data, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);

    private static float ReadSingle(byte[] data, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)));

    private static void WriteSingle(byte[] data, int offset, float value) =>
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), BitConverter.SingleToInt32Bits(value));

    private static float ReadHalf(byte[] data, int offset) =>
        (float)BitConverter.UInt16BitsToHalf(ReadUInt16(data, offset));

    private static void WriteHalf(byte[] data, int offset, float value) =>
        WriteUInt16(data, offset, BitConverter.HalfToUInt16Bits((Half)value));

    private static byte ToByte(float value) => (byte)Math.Clamp((int)(value * 255f), 0, 255);

    private static int ToNibble(float value) => Math.Clamp((int)(value * 15f), 0, 15);

    private static int ToBits(float value, int maximum) => Math.Clamp((int)(value * maximum), 0, maximum);

    private static ushort ToUShortNormalized(float value) => (ushort)Math.Clamp((int)(value * 65535f), 0, 65535);

    private static ushort ToUShortInteger(float value) => (ushort)Math.Clamp((int)value, 0, 65535);

    private static uint ToRgbe9995(Color color)
    {
        const float maximum = 65408f;
        const float minimum = 1f / 65536f;
        var red = Math.Clamp(color.R, 0f, maximum);
        var green = Math.Clamp(color.G, 0f, maximum);
        var blue = Math.Clamp(color.B, 0f, maximum);
        var maxChannel = Math.Max(Math.Max(red, green), Math.Max(blue, minimum));
        var exponentBits = BitConverter.SingleToUInt32Bits(maxChannel) + 0x07804000u;
        exponentBits &= 0x7f800000u;
        var bias = BitConverter.UInt32BitsToSingle(exponentBits);
        var r = BitConverter.SingleToUInt32Bits(red + bias);
        var g = BitConverter.SingleToUInt32Bits(green + bias);
        var b = BitConverter.SingleToUInt32Bits(blue + bias);
        exponentBits = (exponentBits << 4) + 0x10000000u;
        return exponentBits | (b << 18) | (g << 9) | (r & 511u);
    }

    private readonly record struct State(int Width, int Height, Format Format, bool HasMipmaps, byte[] Data);

    private readonly record struct Level(int Offset, int Length, int Width, int Height);
}

/// <summary>Contains scalar error measurements produced by <see cref="Image.ComputeImageMetrics"/>.</summary>
public readonly record struct ImageMetrics
{
    /// <summary>Initializes a complete image-metrics value.</summary>
    /// <param name="maximum">The largest observed absolute 8-bit component error.</param>
    /// <param name="mean">The mean absolute 8-bit component error.</param>
    /// <param name="meanSquared">The mean squared 8-bit component error.</param>
    /// <param name="rootMeanSquared">The square root of <paramref name="meanSquared"/>.</param>
    /// <param name="peakSignalToNoiseRatio">The peak signal-to-noise ratio in decibels, capped at 500.</param>
    public ImageMetrics(double maximum, double mean, double meanSquared, double rootMeanSquared, double peakSignalToNoiseRatio)
    {
        Maximum = maximum;
        Mean = mean;
        MeanSquared = meanSquared;
        RootMeanSquared = rootMeanSquared;
        PeakSignalToNoiseRatio = peakSignalToNoiseRatio;
    }

    /// <summary>Gets the largest observed absolute 8-bit component error.</summary>
    /// <value>A value from zero through 255.</value>
    public double Maximum { get; }

    /// <summary>Gets the mean absolute 8-bit component error.</summary>
    /// <value>A value from zero through 255.</value>
    public double Mean { get; }

    /// <summary>Gets the mean squared 8-bit component error.</summary>
    /// <value>A value from zero through 65025.</value>
    public double MeanSquared { get; }

    /// <summary>Gets the root-mean-squared 8-bit component error.</summary>
    /// <value>A value from zero through 255.</value>
    public double RootMeanSquared { get; }

    /// <summary>Gets the peak signal-to-noise ratio in decibels.</summary>
    /// <value>A value from zero through 500; identical images report 500.</value>
    public double PeakSignalToNoiseRatio { get; }
}
