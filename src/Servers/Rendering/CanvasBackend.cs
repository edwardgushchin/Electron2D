using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

internal readonly record struct CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null,
    TextureFilter Filter = TextureFilter.Nearest,
    TextureRepeat Repeat = TextureRepeat.Disabled, int MaxAnisotropy = 1,
    BlendMode Blend = BlendMode.Mix, Rect2i? Clip = null)
{
    internal byte[]? ShaderCode => Material?.Program.Code;
}

internal abstract class CanvasBackend : IDisposable
{
    internal abstract string Method { get; }
    internal abstract string Driver { get; }
    internal abstract Vector2i GetPixelSize();
    private readonly Dictionary<Viewport, CanvasRenderTarget> _targets = new(ReferenceEqualityComparer.Instance);
    internal abstract RenderHandle CreateTarget(Vector2i size, Color clear);
    internal CanvasRenderTarget Target(Viewport viewport, Vector2i size, Color clear)
    {
        if (_targets.TryGetValue(viewport, out var target) && target.Size == size) return target;
        if (size.X is < 1 or > 16384 || size.Y is < 1 or > 16384) throw new NotSupportedException("Canvas targets require 1..16384 pixels per axis on this backend profile.");
        var current = CreateTarget(size, clear); RenderHandle next;
        try { next = CreateTarget(size, clear); } catch { current.Dispose(); throw; }
        var replacement = new CanvasRenderTarget(current, next, size); target?.Dispose(); _targets[viewport] = replacement; return replacement;
    }
    internal CanvasRenderTarget? FindTarget(Viewport viewport) => _targets.GetValueOrDefault(viewport);
    internal void ReleaseTarget(Viewport viewport) { if (_targets.Remove(viewport, out var target)) target.Dispose(); }
    protected void ReleaseTargets() { foreach (var target in _targets.Values) target.Dispose(); _targets.Clear(); }
    internal virtual void SetWindowVisible(DisplayServer display, bool visible) => display.SetWindowVisible(visible);
    internal virtual void BeginFrame() { }
    internal virtual void EndFrame() { }
    internal abstract void Draw(CanvasRenderTarget target, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, bool present, double time);
    internal abstract Image Readback(CanvasRenderTarget target);
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

    protected override bool ReleaseHandle()
    {
        try { _release(handle); }
        finally { _owner?.DangerousRelease(); }
        return true;
    }
}
