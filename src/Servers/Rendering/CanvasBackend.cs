using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

internal enum CanvasOperation { Draw, Copy, GroupBegin, GroupEnd, MaskBegin, MaskEnd, MaskFinish }

internal readonly record struct CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null,
    TextureFilter Filter = TextureFilter.Nearest,
    TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1,
    BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null, CanvasOperation Operation = CanvasOperation.Draw, Rect2i Region = default, bool MIPMaps = false, bool GroupShader = false, bool MaskShader = false, int FirstInstance = 0, int InstanceCount = 0, CanvasLightingItem? Lighting = null, CanvasItem? InstanceOwner = null, CanvasGlyph Glyph = default, Color GlyphModulation = default, bool LinearColors = false, Color? CanvasModulation = null, Rect2? VisibilityBounds = null, bool Procedural = false, Mesh.PrimitiveType Primitive = Mesh.PrimitiveType.Triangles, bool SnapGeneratedVertices = false, MultiMeshGPUDraw GPU = default, ParticleGPUDraw? Particles = null, Texture? TextureSource = null)
{
    internal byte[]? ShaderCode => Material?.Program.NativeCode;
}

internal readonly record struct CanvasPresentation(CanvasRenderTarget Target, Rect2 Rect);

internal abstract class CanvasBackend : IDisposable
{
    private CanvasRenderTarget? _presentation;
    protected CanvasRenderTarget PreparePresentation() => PreparePresentation(GetPixelSize(), ref _presentation);
    protected CanvasRenderTarget PreparePresentation(Vector2i size, ref CanvasRenderTarget? storage)
    {
        if (size.X is < 1 or > 16384 || size.Y is < 1 or > 16384) throw new NotSupportedException("Presentation targets require 1..16384 pixels per axis on this backend profile.");
        if (storage?.Size == size) return storage;
        var current = CreateTarget(size, Colors.Black); RenderHandle next;
        try { next = CreateTarget(size, Colors.Black); } catch { current.Dispose(); throw; }
        var replacement = new CanvasRenderTarget(current, next, size);
        storage?.Dispose(); return storage = replacement;
    }
    internal Image ReadbackScreen(CanvasRenderTarget? content) => Readback(_presentation is { HasFrame: true } ? _presentation : content ?? throw new InvalidOperationException("No canvas frame has completed."));
    internal abstract void Present(ReadOnlySpan<CanvasPresentation> viewports);
    // Clip before native submission, preserving the source mapping for partially off-window rectangles.
    protected static bool PresentationRegion(Rect2 destination, Vector2i client, out Rect2 clipped, out Rect2 uv)
    {
        clipped = destination.Intersection(new Rect2(Vector2.Zero, client)); uv = default;
        if (!destination.HasArea() || !clipped.HasArea()) return false;
        uv = new((clipped.Position - destination.Position) / destination.Size, clipped.Size / destination.Size);
        return true;
    }
    internal abstract DisplayServer.VSyncMode VSync { get; }
    internal abstract void SetVSync(DisplayServer.VSyncMode mode);
    internal abstract string Method { get; }
    internal abstract string Driver { get; }
    internal abstract Vector2i GetPixelSize();
    private readonly Dictionary<Viewport, CanvasRenderTarget> _targets = new(ReferenceEqualityComparer.Instance);
    internal abstract RenderHandle CreateTarget(Vector2i size, Color clear, bool mipmaps = false, bool HDR = false);
    internal virtual void ValidateHDR(bool enabled) { if (enabled) throw new NotSupportedException("The compatibility renderer cannot use linear HDR canvas targets."); }
    internal virtual void ValidateMSAA(Viewport.MSAA mode) { if (mode != Viewport.MSAA.Disabled) throw new NotSupportedException("The compatibility renderer cannot use canvas multisample antialiasing."); }
    internal virtual RenderHandle CreateMultisampleTarget(Vector2i size, Color clear, Viewport.MSAA mode, bool HDR = false) => throw new NotSupportedException("Multisample canvas targets require GPU rendering.");
    internal CanvasRenderTarget Target(Viewport viewport, Vector2i size, Color clear)
    {
        var mode = viewport.RenderingOwner?.ViewportMSAA(viewport) ?? viewport.MSAA2D; ValidateMSAA(mode);
        var hdr = viewport.RenderingOwner?.ViewportHDR(viewport) ?? viewport.UseHDR2D; ValidateHDR(hdr);
        if (_targets.TryGetValue(viewport, out var target) && target.Size == size && target.MSAA == mode && target.HDR == hdr) return target;
        if (size.X is < 1 or > 16384 || size.Y is < 1 or > 16384) throw new NotSupportedException("Canvas targets require 1..16384 pixels per axis on this backend profile.");
        var current = CreateTarget(size, clear, HDR: hdr); RenderHandle next;
        try { next = CreateTarget(size, clear, HDR: hdr); } catch { current.Dispose(); throw; }
        var replacement = new CanvasRenderTarget(current, next, size) { MSAA = mode, HDR = hdr };
        try { if (mode != Viewport.MSAA.Disabled) replacement.Multisample = CreateMultisampleTarget(size, clear, mode, hdr); } catch { replacement.Dispose(); throw; }
        target?.Dispose(); _targets[viewport] = replacement; return replacement;
    }
    internal RenderHandle BackBuffer(CanvasRenderTarget target, bool mipmaps)
    {
        if (target.BackBuffer is not null && (!mipmaps || target.BackBufferMipmaps)) return target.BackBuffer;
        var replacement = CreateTarget(target.Size, default, mipmaps, target.HDR); target.BackBuffer?.Dispose();
        target.BackBuffer = replacement; target.BackBufferMipmaps = mipmaps; return replacement;
    }
    internal CanvasRenderTarget? FindTarget(Viewport viewport) => _targets.GetValueOrDefault(viewport);
    internal void ReleaseTarget(Viewport viewport) { if (_targets.Remove(viewport, out var target)) target.Dispose(); }
    protected void ReleaseTargets() { foreach (var target in _targets.Values) target.Dispose(); _targets.Clear(); _presentation?.Dispose(); _presentation = null; }
    internal virtual void SetWindowVisible(DisplayServer display, bool visible) => display.SetWindowVisible(visible);
    internal virtual void BeginFrame() { }
    internal virtual void EndFrame() { }
    internal abstract void Sync(CanvasRenderTarget? target);
    internal abstract void ReleaseTemporaryTexture(Texture texture);
    internal abstract void Draw(CanvasRenderTarget target, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting, bool processParticles = true);
    internal abstract Image Readback(CanvasRenderTarget target, bool backBuffer = false, bool requireFrame = true);
    internal void SeedHistory(CanvasBackend source)
    {
        ReleaseTargets();
        foreach (var pair in source._targets)
        {
            var sourceTarget = pair.Value;
            var target = Target(pair.Key, sourceTarget.Size, default);
            using (var pixels = source.Readback(sourceTarget, requireFrame: false)) ImportPixels(target.Current, pixels);
            if (sourceTarget.BackBuffer is not null)
            {
                using var pixels = source.Readback(sourceTarget, backBuffer: true, requireFrame: false);
                ImportPixels(BackBuffer(target, sourceTarget.BackBufferMipmaps), pixels);
            }
            target.HasFrame = sourceTarget.HasFrame;
        }
    }
    protected virtual void ImportPixels(RenderHandle target, Image pixels) => throw new NotSupportedException("This backend cannot import canvas history.");
    internal virtual nint GetNativeHandle(DisplayServer.HandleType type) =>
        throw new NotSupportedException($"The {Driver} renderer has no {type} identity.");
    public abstract void Dispose();

