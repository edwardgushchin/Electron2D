namespace Electron2D;

/// <summary>Defines scalar noise sampling and converts two-dimensional samples into grayscale images.</summary>
/// <remarks>Derived resources supply the sampling function. Image generation reads a coherent sequence of samples;
/// callers must coordinate concurrent changes to derived generator state.</remarks>
public abstract class Noise : Resource
{
    /// <summary>Constructs a noise resource for a concrete sampler.</summary>
    protected Noise() { }

    /// <summary>Samples noise at a one-dimensional coordinate.</summary>
    /// <param name="x">The sample coordinate.</param>
    /// <returns>The noise value.</returns>
    public abstract float GetNoise1D(float x);

    /// <summary>Samples noise at a two-dimensional coordinate.</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>The noise value.</returns>
    public abstract float GetNoise2D(float x, float y);

    /// <summary>Samples noise at a two-dimensional position.</summary>
    /// <param name="position">The sample position.</param>
    /// <returns>The noise value.</returns>
    public virtual float GetNoise2DV(Vector2 position)
    {
        ThrowIfDisposed();
        return GetNoise2D(position.X, position.Y);
    }

    /// <summary>Samples an L8 image from two-dimensional noise.</summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="invert">Whether to invert the resulting byte values.</param>
    /// <param name="normalize">Whether to scale the sampled minimum and maximum to zero and 255.</param>
    /// <returns>A new independent L8 image.</returns>
    /// <remarks>Without normalization, noise values are mapped from the expected range [-1, 1]. Constant normalized noise maps to zero before inversion.</remarks>
    public virtual Image GetImage(int width, int height, bool invert = false, bool normalize = true)
    {
        ThrowIfDisposed();
        ValidateSize(width, height);
        var values = new float[checked(width * height)];
        var minimum = float.MaxValue;
        var maximum = -float.MaxValue;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var value = GetNoise2D(x, y);
                if (!float.IsFinite(value)) throw new InvalidOperationException("The noise generator returned a nonfinite sample.");
                values[x + y * width] = value;
                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }

