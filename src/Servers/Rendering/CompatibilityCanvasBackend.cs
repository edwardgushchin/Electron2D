using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed class CompatibilityCanvasBackend : CanvasBackend
{
    private readonly RenderHandle _renderer;
    private RenderHandle? _target;
    private Vector2i _targetSize;
    private SDL.Vertex[] _vertices = [];
    private readonly Dictionary<Texture, (RenderHandle Handle, TexturePixels Pixels)> _textures = [];
    private readonly HashSet<Texture> _usedTextures = [];
    private bool _hasFrame;
    private (nint Context, nint EGLDisplay, nint EGLConfig, nint GLXVisualID, nint GLXFBConfig) _graphics;
    internal override string Method => "compatibility";
    internal override string Driver { get; }

    internal CompatibilityCanvasBackend(SafeHandle window)
    {
        _renderer = new RenderHandle(SDL.CreateRenderer(window.DangerousGetHandle(), null), SDL.DestroyRenderer, window);
        try
        {
            Driver = SDL.GetRendererName(_renderer.DangerousGetHandle()) ?? "unknown";
            CaptureGraphicsHandles(window.DangerousGetHandle());
            Check(SDL.SetRenderDrawBlendMode(_renderer.DangerousGetHandle(), SDL.BlendMode.Blend), "set canvas blending");
        }
        catch { _renderer.Dispose(); throw; }
    }

    internal override nint GetNativeHandle(DisplayServer.HandleType type)
    {
        var handle = type switch
        {
            DisplayServer.HandleType.OpenGLContext => _graphics.Context,
            DisplayServer.HandleType.EGLDisplay => _graphics.EGLDisplay,
            DisplayServer.HandleType.EGLConfig => _graphics.EGLConfig,
            DisplayServer.HandleType.GLXVisualID => _graphics.GLXVisualID,
            DisplayServer.HandleType.GLXFBConfig => _graphics.GLXFBConfig,
            _ => 0,
        };
        return handle != 0 ? handle : base.GetNativeHandle(type);
    }

    private unsafe void CaptureGraphicsHandles(nint window)
    {
        if (!OperatingSystem.IsLinux() || Driver is not ("opengl" or "opengles2") ||
            SDL.GetCurrentVideoDriver() is not ("x11" or "wayland") || SDL.GLGetCurrentWindow() != window)
            return;
        var context = SDL.GLGetCurrentContext();
        if (context == 0) return;
        // SDL's GL renderers create and make their owned context current before returning.
        // Capture once, while its window association is known; later queries must not use thread-current state.
        _graphics.Context = context;
        var display = SDL.EGLGetCurrentDisplay();
        var config = SDL.EGLGetCurrentConfig();
        if (display != 0 && config != 0)
        {
            var currentContext = (delegate* unmanaged[Cdecl]<nint>)SDL.EGLGetProcAddress("eglGetCurrentContext");
            var currentDisplay = (delegate* unmanaged[Cdecl]<nint>)SDL.EGLGetProcAddress("eglGetCurrentDisplay");
            var query = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, uint>)SDL.EGLGetProcAddress("eglQueryContext");
            var attribute = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, uint>)SDL.EGLGetProcAddress("eglGetConfigAttrib");
            int contextConfig = 0, configID = 0;
            if (currentContext != null && currentDisplay != null && query != null && attribute != null &&
                currentContext() == context && currentDisplay() == display &&
                query(display, context, 0x3028 /* EGL_CONFIG_ID */, &contextConfig) != 0 &&
                attribute(display, config, 0x3028, &configID) != 0 && contextConfig == configID)
            {
                _graphics.EGLDisplay = display;
                _graphics.EGLConfig = config;
                return;
            }
        }
        if (SDL.GetCurrentVideoDriver() != "x11") return;
        display = SDL.GetPointerProperty(SDL.GetWindowProperties(window), SDL.Props.WindowX11DisplayPointer, 0);
        var glContext = (delegate* unmanaged[Cdecl]<nint>)SDL.GLGetProcAddress("glXGetCurrentContext");
        var glDisplay = (delegate* unmanaged[Cdecl]<nint>)SDL.GLGetProcAddress("glXGetCurrentDisplay");
        var glQuery = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, int>)SDL.GLGetProcAddress("glXQueryContext");
        var configs = (delegate* unmanaged[Cdecl]<nint, int, int*, nint*>)SDL.GLGetProcAddress("glXGetFBConfigs");
        var glAttribute = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, int>)SDL.GLGetProcAddress("glXGetFBConfigAttrib");
        int selectedConfig = 0, screen = -1;
        if (display == 0 || glContext == null || glDisplay == null || glQuery == null || configs == null || glAttribute == null ||
            glContext() != context || glDisplay() != display ||
            glQuery(display, context, 0x8013 /* GLX_FBCONFIG_ID */, &selectedConfig) != 0 || selectedConfig == 0 ||
            glQuery(display, context, 0x800C /* GLX_SCREEN */, &screen) != 0 || screen < 0 ||
            !NativeLibrary.TryLoad("libX11.so.6", out var x11)) return;
        try
        {
            if (!NativeLibrary.TryGetExport(x11, "XFree", out var free)) return;
            var count = 0;
            var values = configs(display, screen, &count);
            if (values == null) return;
            try
            {
                for (var i = 0; i < count; i++)
                {
                    int id = 0, visual = 0;
                    if (glAttribute(display, values[i], 0x8013, &id) != 0 || id != selectedConfig) continue;
                    _graphics.GLXFBConfig = values[i];
                    if (glAttribute(display, values[i], 0x800B /* GLX_VISUAL_ID */, &visual) == 0)
                        _graphics.GLXVisualID = unchecked((nint)(uint)visual);
                    break;
                }
            }
            finally { ((delegate* unmanaged[Cdecl]<nint, int>)free)((nint)values); }
        }
        finally { NativeLibrary.Free(x11); }
    }

    internal override Vector2i GetPixelSize()
    {
        Check(SDL.GetRenderOutputSize(_renderer.DangerousGetHandle(), out var width, out var height), "query output size");
        return new(width, height);
    }

    internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)
    {
        foreach (var batch in batches)
            if (batch.ShaderCode is not null)
                throw new NotSupportedException("The compatibility renderer does not execute shader materials.");
        if (Driver == "software")
            foreach (var batch in batches)
                if (batch.Blend != BlendMode.Mix)
                    throw new NotSupportedException($"The software compatibility renderer cannot execute {batch.Blend} canvas blending.");
        var renderer = _renderer.DangerousGetHandle();
        var size = GetPixelSize();
        if (size.X <= 0 || size.Y <= 0) return;
        _usedTextures.Clear();
        foreach (var batch in batches)
            if (batch.Texture is { } texture)
            {
                if (Driver == "software" && batch.Filter == TextureFilter.Linear)
                    throw new NotSupportedException("The software compatibility driver cannot linearly filter canvas triangles; select Nearest or a hardware renderer.");
                if (batch.Filter >= TextureFilter.NearestWithMipmaps)
                    throw new NotSupportedException("The compatibility renderer cannot sample texture mipmaps or use anisotropic filtering.");
                if (batch.Repeat == TextureRepeat.Mirror)
                    throw new NotSupportedException("The compatibility renderer cannot use mirrored texture repeat.");
                if (_usedTextures.Add(texture)) PrepareTexture(texture);
                var image = _textures[texture].Pixels.Source;
                if (batch.Repeat == TextureRepeat.Enabled && ((image.Width & (image.Width - 1)) != 0 || (image.Height & (image.Height - 1)) != 0) &&
                    !SDL.GetBooleanProperty(SDL.GetRendererProperties(renderer), SDL.Props.RendererTextureWrappingBoolean, false))
                    throw new NotSupportedException("This compatibility driver cannot repeat textures whose dimensions are not powers of two.");
            }
        foreach (var pair in _textures)
            if (!_usedTextures.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed)) { pair.Value.Handle.Dispose(); _textures.Remove(pair.Key); }
        if (_targetSize != size)
        {
            var target = new RenderHandle(SDL.CreateTexture(renderer, (BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888), SDL.TextureAccess.Target, size.X, size.Y), SDL.DestroyTexture, _renderer);
            try { Check(SDL.SetTextureBlendMode(target.DangerousGetHandle(), SDL.BlendMode.None), "set framebuffer copy blending"); }
            catch { target.Dispose(); throw; }
            _target?.Dispose();
            _target = target;
            _targetSize = size;
            _hasFrame = false;
        }
        Check(SDL.SetRenderTarget(renderer, _target!.DangerousGetHandle()), "bind canvas framebuffer");
        try
        {
            Check(SDL.SetRenderClipRect(renderer, 0), "reset canvas clipping");
            Check(SDL.SetRenderDrawColorFloat(renderer, clear.R, clear.G, clear.B, clear.A), "set clear color");
            Check(SDL.RenderClear(renderer), "clear canvas framebuffer");
            if (_vertices.Length < vertices.Length) Array.Resize(ref _vertices, Math.Max(vertices.Length, _vertices.Length * 2));
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                _vertices[i] = new SDL.Vertex
                {
                    Position = new SDL.FPoint { X = v.Position.X, Y = v.Position.Y },
                    Color = new SDL.FColor { R = v.Color.R, G = v.Color.G, B = v.Color.B, A = v.Color.A },
                    TexCoord = new SDL.FPoint { X = v.UV.X, Y = v.UV.Y }
                };
            }
            Rect2i? activeClip = null;
            foreach (var batch in batches)
            {
                if (batch.Clip != activeClip)
                {
                    if (batch.Clip is { } clip)
                    {
                        var rect = new SDL.Rect { X = clip.Position.X, Y = clip.Position.Y, W = clip.Size.X, H = clip.Size.Y };
                        Check(SDL.SetRenderClipRect(renderer, in rect), "set canvas clipping");
                    }
                    else Check(SDL.SetRenderClipRect(renderer, 0), "clear canvas clipping");
                    activeClip = batch.Clip;
                }
                var mode = batch.Repeat == TextureRepeat.Enabled ? SDL.TextureAddressMode.Wrap : SDL.TextureAddressMode.Clamp;
                Check(SDL.SetRenderTextureAddressMode(renderer, mode, mode), "set texture addressing");
                var texture = batch.Texture is null ? 0 : _textures[batch.Texture].Handle.DangerousGetHandle();
                var blend = CanvasBlend(batch.Blend);
                if (texture == 0) Check(SDL.SetRenderDrawBlendMode(renderer, blend), "set canvas geometry blending");
                else Check(SDL.SetTextureBlendMode(texture, blend), "set canvas texture blending");
                if (texture != 0) Check(SDL.SetTextureScaleMode(texture,
                    batch.Filter == TextureFilter.Linear ? SDL.ScaleMode.Linear : SDL.ScaleMode.Nearest), "set canvas texture filtering");
                // SDL 3.4.16's software quad shortcut loses transposed/constant UVs. Separate triangles bypass it.
                var step = Driver == "software" && texture != 0 ? 3 : batch.Count;
                for (var first = batch.First; first < batch.First + batch.Count; first += step)
                    Check(SDL.RenderGeometry(renderer, texture, _vertices.AsSpan(first, step), step, 0, 0), "draw canvas geometry");
            }
            _hasFrame = true;
        }
        finally
        {
            try { Check(SDL.SetRenderClipRect(renderer, 0), "clear canvas clipping"); }
            finally { Check(SDL.SetRenderTarget(renderer, 0), "restore window render target"); }
        }
        if (!present) return;
        Check(SDL.RenderTexture(renderer, _target.DangerousGetHandle(), 0, 0), "copy canvas to window");
        Check(SDL.RenderPresent(renderer), "present canvas");
    }

    private static SDL.BlendMode CanvasBlend(BlendMode blend)
    {
        if (blend == BlendMode.Mix) return SDL.BlendMode.Blend;
        if (blend == BlendMode.PremultAlpha) return SDL.BlendMode.BlendPremultiplied;
        var srcColor = blend == BlendMode.Mul ? SDL.BlendFactor.DstColor :
            blend == BlendMode.PremultAlpha ? SDL.BlendFactor.One : SDL.BlendFactor.SrcAlpha;
        var dstColor = blend == BlendMode.Mul ? SDL.BlendFactor.Zero :
            blend == BlendMode.PremultAlpha ? SDL.BlendFactor.OneMinusSrcAlpha : SDL.BlendFactor.One;
        var srcAlpha = blend == BlendMode.Mul ? SDL.BlendFactor.DstAlpha :
            blend == BlendMode.PremultAlpha ? SDL.BlendFactor.One : SDL.BlendFactor.SrcAlpha;
        var dstAlpha = blend == BlendMode.Mul ? SDL.BlendFactor.Zero :
            blend == BlendMode.PremultAlpha ? SDL.BlendFactor.OneMinusSrcAlpha : SDL.BlendFactor.One;
        var operation = blend == BlendMode.Sub ? SDL.BlendOperation.RevSubtract : SDL.BlendOperation.Add;
        return SDL.ComposeCustomBlendMode(srcColor, dstColor, operation, srcAlpha, dstAlpha, operation);
    }

    private unsafe void PrepareTexture(Texture texture)
    {
        var pixels = texture.CapturePixels() ?? throw new InvalidOperationException("The drawn texture has no readable image.");
        _textures.TryGetValue(texture, out var previous);
        if (ReferenceEquals(previous.Pixels, pixels)) return;
        var replacement = previous.Handle is null || !ReferenceEquals(previous.Pixels.Allocation, pixels.Allocation);
        var handle = previous.Handle;
        if (replacement)
        {
            var format = pixels.Upload.Format == Image.Format.Rgba8
                ? (BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888) : SDL.PixelFormat.RGBA128Float;
            if (format == SDL.PixelFormat.RGBA128Float)
            {
                var formats = (SDL.PixelFormat*)SDL.GetPointerProperty(SDL.GetRendererProperties(_renderer.DangerousGetHandle()), SDL.Props.RendererTextureFormatsPointer, 0);
                if (formats is null) throw new NotSupportedException("The compatibility driver does not report high-precision texture support.");
                while (*formats != 0 && *formats != format) formats++;
                if (*formats == 0) throw new NotSupportedException("This compatibility driver cannot retain HDR texture precision; use GPU rendering.");
            }
            handle = new RenderHandle(SDL.CreateTexture(_renderer.DangerousGetHandle(), format, SDL.TextureAccess.Static,
                pixels.Upload.Width, pixels.Upload.Height), SDL.DestroyTexture, _renderer);
        }
        try
        {
            Check(SDL.SetTextureBlendMode(handle!.DangerousGetHandle(), SDL.BlendMode.Blend), "set texture blending");
            fixed (byte* data = pixels.Upload.Data)
                Check(SDL.UpdateTexture(handle.DangerousGetHandle(), 0, (nint)data, checked(pixels.Upload.Width * pixels.BytesPerPixel)), "upload texture pixels");
        }
        catch { if (replacement) handle!.Dispose(); throw; }
        if (replacement) previous.Handle?.Dispose();
        _textures[texture] = (handle, pixels);
    }

    internal override Image Readback()
    {
        if (!_hasFrame) throw new InvalidOperationException("No canvas frame has completed.");
        var renderer = _renderer.DangerousGetHandle();
        Check(SDL.SetRenderTarget(renderer, _target!.DangerousGetHandle()), "bind readback target");
        try
        {
            using var surface = new RenderHandle(SDL.RenderReadPixels(renderer, null), SDL.DestroySurface);
            using var rgba = new RenderHandle(SDL.ConvertSurface(surface.DangerousGetHandle(), (BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888)), SDL.DestroySurface);
            var data = Marshal.PtrToStructure<SDL.Surface>(rgba.DangerousGetHandle());
            var pixels = new byte[checked(data.Width * data.Height * 4)];
            for (var y = 0; y < data.Height; y++) Marshal.Copy(data.Pixels + y * data.Pitch, pixels, y * data.Width * 4, data.Width * 4);
            return Image.CreateFromData(data.Width, data.Height, false, Image.Format.Rgba8, pixels);
        }
        finally { Check(SDL.SetRenderTarget(renderer, 0), "restore readback target"); }
    }

    public override void Dispose()
    {
        _graphics = default;
        foreach (var texture in _textures.Values) texture.Handle.Dispose();
        _textures.Clear(); _usedTextures.Clear(); _target?.Dispose(); _renderer.Dispose();
    }
}