    internal static InvalidOperationException Failure(string operation) => new($"Rendering failed to {operation}: {SDL.GetError()}");
    internal static void Check(bool result, string operation) { if (!result) throw Failure(operation); }
}

internal sealed class RenderHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly Action<nint> _release;
    private readonly SafeHandle? _owner;

    internal RenderHandle(nint value, Action<nint> release, SafeHandle? owner = null) : base(true)
    {
        if (value == 0) throw CanvasBackend.Failure("create a native graphics resource");
        _release = release;
        _owner = owner;
        var retained = false;
        try { owner?.DangerousAddRef(ref retained); SetHandle(value); }
        catch { release(value); throw; }
    }

    /// <summary>Adopts an exclusively owned private capacity buffer while retaining its wrapper and device reference.</summary>
    /// <remarks>Consumes the incoming nonzero resource on every path. Old release failure leaves the replacement owned;
    /// closed wrappers clean the incoming resource and reject. Call only on the resource owner, without borrowed aliases.</remarks>
    internal void ReplaceNative(nint value)
    {
        if (value == 0) throw CanvasBackend.Failure("create a native graphics resource");
        try
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            var previous = handle;
            SetHandle(value); value = 0;
            _release(previous);
        }
        finally { if (value != 0) _release(value); }
    }

    protected override bool ReleaseHandle()
    {
        try { _release(handle); }
        finally { _owner?.DangerousRelease(); }
        return true;
    }
}