        var pixels = new byte[values.Length];
        for (var index = 0; index < pixels.Length; index++)
        {
            var scaled = normalize ? maximum == minimum ? 0d : ((double)values[index] - minimum) / ((double)maximum - minimum) * 255d
                : values[index] * 127.5f + 127.5f;
            var value = (byte)Math.Clamp(scaled, 0d, 255d);
            pixels[index] = invert ? (byte)(255 - value) : value;
        }
        return Image.CreateFromData(width, height, false, Image.Format.L8, pixels);
    }

    /// <summary>Blends overlapping strips of a larger noise image into a seamless two-dimensional image.</summary>
    /// <param name="width">The positive output width.</param>
    /// <param name="height">The positive output height.</param>
    /// <param name="invert">Whether to invert samples before blending.</param>
    /// <param name="skirt">The nonnegative fraction of each output dimension added as an overlap.</param>
    /// <param name="normalize">Whether to normalize the larger source image before blending.</param>
    /// <returns>A new independent image in the source format.</returns>
    /// <remarks>The default algorithm accepts L8 and RGBA8 source images. Derived generators may override it with a native seamless algorithm.</remarks>
    public virtual Image GetSeamlessImage(int width, int height, bool invert = false, float skirt = 0.1f, bool normalize = true)
    {
        ThrowIfDisposed();
        ValidateSize(width, height);
        if (!float.IsFinite(skirt) || skirt < 0f) throw new ArgumentOutOfRangeException(nameof(skirt));
        if (width * (double)skirt >= 16_384 || height * (double)skirt >= 16_384)
            throw new ArgumentOutOfRangeException(nameof(skirt));
        var skirtWidth = Math.Max(1, checked((int)(width * (double)skirt)));
        var skirtHeight = Math.Max(1, checked((int)(height * (double)skirt)));
        var sourceWidth = checked(width + skirtWidth);
        var sourceHeight = checked(height + skirtHeight);
        ValidateSize(sourceWidth, sourceHeight);

        using var source = GetImage(sourceWidth, sourceHeight, invert, normalize);
        if (source is null || source.Width != sourceWidth || source.Height != sourceHeight || source.HasMipmaps)
            throw new InvalidOperationException("The noise generator returned an incompatible source image.");
        var channels = source.PixelFormat switch
        {
            Image.Format.L8 => 1,
            Image.Format.Rgba8 => 4,
            _ => throw new NotSupportedException("Seamless blending requires an L8 or RGBA8 source image."),
        };
        var sourcePixels = source.GetData();
        var result = new byte[checked(width * height * channels)];
        var halfWidth = width / 2;
        var halfHeight = height / 2;
        var edgeX = Math.Min(width, checked(halfWidth + skirtWidth));
        var edgeY = Math.Min(height, checked(halfHeight + skirtHeight));

        byte Sample(int x, int y, int channel, int moduloWidth, int moduloHeight)
        {
            var sx = (x + halfWidth) % moduloWidth;
            var sy = (y + halfHeight) % moduloHeight;
            return sourcePixels[(sx + sy * sourceWidth) * channels + channel];
        }

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                for (var channel = 0; channel < channels; channel++)
                    result[(x + y * width) * channels + channel] = Sample(x, y, channel, width, height);

        for (var x = halfWidth; x < edgeX; x++)
        {
            var alpha = BlendAlpha(x - halfWidth, skirtWidth);
            for (var y = 0; y < height; y++)
            {
                if (y >= halfHeight && y < edgeY) continue;
                for (var channel = 0; channel < channels; channel++)
                {
                    var index = (x + y * width) * channels + channel;
                    result[index] = Blend(result[index], Sample(x, y, channel, sourceWidth, height), alpha);
                }
            }
        }

        for (var y = halfHeight; y < edgeY; y++)
        {
            var alpha = BlendAlpha(y - halfHeight, skirtHeight);
            for (var x = 0; x < width; x++)
            {
                if (x >= halfWidth && x < edgeX) continue;
                for (var channel = 0; channel < channels; channel++)
                {
                    var index = (x + y * width) * channels + channel;
                    result[index] = Blend(result[index], Sample(x, y, channel, width, sourceHeight), alpha);
                }
            }
        }

        for (var y = halfHeight; y < edgeY; y++)
            for (var x = halfWidth; x < edgeX; x++)
            {
                var alphaX = BlendAlpha(x - halfWidth, skirtWidth);
                var alphaY = BlendAlpha(y - halfHeight, skirtHeight);
                for (var channel = 0; channel < channels; channel++)
                {
                    var upper = Blend(Sample(x, y, channel, width, sourceHeight),
                        Sample(x, y, channel, sourceWidth, sourceHeight), alphaX);
                    var lower = Blend(Sample(x, y, channel, width, height),
                        Sample(x, y, channel, sourceWidth, height), alphaX);
                    result[(x + y * width) * channels + channel] = Blend(lower, upper, alphaY);
                }
            }

        return Image.CreateFromData(width, height, false, source.PixelFormat, result);
    }

    private static void ValidateSize(int width, int height)
    {
        if (width is < 1 or > 16_384) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 1 or > 16_384) throw new ArgumentOutOfRangeException(nameof(height));
        if ((long)width * height > int.MaxValue / 4) throw new ArgumentOutOfRangeException(nameof(width));
    }

    private static int BlendAlpha(int distance, int skirt)
    {
        var weight = Math.Clamp(distance / (float)skirt * 1.25f - 0.125f, 0f, 1f);
        var smooth = weight * weight * (3f - 2f * weight);
        return (int)(255f * (1f - smooth));
    }

    private static byte Blend(byte background, byte foreground, int alpha) =>
        (byte)(((alpha + 1) * foreground + (256 - alpha) * background) >> 8);
}
