namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Gets whether every base-level pixel is fully transparent.</summary>
    /// <value><see langword="true"/> for an empty image or when every alpha-capable base-level pixel has zero alpha.</value>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public bool IsInvisible
    {
        get
        {
            var state = Snapshot();
            if (state.Data.Length == 0)
                return true;
            if (IsCompressedFormat(state.Format) || !HasAlpha(state.Format))
                return false;

            var bytes = GetBytesPerPixel(state.Format);
            for (var index = 0; index < state.Width * state.Height; index++)
            {
                if (ReadColor(state.Data, index * bytes, state.Format).A != 0f)
                    return false;
            }

            return true;
        }
    }

    /// <summary>Classifies the alpha values used by the base level.</summary>
    /// <returns><see cref="AlphaMode.None"/>, <see cref="AlphaMode.Bit"/>, or <see cref="AlphaMode.Blend"/>.</returns>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public AlphaMode DetectAlpha()
    {
        var state = Snapshot();
        if (state.Data.Length == 0)
            return AlphaMode.None;
        if (IsCompressedFormat(state.Format))
            return state.Format is Format.Dxt3 or Format.Dxt5 ? AlphaMode.Blend : AlphaMode.None;
        if (!HasAlpha(state.Format))
            return AlphaMode.None;

        var hasTransparent = false;
        var opaque = state.Format == Format.Rgba16I ? 65535f : 1f;
        var bytes = GetBytesPerPixel(state.Format);
        for (var index = 0; index < state.Width * state.Height; index++)
        {
            var alpha = ReadColor(state.Data, index * bytes, state.Format).A;
            if (alpha > 0f && alpha < opaque)
                return AlphaMode.Blend;
            hasTransparent |= alpha <= 0f;
        }

        return hasTransparent ? AlphaMode.Bit : AlphaMode.None;
    }

    /// <summary>Finds the smallest meaningful channel set used by the base level.</summary>
    /// <param name="source">The semantic interpretation of the data.</param>
    /// <returns>The detected channel set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is not defined.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public UsedChannels DetectUsedChannels(CompressSource source = CompressSource.Generic)
    {
        if (source < CompressSource.Generic || source > CompressSource.Normal)
            throw new ArgumentOutOfRangeException(nameof(source));

        var state = RequireReadablePixels();
        if (source == CompressSource.Normal)
            return UsedChannels.RedGreen;
        if (state.Format == Format.L8)
            return UsedChannels.Luminance;
        if (state.Format is Format.R8 or Format.Rf or Format.Rh or Format.R16 or Format.R16I)
            return UsedChannels.Red;

        var bytes = GetBytesPerPixel(state.Format);
        var red = false;
        var green = false;
        var blue = false;
        var alpha = false;
        var color = false;
        var opaque = state.Format == Format.Rgba16I ? 65535f : 1f;
        for (var index = 0; index < state.Width * state.Height; index++)
        {
            var pixel = ReadColor(state.Data, index * bytes, state.Format);
            red |= pixel.R > 0.001f;
            green |= pixel.G > 0.001f;
            blue |= pixel.B > 0.001f;
            alpha |= pixel.A < opaque - (state.Format == Format.Rgba16I ? 0f : 0.001f);
            color |= pixel.R != pixel.G || pixel.R != pixel.B || pixel.G != pixel.B;
        }

        if (!color)
            return alpha ? UsedChannels.LuminanceAlpha : UsedChannels.Luminance;
        if (alpha)
            return UsedChannels.Rgba;
        if (blue)
            return UsedChannels.Rgb;
        if (green)
            return UsedChannels.RedGreen;
        return red ? UsedChannels.Red : UsedChannels.Luminance;
    }

    /// <summary>Returns the smallest rectangle containing every nontransparent base-level pixel.</summary>
    /// <returns>An empty rectangle when no pixel is visible; otherwise half-open pixel bounds.</returns>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public RectI GetUsedRect()
    {
        var state = RequireReadablePixels();
        var bytes = GetBytesPerPixel(state.Format);
        var minX = state.Width;
        var minY = state.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < state.Height; y++)
        {
            for (var x = 0; x < state.Width; x++)
            {
                if (ReadColor(state.Data, ((y * state.Width) + x) * bytes, state.Format).A <= 0f)
                    continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX ? default : new RectI(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
    }

    /// <summary>Fills every pixel in every stored level with one color.</summary>
    /// <param name="color">The color encoded into the current format.</param>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the fill commits.</exception>
    public void Fill(Color color) => Mutate(
        state =>
        {
            RequireReadablePixels(state);
            var bytes = GetBytesPerPixel(state.Format);
            for (var offset = 0; offset < state.Data.Length; offset += bytes)
                WriteColor(state.Data, offset, state.Format, color);
            return state;
        });

    /// <summary>Fills the base-level intersection of a rectangle and the image bounds.</summary>
    /// <param name="rectangle">The half-open pixel rectangle; nonpositive sizes are treated as empty.</param>
    /// <param name="color">The color encoded into the current format.</param>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the fill commits.</exception>
    public void FillRect(RectI rectangle, Color color) => Mutate(
        state =>
        {
            RequireReadablePixels(state);
            var clipped = ClipRectangle(rectangle, state.Width, state.Height);
            if (!clipped.HasArea())
                return state;
            var bytes = GetBytesPerPixel(state.Format);
            for (var y = clipped.Position.Y; y < clipped.End.Y; y++)
            {
                for (var x = clipped.Position.X; x < clipped.End.X; x++)
                    WriteColor(state.Data, ((y * state.Width) + x) * bytes, state.Format, color);
            }

            return RebuildMipmapsIfNeeded(state);
        });

    /// <summary>Removes all mip levels while retaining the base image unchanged.</summary>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the operation commits.</exception>
    public void ClearMipmaps() => Mutate(
        state =>
        {
            if (!state.HasMipmaps)
                return state;
            var baseLength = GetLevelByteCount(state.Width, state.Height, state.Format);
            return state with { HasMipmaps = false, Data = state.Data[..baseLength] };
        });

    /// <summary>Generates a complete lower-resolution mip chain from the base image.</summary>
    /// <param name="renormalize">Whether RGB values are decoded as signed normal components and normalized after averaging.</param>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the chain commits.</exception>
    public void GenerateMipmaps(bool renormalize = false) => Mutate(state => BuildMipmaps(RequireReadablePixels(state), renormalize));

    /// <summary>Converts every stored level to another uncompressed pixel format.</summary>
    /// <param name="format">The destination format.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">The source or destination format is block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the conversion commits.</exception>
    public void Convert(Format format)
    {
        ValidateFormat(format);
        Mutate(state => ConvertState(state, format));
    }

    /// <summary>Crops or expands the image from its top-left corner, filling new pixels with transparent black.</summary>
    /// <param name="width">The positive destination width.</param>
    /// <param name="height">The positive destination height.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is invalid or exceeds supported bounds.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the crop commits.</exception>
    public void Crop(int width, int height)
    {
        ValidateDimensions(width, height);
        Mutate(state => CropState(RequireReadablePixels(state), width, height));
    }

    /// <summary>Returns a new image containing the intersection of a region and the base image.</summary>
    /// <param name="region">The requested half-open pixel rectangle.</param>
    /// <returns>An empty image when the intersection has no area; otherwise an uncompressed image without mipmaps.</returns>
    /// <exception cref="InvalidOperationException">This image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">This image is disposing or disposed.</exception>
    public Image GetRegion(RectI region)
    {
        var state = RequireReadablePixels();
        var clipped = ClipRectangle(region, state.Width, state.Height);
        if (!clipped.HasArea())
            return new Image();

        var bytes = GetBytesPerPixel(state.Format);
        var data = new byte[GetRequiredDataSize(clipped.Size.X, clipped.Size.Y, state.Format, false)];
        for (var y = 0; y < clipped.Size.Y; y++)
        {
            Buffer.BlockCopy(
                state.Data,
                (((clipped.Position.Y + y) * state.Width) + clipped.Position.X) * bytes,
                data,
                y * clipped.Size.X * bytes,
                clipped.Size.X * bytes);
        }

        var result = new Image();
        result.SetStateWithoutNotification(new State(clipped.Size.X, clipped.Size.Y, state.Format, false, data));
        return result;
    }

    /// <summary>Flips every stored level horizontally.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the flip commits.</exception>
    public void FlipX() => Mutate(state => FlipState(RequireReadablePixels(state), horizontal: true));

    /// <summary>Flips every stored level vertically.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the flip commits.</exception>
    public void FlipY() => Mutate(state => FlipState(RequireReadablePixels(state), horizontal: false));

    /// <summary>Rotates the base image by 90 degrees and regenerates an existing mip chain.</summary>
    /// <param name="direction">The rotation direction.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not defined.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the rotation commits.</exception>
    public void Rotate90(ClockDirection direction)
    {
        if (!Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(direction));
        Mutate(state => Rotate90State(RequireReadablePixels(state), direction));
    }

    /// <summary>Rotates every stored level by 180 degrees.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the rotation commits.</exception>
    public void Rotate180() => Mutate(
        state => FlipState(FlipState(RequireReadablePixels(state), horizontal: true), horizontal: false));

    /// <summary>Halves both dimensions using box filtering, clamped to one pixel, and regenerates existing mipmaps.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the resize commits.</exception>
    public void ShrinkX2() => Mutate(
        state => ResizeState(
            RequireReadablePixels(state),
            Math.Max(1, state.Width / 2),
            Math.Max(1, state.Height / 2),
            Interpolation.Bilinear));

    /// <summary>Resizes the base image and regenerates an existing mip chain.</summary>
    /// <param name="width">The positive destination width.</param>
    /// <param name="height">The positive destination height.</param>
    /// <param name="interpolation">The reconstruction filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or interpolation mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the resize commits.</exception>
    public void Resize(int width, int height, Interpolation interpolation = Interpolation.Bilinear)
    {
        ValidateDimensions(width, height);
        ValidateInterpolation(interpolation);
        Mutate(state => ResizeState(RequireReadablePixels(state), width, height, interpolation));
    }

    /// <summary>Resizes each dimension to the next power of two and optionally makes the result square.</summary>
    /// <param name="square">Whether both dimensions use the larger resulting power of two.</param>
    /// <param name="interpolation">The reconstruction filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">The resulting size or interpolation mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the resize commits.</exception>
    public void ResizeToPowerOfTwo(bool square = false, Interpolation interpolation = Interpolation.Bilinear)
    {
        ValidateInterpolation(interpolation);
        Mutate(
            state =>
            {
                RequireReadablePixels(state);
                var width = NextPowerOfTwo(state.Width);
                var height = NextPowerOfTwo(state.Height);
                if (square)
                    width = height = Math.Max(width, height);
                ValidateDimensions(width, height);
                return ResizeState(state, width, height, interpolation);
            });
    }

    /// <summary>Copies a clipped source rectangle into this image without alpha blending.</summary>
    /// <param name="source">The source image.</param>
    /// <param name="sourceRect">The half-open source rectangle.</param>
    /// <param name="destination">The target coordinate corresponding to <paramref name="sourceRect"/>'s position.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The source and destination formats differ.</exception>
    /// <exception cref="InvalidOperationException">Either image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">Either image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the copy commits.</exception>
    public void BlitRect(Image source, RectI sourceRect, Vector2I destination) =>
        Composite(source, null, sourceRect, destination, blend: false);

    /// <summary>Alpha-composites a clipped source rectangle over this image.</summary>
    /// <param name="source">The straight-alpha source image.</param>
    /// <param name="sourceRect">The half-open source rectangle.</param>
    /// <param name="destination">The target coordinate corresponding to <paramref name="sourceRect"/>'s position.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The source and destination formats differ.</exception>
    /// <exception cref="InvalidOperationException">Either image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">Either image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the blend commits.</exception>
    public void BlendRect(Image source, RectI sourceRect, Vector2I destination) =>
        Composite(source, null, sourceRect, destination, blend: true);

    /// <summary>Copies source pixels whose corresponding mask alpha is nonzero.</summary>
    /// <param name="source">The source image.</param>
    /// <param name="mask">A same-sized mask image.</param>
    /// <param name="sourceRect">The half-open source rectangle.</param>
    /// <param name="destination">The target coordinate corresponding to <paramref name="sourceRect"/>'s position.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="mask"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Formats or source/mask dimensions violate the operation contract.</exception>
    /// <exception cref="InvalidOperationException">An image is empty or block-compressed, or the mask has no alpha channel.</exception>
    /// <exception cref="ObjectDisposedException">An image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the copy commits.</exception>
    public void BlitRectMask(Image source, Image mask, RectI sourceRect, Vector2I destination) =>
        Composite(source, mask, sourceRect, destination, blend: false);

    /// <summary>Alpha-composites source pixels whose corresponding mask alpha is nonzero.</summary>
    /// <param name="source">The straight-alpha source image.</param>
    /// <param name="mask">A same-sized mask image.</param>
    /// <param name="sourceRect">The half-open source rectangle.</param>
    /// <param name="destination">The target coordinate corresponding to <paramref name="sourceRect"/>'s position.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="mask"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Formats or source/mask dimensions violate the operation contract.</exception>
    /// <exception cref="InvalidOperationException">An image is empty or block-compressed, or source or mask lacks alpha.</exception>
    /// <exception cref="ObjectDisposedException">An image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the blend commits.</exception>
    public void BlendRectMask(Image source, Image mask, RectI sourceRect, Vector2I destination) =>
        Composite(source, mask, sourceRect, destination, blend: true);

    /// <summary>Adjusts RGB brightness, contrast, and saturation in every stored level.</summary>
    /// <param name="brightness">The RGB multiplier; <c>1</c> preserves brightness.</param>
    /// <param name="contrast">The interpolation factor away from middle gray; <c>1</c> preserves contrast.</param>
    /// <param name="saturation">The interpolation factor away from the RGB average; <c>1</c> preserves saturation.</param>
    /// <exception cref="ArgumentOutOfRangeException">A factor is not finite.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the adjustment commits.</exception>
    public void AdjustBcs(float brightness, float contrast, float saturation)
    {
        if (!float.IsFinite(brightness)) throw new ArgumentOutOfRangeException(nameof(brightness));
        if (!float.IsFinite(contrast)) throw new ArgumentOutOfRangeException(nameof(contrast));
        if (!float.IsFinite(saturation)) throw new ArgumentOutOfRangeException(nameof(saturation));

        Mutate(
            state =>
            {
                RequireReadablePixels(state);
                var bytes = GetBytesPerPixel(state.Format);
                for (var offset = 0; offset < state.Data.Length; offset += bytes)
                {
                    var color = ReadColor(state.Data, offset, state.Format);
                    var red = 0.5f + ((color.R * brightness) - 0.5f) * contrast;
                    var green = 0.5f + ((color.G * brightness) - 0.5f) * contrast;
                    var blue = 0.5f + ((color.B * brightness) - 0.5f) * contrast;
                    var center = (red + green + blue) / 3f;
                    WriteColor(
                        state.Data,
                        offset,
                        state.Format,
                        new Color(
                            center + ((red - center) * saturation),
                            center + ((green - center) * saturation),
                            center + ((blue - center) * saturation),
                            color.A));
                }

                return state;
            });
    }

    /// <summary>Copies nearby opaque RGB values into low-alpha pixels of an RGBA8 image.</summary>
    /// <remarks>Search is limited to a four-pixel radius. Alpha values are not changed.</remarks>
    /// <exception cref="InvalidOperationException">The image is empty, compressed, or not <see cref="Format.Rgba8"/>.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the operation commits.</exception>
    public void FixAlphaEdges() => Mutate(
        state =>
        {
            RequireReadablePixels(state);
            if (state.Format != Format.Rgba8)
                throw new InvalidOperationException("Alpha-edge repair requires Rgba8 data.");

            var source = (byte[])state.Data.Clone();
            for (var y = 0; y < state.Height; y++)
            {
                for (var x = 0; x < state.Width; x++)
                {
                    var offset = ((y * state.Width) + x) * 4;
                    if (source[offset + 3] >= 20)
                        continue;

                    var bestDistance = int.MaxValue;
                    var bestOffset = -1;
                    for (var sampleY = Math.Max(0, y - 4); sampleY <= Math.Min(state.Height - 1, y + 4); sampleY++)
                    {
                        for (var sampleX = Math.Max(0, x - 4); sampleX <= Math.Min(state.Width - 1, x + 4); sampleX++)
                        {
                            var sampleOffset = ((sampleY * state.Width) + sampleX) * 4;
                            if (source[sampleOffset + 3] < 20)
                                continue;
                            var dx = x - sampleX;
                            var dy = y - sampleY;
                            var distance = (dx * dx) + (dy * dy);
                            if (distance >= bestDistance)
                                continue;
                            bestDistance = distance;
                            bestOffset = sampleOffset;
                        }
                    }

                    if (bestOffset >= 0)
                        Buffer.BlockCopy(source, bestOffset, state.Data, offset, 3);
                }
            }

            return RebuildMipmapsIfNeeded(state);
        });

    /// <summary>Multiplies every stored RGB component by its alpha component.</summary>
    /// <exception cref="InvalidOperationException">The image is empty, block-compressed, or not <see cref="Format.Rgba8"/>.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the operation commits.</exception>
    public void PremultiplyAlpha() => Mutate(
        state =>
        {
            RequireReadablePixels(state);
            if (state.Format != Format.Rgba8)
                throw new InvalidOperationException("Alpha premultiplication requires Rgba8 data.");
            var baseLength = GetLevelByteCount(state.Width, state.Height, state.Format);
            for (var offset = 0; offset < baseLength; offset += 4)
            {
                var alpha = state.Data[offset + 3];
                state.Data[offset] = (byte)(((state.Data[offset] * alpha) + 255) >> 8);
                state.Data[offset + 1] = (byte)(((state.Data[offset + 1] * alpha) + 255) >> 8);
                state.Data[offset + 2] = (byte)(((state.Data[offset + 2] * alpha) + 255) >> 8);
            }

            return RebuildMipmapsIfNeeded(state);
        });

    /// <summary>Converts RGB components from nonlinear sRGB encoding to linear encoding.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or is not RGB8 or RGBA8.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the conversion commits.</exception>
    public void SRGBToLinear() => TransformRGB8Colors(static color => color.SRGBToLinear());

    /// <summary>Converts RGB components from linear encoding to nonlinear sRGB encoding.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or is not RGB8 or RGBA8.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the conversion commits.</exception>
    public void LinearToSRGB() => TransformRGB8Colors(static color => color.LinearToSRGB());

    /// <summary>Converts a height image into a wrapping RGBA8 tangent-space normal map.</summary>
    /// <param name="bumpScale">The finite multiplier applied to neighboring height differences.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bumpScale"/> is not finite.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the conversion commits.</exception>
    public void BumpMapToNormalMap(float bumpScale = 1f)
    {
        if (!float.IsFinite(bumpScale))
            throw new ArgumentOutOfRangeException(nameof(bumpScale));

        Mutate(
            state =>
            {
                RequireReadablePixels(state);
                var source = ConvertState(state with { HasMipmaps = false, Data = BaseData(state) }, Format.Rf);
                var result = new byte[GetRequiredDataSize(state.Width, state.Height, Format.Rgba8, false)];
                for (var y = 0; y < state.Height; y++)
                {
                    var below = (y + 1) % state.Height;
                    for (var x = 0; x < state.Width; x++)
                    {
                        var right = (x + 1) % state.Width;
                        var here = ReadColor(source.Data, ((y * state.Width) + x) * 4, Format.Rf).R;
                        var toRight = ReadColor(source.Data, ((y * state.Width) + right) * 4, Format.Rf).R;
                        var down = ReadColor(source.Data, ((below * state.Width) + x) * 4, Format.Rf).R;
                        var nx = (here - toRight) * bumpScale;
                        var ny = (down - here) * bumpScale;
                        var nz = 1f;
                        var inverseLength = 1f / MathF.Sqrt((nx * nx) + (ny * ny) + 1f);
                        WriteColor(
                            result,
                            ((y * state.Width) + x) * 4,
                            Format.Rgba8,
                            new Color(
                                0.5f + (nx * inverseLength * 0.5f),
                                0.5f + (ny * inverseLength * 0.5f),
                                0.5f + (nz * inverseLength * 0.5f),
                                1f));
                    }
                }

                return new State(state.Width, state.Height, Format.Rgba8, false, result);
            });
    }

    /// <summary>Packs normal-map X and Y components into LA8 luminance and alpha storage.</summary>
    /// <exception cref="InvalidOperationException">The image is empty or block-compressed.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    /// <exception cref="Exception">A change subscriber throws after the conversion commits.</exception>
    public void NormalMapToXy() => Mutate(
        state =>
        {
            state = ConvertState(RequireReadablePixels(state), Format.Rgba8);
            var result = new byte[(state.Data.Length / 4) * 2];
            for (var source = 0; source < state.Data.Length; source += 4)
            {
                var destination = (source / 4) * 2;
                result[destination] = state.Data[source + 1];
                result[destination + 1] = state.Data[source];
            }

            return state with { Format = Format.La8, Data = result };
        });

    /// <summary>Creates an RGB8 image by decoding RGBE9995 pixels and applying linear-to-sRGB conversion.</summary>
    /// <returns>A new independent image that preserves the source mipmap policy.</returns>
    /// <exception cref="InvalidOperationException">The image is empty or is not RGBE9995.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposing or disposed.</exception>
    public Image RGBEToSRGB()
    {
        var state = RequireReadablePixels();
        if (state.Format != Format.Rgbe9995)
            throw new InvalidOperationException("RGBE conversion requires Rgbe9995 data.");

        var resultState = ConvertState(state, Format.Rgbf);
        var bytes = GetBytesPerPixel(resultState.Format);
        for (var offset = 0; offset < resultState.Data.Length; offset += bytes)
            WriteColor(resultState.Data, offset, resultState.Format, ReadColor(resultState.Data, offset, resultState.Format).LinearToSRGB());
        resultState = ConvertState(resultState, Format.Rgb8);
        var result = new Image();
        result.SetStateWithoutNotification(resultState);
        return result;
    }

    /// <summary>Computes absolute-error metrics against another image over their common base-level area.</summary>
    /// <param name="comparedImage">The image to compare.</param>
    /// <param name="useLuma">Whether to compare Rec. 709 luminance instead of four RGBA components.</param>
    /// <returns>Maximum, mean, squared, root-mean-squared, and peak signal-to-noise values in 8-bit units.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparedImage"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Either image is empty, compressed, or contains a component outside <c>0..1</c>.</exception>
    /// <exception cref="ObjectDisposedException">Either image is disposing or disposed.</exception>
    public ImageMetrics ComputeImageMetrics(Image comparedImage, bool useLuma)
    {
        ArgumentNullException.ThrowIfNull(comparedImage);
        var first = RequireReadablePixels();
        var second = comparedImage.RequireReadablePixels();
        var width = Math.Min(first.Width, second.Width);
        var height = Math.Min(first.Height, second.Height);
        var histogram = new long[256];
        var firstBytes = GetBytesPerPixel(first.Format);
        var secondBytes = GetBytesPerPixel(second.Format);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var a = ReadColor(first.Data, ((y * first.Width) + x) * firstBytes, first.Format);
                var b = ReadColor(second.Data, ((y * second.Width) + x) * secondBytes, second.Format);
                ValidateMetricColor(a);
                ValidateMetricColor(b);
                if (useLuma)
                {
                    histogram[Math.Abs(ToLumaByte(a) - ToLumaByte(b))]++;
                }
                else
                {
                    histogram[Math.Abs(ToByte(a.R) - ToByte(b.R))]++;
                    histogram[Math.Abs(ToByte(a.G) - ToByte(b.G))]++;
                    histogram[Math.Abs(ToByte(a.B) - ToByte(b.B))]++;
                    histogram[Math.Abs(ToByte(a.A) - ToByte(b.A))]++;
                }
            }
        }

        double maximum = 0;
        double sum = 0;
        double squaredSum = 0;
        for (var error = 0; error < histogram.Length; error++)
        {
            if (histogram[error] == 0)
                continue;
            maximum = error;
            sum += error * histogram[error];
            squaredSum += error * error * histogram[error];
        }

        var values = (double)width * height * (useLuma ? 1 : 4);
        var mean = sum / values;
        var meanSquared = squaredSum / values;
        var rootMeanSquared = Math.Sqrt(meanSquared);
        var peak = rootMeanSquared == 0d ? 500d : Math.Clamp(Math.Log10(255d / rootMeanSquared) * 20d, 0d, 500d);
        return new ImageMetrics(maximum, mean, meanSquared, rootMeanSquared, peak);
    }

    private void Composite(Image source, Image? mask, RectI sourceRect, Vector2I destination, bool blend)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sourceState = source.RequireReadablePixels();
        State? maskState = null;
        if (mask is not null)
        {
            maskState = mask.RequireReadablePixels();
            if (maskState.Value.Width != sourceState.Width || maskState.Value.Height != sourceState.Height)
                throw new ArgumentException("Source and mask dimensions must match.", nameof(mask));
            if (!HasAlpha(maskState.Value.Format))
                throw new InvalidOperationException("The mask format must contain alpha.");
            if (blend && !HasAlpha(sourceState.Format))
                throw new InvalidOperationException("Masked blending requires source alpha.");
        }

        Mutate(
            destinationState =>
            {
                RequireReadablePixels(destinationState);
                if (destinationState.Format != sourceState.Format)
                    throw new ArgumentException("Source and destination formats must match.", nameof(source));

                var clippedSource = ClipRectangle(sourceRect, sourceState.Width, sourceState.Height);
                if (!clippedSource.HasArea())
                    return destinationState;

                var shift = clippedSource.Position - sourceRect.Position;
                var targetStart = destination + shift;
                var bytes = GetBytesPerPixel(sourceState.Format);
                var maskBytes = maskState is null ? 0 : GetBytesPerPixel(maskState.Value.Format);
                for (var y = 0; y < clippedSource.Size.Y; y++)
                {
                    var targetY = targetStart.Y + y;
                    if ((uint)targetY >= (uint)destinationState.Height)
                        continue;
                    for (var x = 0; x < clippedSource.Size.X; x++)
                    {
                        var targetX = targetStart.X + x;
                        if ((uint)targetX >= (uint)destinationState.Width)
                            continue;
                        var sourceX = clippedSource.Position.X + x;
                        var sourceY = clippedSource.Position.Y + y;
                        if (maskState is not null &&
                            ReadColor(maskState.Value.Data, ((sourceY * maskState.Value.Width) + sourceX) * maskBytes, maskState.Value.Format).A <= 0f)
                            continue;

                        var sourceColor = ReadColor(sourceState.Data, ((sourceY * sourceState.Width) + sourceX) * bytes, sourceState.Format);
                        var destinationOffset = ((targetY * destinationState.Width) + targetX) * bytes;
                        if (blend)
                        {
                            var destinationColor = ReadColor(destinationState.Data, destinationOffset, destinationState.Format);
                            if (sourceState.Format == Format.Rgba16I)
                            {
                                sourceColor.A /= 65535f;
                                destinationColor.A /= 65535f;
                                sourceColor = destinationColor.Blend(sourceColor);
                                sourceColor.A *= 65535f;
                            }
                            else
                            {
                                sourceColor = destinationColor.Blend(sourceColor);
                            }
                        }
                        WriteColor(destinationState.Data, destinationOffset, destinationState.Format, sourceColor);
                    }
                }

                return RebuildMipmapsIfNeeded(destinationState);
            });
    }

    private void TransformRGB8Colors(Func<Color, Color> transform) => Mutate(
        state =>
        {
            RequireReadablePixels(state);
            if (state.Format is not (Format.Rgb8 or Format.Rgba8))
                throw new InvalidOperationException("The color-space conversion requires Rgb8 or Rgba8 data.");
            var bytes = GetBytesPerPixel(state.Format);
            for (var offset = 0; offset < state.Data.Length; offset += bytes)
                WriteColor(state.Data, offset, state.Format, transform(ReadColor(state.Data, offset, state.Format)));
            return state;
        });

    private void Mutate(Func<State, State> operation)
    {
        ThrowIfDisposed();
        lock (_mutationGate)
        {
            State current;
            lock (_stateGate)
            {
                ThrowIfDisposed();
                current = CaptureState(copyData: true);
            }

            var next = operation(current);
            SetStateWithoutNotification(next);
        }

        EmitChanged();
    }

    private State RequireReadablePixels() => RequireReadablePixels(Snapshot());

    private static State RequireReadablePixels(State state)
    {
        if (state.Data.Length == 0)
            throw new InvalidOperationException("The image is empty.");
        if (IsCompressedFormat(state.Format))
            throw new InvalidOperationException("The operation requires uncompressed pixel data.");
        return state;
    }

    private static State ConvertState(State state, Format destinationFormat)
    {
        ValidateFormat(destinationFormat);
        RequireReadablePixels(state);
        if (IsCompressedFormat(destinationFormat))
            throw new InvalidOperationException("Conversion to a block-compressed format requires a compression backend.");
        if (state.Format == destinationFormat)
            return state;

        var levelCount = GetRequiredMipmapCount(state.Width, state.Height, state.HasMipmaps) + 1;
        var data = new byte[GetRequiredDataSize(state.Width, state.Height, destinationFormat, state.HasMipmaps)];
        var sourceBytes = GetBytesPerPixel(state.Format);
        var destinationBytes = GetBytesPerPixel(destinationFormat);
        var destinationOffset = 0;
        for (var levelIndex = 0; levelIndex < levelCount; levelIndex++)
        {
            var level = GetLevel(state, levelIndex);
            var pixels = level.Width * level.Height;
            for (var pixel = 0; pixel < pixels; pixel++)
            {
                var color = ReadColor(state.Data, level.Offset + (pixel * sourceBytes), state.Format);
                WriteColor(data, destinationOffset + (pixel * destinationBytes), destinationFormat, color);
            }

            destinationOffset += pixels * destinationBytes;
        }

        return new State(state.Width, state.Height, destinationFormat, state.HasMipmaps, data);
    }

    private static State BuildMipmaps(State state, bool renormalize)
    {
        RequireReadablePixels(state);
        var mipCount = GetRequiredMipmapCount(state.Width, state.Height, true);
        if (mipCount == 0)
            return state with { HasMipmaps = false, Data = BaseData(state) };

        var data = new byte[GetRequiredDataSize(state.Width, state.Height, state.Format, true)];
        var baseData = BaseData(state);
        Buffer.BlockCopy(baseData, 0, data, 0, baseData.Length);
        var bytes = GetBytesPerPixel(state.Format);
        var sourceOffset = 0;
        var destinationOffset = baseData.Length;
        var sourceWidth = state.Width;
        var sourceHeight = state.Height;
        for (var level = 1; level <= mipCount; level++)
        {
            var destinationWidth = Math.Max(1, sourceWidth / 2);
            var destinationHeight = Math.Max(1, sourceHeight / 2);
            for (var y = 0; y < destinationHeight; y++)
            {
                for (var x = 0; x < destinationWidth; x++)
                {
                    var x0 = Math.Min(sourceWidth - 1, x * 2);
                    var x1 = Math.Min(sourceWidth - 1, x0 + 1);
                    var y0 = Math.Min(sourceHeight - 1, y * 2);
                    var y1 = Math.Min(sourceHeight - 1, y0 + 1);
                    var color = Average(
                        ReadColor(data, sourceOffset + (((y0 * sourceWidth) + x0) * bytes), state.Format),
                        ReadColor(data, sourceOffset + (((y0 * sourceWidth) + x1) * bytes), state.Format),
                        ReadColor(data, sourceOffset + (((y1 * sourceWidth) + x0) * bytes), state.Format),
                        ReadColor(data, sourceOffset + (((y1 * sourceWidth) + x1) * bytes), state.Format));
                    if (renormalize)
                        color = RenormalizeNormal(color);
                    WriteColor(data, destinationOffset + (((y * destinationWidth) + x) * bytes), state.Format, color);
                }
            }

            sourceOffset = destinationOffset;
            destinationOffset += destinationWidth * destinationHeight * bytes;
            sourceWidth = destinationWidth;
            sourceHeight = destinationHeight;
        }

        return new State(state.Width, state.Height, state.Format, true, data);
    }

    private static State RebuildMipmapsIfNeeded(State state) => state.HasMipmaps ? BuildMipmaps(state, false) : state;

    private static byte[] BaseData(State state)
    {
        var length = GetLevelByteCount(state.Width, state.Height, state.Format);
        return state.Data[..length];
    }

    private static State CropState(State state, int width, int height)
    {
        var bytes = GetBytesPerPixel(state.Format);
        var data = new byte[GetRequiredDataSize(width, height, state.Format, false)];
        var copyWidth = Math.Min(width, state.Width);
        var copyHeight = Math.Min(height, state.Height);
        for (var y = 0; y < copyHeight; y++)
            Buffer.BlockCopy(state.Data, y * state.Width * bytes, data, y * width * bytes, copyWidth * bytes);
        var result = new State(width, height, state.Format, false, data);
        return state.HasMipmaps ? BuildMipmaps(result, false) : result;
    }

    private static State FlipState(State state, bool horizontal)
    {
        var data = new byte[state.Data.Length];
        var bytes = GetBytesPerPixel(state.Format);
        var levelCount = GetRequiredMipmapCount(state.Width, state.Height, state.HasMipmaps) + 1;
        for (var levelIndex = 0; levelIndex < levelCount; levelIndex++)
        {
            var level = GetLevel(state, levelIndex);
            for (var y = 0; y < level.Height; y++)
            {
                for (var x = 0; x < level.Width; x++)
                {
                    var sourceX = horizontal ? level.Width - 1 - x : x;
                    var sourceY = horizontal ? y : level.Height - 1 - y;
                    Buffer.BlockCopy(
                        state.Data,
                        level.Offset + (((sourceY * level.Width) + sourceX) * bytes),
                        data,
                        level.Offset + (((y * level.Width) + x) * bytes),
                        bytes);
                }
            }
        }

        return state with { Data = data };
    }

    private static State Rotate90State(State state, ClockDirection direction)
    {
        var bytes = GetBytesPerPixel(state.Format);
        var data = new byte[state.Width * state.Height * bytes];
        for (var y = 0; y < state.Height; y++)
        {
            for (var x = 0; x < state.Width; x++)
            {
                var destinationX = direction == ClockDirection.Clockwise ? state.Height - 1 - y : y;
                var destinationY = direction == ClockDirection.Clockwise ? x : state.Width - 1 - x;
                Buffer.BlockCopy(
                    state.Data,
                    ((y * state.Width) + x) * bytes,
                    data,
                    ((destinationY * state.Height) + destinationX) * bytes,
                    bytes);
            }
        }

        var result = new State(state.Height, state.Width, state.Format, false, data);
        return state.HasMipmaps ? BuildMipmaps(result, false) : result;
    }

    private static State ResizeState(State state, int width, int height, Interpolation interpolation)
    {
        if (width == state.Width && height == state.Height)
            return state;

        var result = interpolation == Interpolation.Trilinear
            ? ResizeTrilinear(state, width, height)
            : ResizeLevel(state, 0, width, height, interpolation);
        return state.HasMipmaps ? BuildMipmaps(result, false) : result;
    }

    private static State ResizeTrilinear(State state, int width, int height)
    {
        var mipState = state.HasMipmaps ? state : BuildMipmaps(state, false);
        var scale = Math.Max(state.Width / (double)width, state.Height / (double)height);
        var level = Math.Max(0d, Math.Log2(Math.Max(1d, scale)));
        var maximum = GetRequiredMipmapCount(mipState.Width, mipState.Height, mipState.HasMipmaps);
        var lower = Math.Min(maximum, (int)Math.Floor(level));
        var upper = Math.Min(maximum, lower + 1);
        var weight = (float)(level - lower);
        var lowerState = ResizeLevel(mipState, lower, width, height, Interpolation.Bilinear);
        if (upper == lower)
            return lowerState;
        var upperState = ResizeLevel(mipState, upper, width, height, Interpolation.Bilinear);
        var bytes = GetBytesPerPixel(state.Format);
        for (var offset = 0; offset < lowerState.Data.Length; offset += bytes)
        {
            var color = ReadColor(lowerState.Data, offset, state.Format)
                .Lerp(ReadColor(upperState.Data, offset, state.Format), weight);
            WriteColor(lowerState.Data, offset, state.Format, color);
        }

        return lowerState;
    }

    private static State ResizeLevel(State state, int levelIndex, int width, int height, Interpolation interpolation)
    {
        var level = GetLevel(state, levelIndex);
        var bytes = GetBytesPerPixel(state.Format);
        var data = new byte[GetRequiredDataSize(width, height, state.Format, false)];
        for (var y = 0; y < height; y++)
        {
            var sourceY = ((y + 0.5) * level.Height / height) - 0.5;
            for (var x = 0; x < width; x++)
            {
                var sourceX = ((x + 0.5) * level.Width / width) - 0.5;
                var color = interpolation switch
                {
                    Interpolation.Nearest => SampleNearest(state, level, sourceX, sourceY),
                    Interpolation.Bilinear => SampleBilinear(state, level, sourceX, sourceY),
                    Interpolation.Cubic => SampleCubic(state, level, sourceX, sourceY),
                    Interpolation.Lanczos => SampleLanczos(state, level, sourceX, sourceY),
                    _ => throw new ArgumentOutOfRangeException(nameof(interpolation)),
                };
                WriteColor(data, ((y * width) + x) * bytes, state.Format, color);
            }
        }

        return new State(width, height, state.Format, false, data);
    }

    private static Color SampleNearest(State state, Level level, double x, double y) =>
        ReadLevelPixel(state, level, (int)Math.Floor(x + 0.5), (int)Math.Floor(y + 0.5));

    private static Color SampleBilinear(State state, Level level, double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var tx = (float)(x - x0);
        var ty = (float)(y - y0);
        var top = ReadLevelPixel(state, level, x0, y0).Lerp(ReadLevelPixel(state, level, x0 + 1, y0), tx);
        var bottom = ReadLevelPixel(state, level, x0, y0 + 1).Lerp(ReadLevelPixel(state, level, x0 + 1, y0 + 1), tx);
        return top.Lerp(bottom, ty);
    }

    private static Color SampleCubic(State state, Level level, double x, double y)
    {
        var baseX = (int)Math.Floor(x);
        var baseY = (int)Math.Floor(y);
        var result = default(Color);
        var total = 0f;
        for (var sampleY = -1; sampleY <= 2; sampleY++)
        {
            var wy = CubicKernel((float)(y - (baseY + sampleY)));
            for (var sampleX = -1; sampleX <= 2; sampleX++)
            {
                var weight = wy * CubicKernel((float)(x - (baseX + sampleX)));
                result += ReadLevelPixel(state, level, baseX + sampleX, baseY + sampleY) * weight;
                total += weight;
            }
        }

        return total == 0f ? default : result / total;
    }

    private static Color SampleLanczos(State state, Level level, double x, double y)
    {
        var baseX = (int)Math.Floor(x);
        var baseY = (int)Math.Floor(y);
        var result = default(Color);
        var total = 0f;
        for (var sampleY = -2; sampleY <= 3; sampleY++)
        {
            var wy = LanczosKernel((float)(y - (baseY + sampleY)));
            for (var sampleX = -2; sampleX <= 3; sampleX++)
            {
                var weight = wy * LanczosKernel((float)(x - (baseX + sampleX)));
                result += ReadLevelPixel(state, level, baseX + sampleX, baseY + sampleY) * weight;
                total += weight;
            }
        }

        return total == 0f ? default : result / total;
    }

    private static Color ReadLevelPixel(State state, Level level, int x, int y)
    {
        x = Math.Clamp(x, 0, level.Width - 1);
        y = Math.Clamp(y, 0, level.Height - 1);
        return ReadColor(state.Data, level.Offset + (((y * level.Width) + x) * GetBytesPerPixel(state.Format)), state.Format);
    }

    private static float CubicKernel(float value)
    {
        value = MathF.Abs(value);
        if (value <= 1f)
            return ((1.5f * value) - 2.5f) * value * value + 1f;
        if (value < 2f)
            return (((-0.5f * value) + 2.5f) * value - 4f) * value + 2f;
        return 0f;
    }

    private static float LanczosKernel(float value)
    {
        value = MathF.Abs(value);
        if (value == 0f)
            return 1f;
        if (value >= 3f)
            return 0f;
        var radians = MathF.PI * value;
        return (MathF.Sin(radians) / radians) * (MathF.Sin(radians / 3f) / (radians / 3f));
    }

    private static Color Average(Color a, Color b, Color c, Color d) => (a + b + c + d) / 4f;

    private static Color RenormalizeNormal(Color color)
    {
        var x = (color.R * 2f) - 1f;
        var y = (color.G * 2f) - 1f;
        var z = (color.B * 2f) - 1f;
        var length = MathF.Sqrt((x * x) + (y * y) + (z * z));
        if (length == 0f)
            return new Color(0.5f, 0.5f, 1f, color.A);
        return new Color(0.5f + (x / length * 0.5f), 0.5f + (y / length * 0.5f), 0.5f + (z / length * 0.5f), color.A);
    }

    private static RectI ClipRectangle(RectI rectangle, int width, int height)
    {
        if (!rectangle.HasArea())
            return default;
        var left = Math.Clamp(rectangle.Position.X, 0, width);
        var top = Math.Clamp(rectangle.Position.Y, 0, height);
        var right = Math.Clamp((long)rectangle.Position.X + rectangle.Size.X, 0L, width);
        var bottom = Math.Clamp((long)rectangle.Position.Y + rectangle.Size.Y, 0L, height);
        return right <= left || bottom <= top
            ? default
            : new RectI(left, top, checked((int)right - left), checked((int)bottom - top));
    }

    private static bool HasAlpha(Format format) => format is Format.La8 or Format.Rgba8 or Format.Rgba4444 or
        Format.Rgbaf or Format.Rgbah or Format.Rgba16 or Format.Rgba16I;

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 1)
            return 1;
        return checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)value));
    }

    private static void ValidateInterpolation(Interpolation interpolation)
    {
        if (!Enum.IsDefined(interpolation))
            throw new ArgumentOutOfRangeException(nameof(interpolation));
    }

    private static void ValidateMetricColor(Color color)
    {
        if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A) ||
            color.R < 0f || color.R > 1f || color.G < 0f || color.G > 1f || color.B < 0f || color.B > 1f ||
            color.A < 0f || color.A > 1f)
            throw new InvalidOperationException("Image metrics require finite normalized color components.");
    }

    private static int ToLumaByte(Color color) =>
        ((13_938 * ToByte(color.R)) + (46_869 * ToByte(color.G)) + (4_729 * ToByte(color.B)) + 32_768) >> 16;
}
