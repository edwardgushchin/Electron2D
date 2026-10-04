using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed class CompatibilityCanvasBackend : CanvasBackend
{
    private readonly RenderHandle _renderer;
    private SDL.Vertex[] _vertices = [];
    private readonly Dictionary<Texture, (RenderHandle Handle, TexturePixels Pixels)> _textures = [];
    private readonly HashSet<Texture> _usedTextures = [];
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

    internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, bool present, double time)
    {
        foreach (var batch in batches)
            if (batch.ShaderCode is not null)
                throw new NotSupportedException("The compatibility renderer does not execute shader materials.");
        if (Driver == "software")
            foreach (var batch in batches)
                if (batch.Blend != BlendMode.Mix || batch.GroupShader || batch.MaskShader)
                    throw new NotSupportedException($"The software compatibility renderer cannot execute {(batch.GroupShader || batch.MaskShader ? BlendMode.PremultAlpha : batch.Blend)} canvas blending.");
        var renderer = _renderer.DangerousGetHandle();
        var size = output.Size; var screen = false;
        foreach (var batch in batches)
        {
            if (batch.Mipmaps) throw new NotSupportedException("The compatibility renderer cannot generate canvas backbuffer mipmaps.");
            screen |= batch.Operation != CanvasOperation.Draw;
        }
        if (screen) _ = BackBuffer(output, false);
        foreach (var batch in batches)
            if (batch.MaskShader) { output.MaskBuffer ??= CreateTarget(size, default); break; }
        foreach (var batch in batches)
            if (!batch.GroupShader && batch.Texture is { } texture)
            {
                if (Driver == "software" && batch.Filter == TextureFilter.Linear)
                    throw new NotSupportedException("The software compatibility driver cannot linearly filter canvas triangles; select Nearest or a hardware renderer.");
                if (batch.Filter >= TextureFilter.NearestWithMipmaps)
                    throw new NotSupportedException("The compatibility renderer cannot sample texture mipmaps or use anisotropic filtering.");
                if (batch.Repeat == TextureRepeat.Mirror)
                    throw new NotSupportedException("The compatibility renderer cannot use mirrored texture repeat.");
                if (texture is not ViewportTexture && _usedTextures.Add(texture)) PrepareTexture(texture);
                var dimensions = texture.GetSize();
                if (batch.Repeat == TextureRepeat.Enabled && (((int)dimensions.X & ((int)dimensions.X - 1)) != 0 || ((int)dimensions.Y & ((int)dimensions.Y - 1)) != 0) &&
                    !SDL.GetBooleanProperty(SDL.GetRendererProperties(renderer), SDL.Props.RendererTextureWrappingBoolean, false))
                    throw new NotSupportedException("This compatibility driver cannot repeat textures whose dimensions are not powers of two.");
            }
        Check(SDL.SetRenderTarget(renderer, output.Next.DangerousGetHandle()), "bind canvas framebuffer");
        try
        {
            Check(SDL.SetRenderClipRect(renderer, 0), "reset canvas clipping");
            if (clearEnabled) { Check(SDL.SetRenderDrawColorFloat(renderer, clear.R, clear.G, clear.B, clear.A), "set clear color"); Check(SDL.RenderClear(renderer), "clear canvas framebuffer"); }
            else { Check(SDL.SetTextureBlendMode(output.Current.DangerousGetHandle(), SDL.BlendMode.None), "preserve framebuffer blending"); Check(SDL.RenderTexture(renderer, output.Current.DangerousGetHandle(), 0, 0), "preserve completed canvas image"); }
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
            Rect2i? activeClip = null; var inMask = false;
            foreach (var batch in batches)
            {
                if (batch.Operation is CanvasOperation.Copy or CanvasOperation.MaskBegin)
                {
                    Check(SDL.SetRenderTarget(renderer, output.BackBuffer!.DangerousGetHandle()), "bind screen copy target");
                    Check(SDL.SetRenderClipRect(renderer, 0), "clear copy clipping");
                    if (batch.Region.HasArea())
                    {
                        var rect = new SDL.FRect { X = batch.Region.Position.X, Y = batch.Region.Position.Y, W = batch.Region.Size.X, H = batch.Region.Size.Y };
                        Check(SDL.SetTextureBlendMode(output.Next.DangerousGetHandle(), SDL.BlendMode.None), "set screen copy blending");
                        Check(SDL.RenderTexture(renderer, output.Next.DangerousGetHandle(), in rect, in rect), "copy screen region");
                    }
                    var target = batch.Operation == CanvasOperation.MaskBegin ? output.BackBuffer! : output.Next;
                    Check(SDL.SetRenderTarget(renderer, target.DangerousGetHandle()), "restore canvas after copy");
                    Check(SDL.SetRenderClipRect(renderer, 0), "reset canvas copy clipping"); activeClip = null;
                    continue;
                }
                if (batch.Operation == CanvasOperation.MaskEnd && !inMask)
                {
                    inMask = true;
                    Check(SDL.SetRenderTarget(renderer, (batch.MaskShader ? output.MaskBuffer! : output.Next).DangerousGetHandle()), "bind mask result target");
                    Check(SDL.SetRenderClipRect(renderer, 0), "reset mask clipping"); activeClip = null;
                    if (batch.MaskShader) { Check(SDL.SetRenderDrawColorFloat(renderer, 0, 0, 0, 0), "set mask clear"); Check(SDL.RenderClear(renderer), "clear mask alpha target"); }
                }
                if (batch.Operation == CanvasOperation.MaskFinish)
                {
                    if (batch.MaskShader && batch.Region.HasArea())
                    {
                        var rect = new SDL.FRect { X = batch.Region.Position.X, Y = batch.Region.Position.Y, W = batch.Region.Size.X, H = batch.Region.Size.Y };
                        Check(SDL.SetRenderClipRect(renderer, 0), "reset mask composite clipping");
                        var maskBlend = SDL.ComposeCustomBlendMode(SDL.BlendFactor.DstAlpha, SDL.BlendFactor.Zero, SDL.BlendOperation.Add, SDL.BlendFactor.Zero, SDL.BlendFactor.One, SDL.BlendOperation.Add);
                        Check(SDL.SetTextureBlendMode(output.BackBuffer!.DangerousGetHandle(), maskBlend), "set screen mask blending");
                        Check(SDL.SetTextureScaleMode(output.BackBuffer.DangerousGetHandle(), SDL.ScaleMode.Nearest), "set screen mask filtering");
                        Check(SDL.RenderTexture(renderer, output.BackBuffer.DangerousGetHandle(), in rect, in rect), "apply screen RGB to mask alpha");
                        Check(SDL.SetRenderTarget(renderer, output.Next.DangerousGetHandle()), "restore main mask target");
                        Check(SDL.SetRenderClipRect(renderer, 0), "reset final mask clipping");
                        Check(SDL.SetTextureBlendMode(output.MaskBuffer!.DangerousGetHandle(), SDL.BlendMode.BlendPremultiplied), "set mask composite blending");
                        Check(SDL.RenderTexture(renderer, output.MaskBuffer.DangerousGetHandle(), in rect, in rect), "composite masked children");
                    }
                    else Check(SDL.SetRenderTarget(renderer, output.Next.DangerousGetHandle()), "restore main after mask");
                    Check(SDL.SetRenderClipRect(renderer, 0), "reset completed mask clipping"); activeClip = null; inMask = false;
                    continue;
                }
                if (batch.Operation is CanvasOperation.GroupBegin or CanvasOperation.GroupEnd)
                {
                    var target = batch.Operation == CanvasOperation.GroupBegin ? output.BackBuffer! : output.Next;
                    Check(SDL.SetRenderTarget(renderer, target.DangerousGetHandle()), "switch group render target");
                    Check(SDL.SetRenderClipRect(renderer, 0), "reset group clipping"); activeClip = null;
                }
                if (batch.Count == 0 || batch.Clip is { } emptyClip && !emptyClip.HasArea()) continue;
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
                var texture = batch.GroupShader ? output.BackBuffer!.DangerousGetHandle() : batch.Texture is null ? 0 : TextureHandle(batch.Texture);
                var blend = batch.Operation == CanvasOperation.GroupBegin ? SDL.BlendMode.None : CanvasBlend(batch.GroupShader ? BlendMode.PremultAlpha : batch.Blend);
                if (texture == 0) Check(SDL.SetRenderDrawBlendMode(renderer, blend), "set canvas geometry blending");
                else Check(SDL.SetTextureBlendMode(texture, blend), "set canvas texture blending");
                if (texture != 0) Check(SDL.SetTextureScaleMode(texture,
                    batch.Filter == TextureFilter.Linear ? SDL.ScaleMode.Linear : SDL.ScaleMode.Nearest), "set canvas texture filtering");
                // SDL 3.4.16's software quad shortcut loses transposed/constant UVs. Separate triangles bypass it.
                var step = Driver == "software" && texture != 0 ? 3 : batch.Count;
                for (var first = batch.First; first < batch.First + batch.Count; first += step)
                    Check(SDL.RenderGeometry(renderer, texture, _vertices.AsSpan(first, step), step, 0, 0), "draw canvas geometry");
            }
            output.Commit();
        }
        finally
        {
            try { Check(SDL.SetRenderClipRect(renderer, 0), "clear canvas clipping"); }
            finally { Check(SDL.SetRenderTarget(renderer, 0), "restore window render target"); }
        }
        if (!present) return;
        Check(SDL.SetTextureBlendMode(output.Current.DangerousGetHandle(), SDL.BlendMode.None), "set presentation copy blending");
        Check(SDL.RenderTexture(renderer, output.Current.DangerousGetHandle(), 0, 0), "copy canvas to window");
        Check(SDL.RenderPresent(renderer), "present canvas");
    }

    internal override void BeginFrame() => _usedTextures.Clear();
    internal override void EndFrame() { foreach (var pair in _textures) if (!_usedTextures.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed)) { pair.Value.Handle.Dispose(); _textures.Remove(pair.Key); } }
    private nint TextureHandle(Texture texture)
    {
        if (texture is ViewportTexture view) { var viewport = view.Bound ?? throw new InvalidOperationException("The sampled viewport texture is unresolved."); return (FindTarget(viewport) ?? throw new InvalidOperationException("The sampled viewport has no native target.")).Current.DangerousGetHandle(); }
        return _textures[texture].Handle.DangerousGetHandle();
    }
    internal override RenderHandle CreateTarget(Vector2i size, Color clear, bool mipmaps = false)
    {
        if (mipmaps) throw new NotSupportedException("The compatibility renderer cannot generate canvas backbuffer mipmaps.");
        var renderer = _renderer.DangerousGetHandle(); var target = new RenderHandle(SDL.CreateTexture(renderer, BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, size.X, size.Y), SDL.DestroyTexture, _renderer);
        try
        {
            Check(SDL.SetTextureBlendMode(target.DangerousGetHandle(), SDL.BlendMode.None), "set framebuffer copy blending"); Check(SDL.SetRenderTarget(renderer, target.DangerousGetHandle()), "initialize canvas target"); Check(SDL.SetRenderClipRect(renderer, 0), "clear initial target clipping"); Check(SDL.SetRenderDrawColorFloat(renderer, clear.R, clear.G, clear.B, clear.A), "set initial target color"); Check(SDL.RenderClear(renderer), "clear initial canvas target"); return target;
        }
        catch { target.Dispose(); throw; }
        finally { Check(SDL.SetRenderTarget(renderer, 0), "restore initialization target"); }
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

    internal override Image Readback(CanvasRenderTarget output, bool backBuffer = false)
    {
        if (!output.HasFrame) throw new InvalidOperationException("No canvas frame has completed.");
        var renderer = _renderer.DangerousGetHandle();
        Check(SDL.SetRenderTarget(renderer, (backBuffer ? output.BackBuffer ?? throw new InvalidOperationException("No screen backbuffer is available.") : output.Current).DangerousGetHandle()), "bind readback target");
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
        _textures.Clear(); _usedTextures.Clear(); ReleaseTargets(); _renderer.Dispose();
    }
}
