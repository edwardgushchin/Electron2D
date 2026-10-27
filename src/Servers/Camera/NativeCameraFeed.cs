using SDL3;

namespace Electron2D;

internal sealed class NativeCameraFeed : CameraFeed
{
    internal readonly uint DeviceID;
    private readonly SDL.CameraSpec[] _specs;
    private readonly CameraFeedFormat[] _formats;
    private nint _camera;
    private int _selected = -1;
    private CameraFeedOutput _output;
    private TexturePixels? _first, _second;
    internal NativeCameraFeed(uint id) : base(FeedDataType.NoImage)
    {
        DeviceID = id; SetName(SDL.GetCameraName(id) ?? "Camera");
        SetPosition(SDL.GetCameraPosition(id) switch { SDL.CameraPosition.FrontFacing => FeedPosition.Front, SDL.CameraPosition.BackFacing => FeedPosition.Back, _ => FeedPosition.Unspecified });
        _specs = SDL.GetCameraSupportedFormats(id, out _) ?? throw new IOException("Cannot query camera formats: " + SDL.GetError());
        if (_specs.Length > 4096) throw new IOException("Camera format count exceeds 4096.");
        _formats = new CameraFeedFormat[_specs.Length];
        for (var i = 0; i < _specs.Length; i++)
        {
            var s = _specs[i];
            _formats[i] = new(s.Width, s.Height, SDL.GetPixelFormatName(s.PixelFormat) ?? "Unknown", Math.Max(0, s.FramerateNumerator), Math.Max(1, s.FramerateDenominator));
        }
    }
    protected override CameraFeedFormat[] OnGetFormats() => _formats;
    protected override bool OnSetFormat(int index, CameraFeedOutput output)
    {
        if (_specs[index].Width <= 0 || _specs[index].Height <= 0 || (long)_specs[index].Width * _specs[index].Height * 8 > 67108864) return false;
        if (output != CameraFeedOutput.RGB && (_specs[index].PixelFormat != SDL.PixelFormat.YUY2 || (_specs[index].Width & 1) != 0)) return false;
        _selected = index; _output = output; return true;
    }
    protected override bool OnActivateFeed()
    {
        var selected = _selected;
        if (selected < 0)
            for (var i = 0; i < _specs.Length; i++)
                if (_specs[i].Width > 0 && _specs[i].Height > 0 && (long)_specs[i].Width * _specs[i].Height * 8 <= 67108864) { selected = i; break; }
        if (selected < 0) throw new NotSupportedException("No camera format fits the bounded frame storage.");
        var requested = _specs[selected];
        if (_output == CameraFeedOutput.RGB) { requested.PixelFormat = BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888; requested.Colorspace = SDL.Colorspace.SRGB; }
        _camera = SDL.OpenCamera(DeviceID, in requested);
        if (_camera == 0) throw new IOException("Cannot open camera: " + SDL.GetError());
        try
        {
            if (SDL.GetCameraPermissionState(_camera) == SDL.CameraPermissionState.Denied) throw new UnauthorizedAccessException("Camera access was denied.");
            return true;
        }
        catch { OnDeactivateFeed(); throw; }
    }
    protected override void OnDeactivateFeed()
    {
        var camera = _camera; _camera = 0;
        if (camera != 0) SDL.CloseCamera(camera);
        _first = _second = null;
    }
    internal override void Poll()
    {
        if (_camera == 0 || !FeedIsActive) return;
        var permission = SDL.GetCameraPermissionState(_camera);
        if (permission == SDL.CameraPermissionState.Pending) return;
        if (permission == SDL.CameraPermissionState.Denied) { FeedIsActive = false; throw new UnauthorizedAccessException("Camera access was denied."); }
        SDL.ClearError();
        var surface = SDL.AcquireCameraFrame(_camera, out _);
        if (surface == 0)
        {
            var error = SDL.GetError();
            if (!string.IsNullOrEmpty(error)) throw new IOException("Cannot acquire camera frame: " + error);
            return;
        }
        try { PublishSurface(surface); }
        finally { SDL.ReleaseCameraFrame(_camera, surface); }
    }
    internal unsafe void PublishSurface(nint surface)
    {
        var data = *(SDL.Surface*)surface;
        if (data.Width is <= 0 or > 16384 || data.Height is <= 0 or > 16384 || (long)data.Width * data.Height * 8 > 67108864)
            throw new InvalidDataException("Camera frame exceeds the pixel budget or has invalid storage.");
        if (_output != CameraFeedOutput.RGB && (data.Format != SDL.PixelFormat.YUY2 || (data.Width & 1) != 0 || data.Pitch < data.Width * 2))
            throw new InvalidDataException("Selected camera output requires an even-width YUYV frame.");
        var format = _output switch { CameraFeedOutput.Copy => Image.Format.Rg8, CameraFeedOutput.Separate => Image.Format.R8, _ => Image.Format.Rgba8 };
        Prepare(ref _first, data.Width, data.Height, format);
        if (_output == CameraFeedOutput.Separate) Prepare(ref _second, data.Width / 2, data.Height, Image.Format.Rg8);
        else _second = null;
        if (!SDL.LockSurface(surface)) throw new IOException("Cannot lock camera frame: " + SDL.GetError());
        try
        {
            data = *(SDL.Surface*)surface;
            if (data.Pixels == 0 || data.Pitch <= 0 || _output != CameraFeedOutput.RGB && data.Pitch < data.Width * 2)
                throw new InvalidDataException("Locked camera surface has invalid pixel storage.");
            if (_output == CameraFeedOutput.RGB)
            {
                var rgba = BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888;
                if (!SDL.ConvertPixelsAndColorspace(data.Width, data.Height, data.Format, SDL.GetSurfaceColorspace(surface), SDL.GetSurfaceProperties(surface), data.Pixels, data.Pitch, rgba, SDL.Colorspace.SRGB, 0, _first!.Source.Data.AsSpan(), data.Width * 4))
                    throw new IOException("Cannot convert camera frame: " + SDL.GetError());
            }
            else for (var y = 0; y < data.Height; y++)
                {
                    var row = new ReadOnlySpan<byte>((void*)(data.Pixels + y * data.Pitch), data.Width * 2);
                    for (var x = 0; x < data.Width; x++)
                    {
                        var at = y * data.Width + x;
                        if (_output == CameraFeedOutput.Copy) { _first!.Source.Data[at * 2] = row[x * 2]; _first.Source.Data[at * 2 + 1] = row[x * 2 + 1]; }
                        else if (_output == CameraFeedOutput.Grayscale) { var d = _first!.Source.Data; d[at * 4] = d[at * 4 + 1] = d[at * 4 + 2] = row[x * 2]; d[at * 4 + 3] = 255; }
                        else { _first!.Source.Data[at] = row[x * 2]; _second!.Source.Data[at] = row[x * 2 + 1]; }
                    }
                }
            Upload(_first!); if (_second is not null) Upload(_second);
            SubmitPrepared(_first!, _second, _output switch { CameraFeedOutput.Copy => FeedDataType.YCBCR, CameraFeedOutput.Separate => FeedDataType.YCBCRSeparate, _ => FeedDataType.RGB });
        }
        finally { SDL.UnlockSurface(surface); }
    }
    private static void Prepare(ref TexturePixels? plane, int width, int height, Image.Format format)
    {
        if (plane is not null && plane.Source.Width == width && plane.Source.Height == height && plane.Source.Format == format) return;
        plane = TexturePixels.PreparedPlane(width, height, format);
    }
    private static void Upload(TexturePixels plane)
    {
        var source = plane.Source.Data; var upload = plane.Upload.Data;
        if (plane.Source.Format == Image.Format.Rgba8) source.CopyTo(upload, 0);
        else
        {
            var size = Image.TexturePixelByteSize(plane.Source.Format);
            for (var i = 0; i < plane.Source.Width * plane.Source.Height; i++) { upload[i * 4] = source[i * size]; upload[i * 4 + 1] = size == 2 ? source[i * size + 1] : (byte)0; upload[i * 4 + 2] = 0; upload[i * 4 + 3] = 255; }
        }
        unchecked { plane.Revision++; }
    }
}
