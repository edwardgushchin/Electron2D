namespace Electron2D;

internal sealed class CanvasRenderTarget(RenderHandle current, RenderHandle next, Vector2i size) : IDisposable
{
    internal RenderHandle Current = current, Next = next;
    internal readonly Vector2i Size = size;
    internal bool HasFrame, BackBufferMipmaps;
    internal RenderHandle? BackBuffer, MaskBuffer;
    internal void Commit() { (Current, Next) = (Next, Current); HasFrame = true; }
    public void Dispose() { try { Current.Dispose(); } finally { try { Next.Dispose(); } finally { try { BackBuffer?.Dispose(); } finally { MaskBuffer?.Dispose(); } } } }
}
