using SDL3;

namespace Electron2D;

internal sealed class CameraBackgroundTexture : Texture
{
    internal long CapturedFrame;
    internal bool HasFrame;
    internal Transform FeedTransform;
    private TexturePixels? _pixels, _converted, _first, _second;
    private ulong _firstRevision, _secondRevision;
    private CameraFeed.FeedDataType _type;
    private SDL.Colorspace _colorspace;

    internal bool Prepare(TexturePixels first, TexturePixels? second, CameraFeed.FeedDataType type, SDL.Colorspace colorspace)
    {
        if (type == CameraFeed.FeedDataType.RGB && first.PixelType == 0)
        {
            if (_converted is not null) ClearFrame();
            _pixels = first;
            return true;
        }

        var input = first.Source;
        var packed = type == CameraFeed.FeedDataType.YCBCR;
        var separate = type == CameraFeed.FeedDataType.YCBCRSeparate;
        var yuyv = packed && input.Format == Image.Format.Rg8 && (input.Width & 1) == 0;
        var channels = packed && input.Format is Image.Format.Rgb8 or Image.Format.Rgba8;
        if (!yuyv && !channels && !(separate && input.Format == Image.Format.R8 && second?.Source.Format == Image.Format.Rg8))
            return false;
        if (colorspace is not (SDL.Colorspace.BT601Full or SDL.Colorspace.BT601Limited or
            SDL.Colorspace.BT709Full or SDL.Colorspace.BT709Limited or SDL.Colorspace.Unknown))
            return false;

        if (ReferenceEquals(first, _first) && ReferenceEquals(second, _second) && first.Revision == _firstRevision &&
            (second?.Revision ?? 0) == _secondRevision && _type == type && _colorspace == colorspace)
        {
            _pixels = _converted;
            return true;
        }

        if (_converted is not { } converted || converted.Source.Width != input.Width || converted.Source.Height != input.Height)
            _converted = converted = TexturePixels.PreparedRGBA(input.Width, input.Height);
        var limited = colorspace is SDL.Colorspace.BT601Limited or SDL.Colorspace.BT709Limited or SDL.Colorspace.Unknown;
        var bt709 = colorspace is SDL.Colorspace.BT709Full or SDL.Colorspace.BT709Limited;
        Convert(input, second?.Source, converted.Source.Data, yuyv, channels, limited, bt709);
        if (!ReferenceEquals(converted.Source.Data, converted.Upload.Data)) converted.Source.Data.CopyTo(converted.Upload.Data, 0);
        unchecked { converted.Revision++; }
        _first = first; _second = second; _firstRevision = first.Revision; _secondRevision = second?.Revision ?? 0;
        _type = type; _colorspace = colorspace; _pixels = converted;
        return true;
    }

    private static void Convert(Image.State input, Image.State? chroma, byte[] output, bool yuyv, bool channels, bool limited, bool bt709)
    {
        var width = input.Width;
        var height = input.Height;
        var stride = input.Format == Image.Format.Rgba8 ? 4 : 3;
        var yOffset = limited ? 16 : 0;
        var yScale = limited ? 255f / 219 : 1;
        var chromaScale = limited ? 255f / 224 : 1;
        var red = (bt709 ? 1.5748f : 1.402f) * chromaScale;
        var greenB = (bt709 ? .187324f : .344136f) * chromaScale;
        var greenR = (bt709 ? .468124f : .714136f) * chromaScale;
        var blue = (bt709 ? 1.8556f : 1.772f) * chromaScale;
        for (var y = 0; y < height; y++)
        {
            var chromaRow = chroma is { } plane ? ((2 * y + 1) * plane.Height / (2 * height)) * plane.Width * 2 : 0;
            for (var x = 0; x < width; x++)
            {
                var pixel = y * width + x;
                byte luma, cb, cr;
                if (yuyv)
                {
                    var pair = (pixel & ~1) * 2;
                    luma = input.Data[pixel * 2];
                    cb = input.Data[pair + 1];
                    cr = input.Data[pair + 3];
                }
                else if (channels)
                {
                    var offset = pixel * stride;
                    luma = input.Data[offset];
                    cb = input.Data[offset + 1];
                    cr = input.Data[offset + 2];
                }
                else
                {
                    var source = chroma!.Value;
                    var offset = chromaRow + ((2 * x + 1) * source.Width / (2 * width)) * 2;
                    luma = input.Data[pixel];
                    cb = source.Data[offset];
                    cr = source.Data[offset + 1];
                }
                var yy = (luma - yOffset) * yScale;
                var u = cb - 128;
                var v = cr - 128;
                var destination = pixel * 4;
                output[destination] = Encode(yy + red * v);
                output[destination + 1] = Encode(yy - greenB * u - greenR * v);
                output[destination + 2] = Encode(yy + blue * u);
                output[destination + 3] = 255;
            }
        }
        static byte Encode(float value) => (byte)Math.Clamp(value + .5f, 0, 255);
    }

    internal void ClearFrame() => _pixels = _converted = _first = _second = null;

    public override int GetWidth() => _pixels?.Source.Width ?? 0;
    public override int GetHeight() => _pixels?.Source.Height ?? 0;
    internal override TexturePixels? CapturePixels() => _pixels;

    protected override void Dispose(bool disposing)
    {
        if (disposing) ClearFrame();
        base.Dispose(disposing);
    }
}
