using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SDL3;

namespace Electron2D;

internal readonly record struct CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null,
    CanvasItem.TextureFilterEnum Filter = CanvasItem.TextureFilterEnum.Nearest,
    CanvasItem.TextureRepeatEnum Repeat = CanvasItem.TextureRepeatEnum.Disabled, int MaxAnisotropy = 1)
{
    internal byte[]? ShaderCode => Material?.Program.Code;
}

internal abstract class CanvasBackend : IDisposable
{
    internal abstract string Method { get; }
    internal abstract string Driver { get; }
    internal abstract Vector2I GetPixelSize();
    internal abstract void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present);
    internal abstract Image Readback();
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
