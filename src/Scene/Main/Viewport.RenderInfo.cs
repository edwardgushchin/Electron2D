namespace Electron2D;

/// <summary>Selects a completed viewport canvas submission count.</summary>
public enum ViewportRenderInfo
{
    /// <summary>Submitted logical drawing records before compatible batch merging; this is not a Node count.</summary>
    ObjectsInFrame = 0,
    /// <summary>Submitted point, line or triangle primitives, multiplied by the requested instance count.</summary>
    PrimitivesInFrame = 1,
    /// <summary>Canvas geometry draw calls issued by the selected native backend.</summary>
    DrawCallsInFrame = 2,
    /// <summary>Exclusive selector extent; not a selectable counter.</summary>
    Max = 3
}

/// <summary>Selects the supported viewport rendering domain.</summary>
public enum ViewportRenderInfoType
{
    /// <summary>Two-dimensional canvas drawing, excluding internal lighting, copy and presentation passes.</summary>
    Canvas = 2,
    /// <summary>Exclusive selector extent; not a selectable rendering domain.</summary>
    Max = 3
}

public abstract partial class Viewport
{
    private long _renderObjects, _renderPrimitives, _renderDrawCalls;

    /// <summary>Reads one count from this viewport's last successfully completed canvas frame.</summary>
    /// <param name="type">Canvas rendering domain.</param><param name="info">Object, primitive or native draw-call count.</param>
    /// <returns>Zero before the first completed frame; otherwise the retained completed count.</returns>
    /// <remarks>Skipped or failed rendering retains prior values. Empty completed frames publish zeros. Counts describe
    /// submitted geometry, not fragments surviving clipping, depth or a shader. GPU-authored indirect visibility/LOD and
    /// particle activity use their requested capacity/base-geometry budget without device readback. Internal backend
    /// lighting/copy/resolve/presentation work and replay on secondary native surfaces are excluded. Detached viewports
    /// retain their last completed values. Separate reads are not an atomic snapshot. Attached reads require the scene owner.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A selector is not supported.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is queried from a foreign thread.</exception>
    public long GetRenderInfo(ViewportRenderInfoType type, ViewportRenderInfo info)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (type != ViewportRenderInfoType.Canvas) throw new ArgumentOutOfRangeException(nameof(type));
        return info switch
        {
            ViewportRenderInfo.ObjectsInFrame => Interlocked.Read(ref _renderObjects),
            ViewportRenderInfo.PrimitivesInFrame => Interlocked.Read(ref _renderPrimitives),
            ViewportRenderInfo.DrawCallsInFrame => Interlocked.Read(ref _renderDrawCalls),
            _ => throw new ArgumentOutOfRangeException(nameof(info))
        };
    }
    internal void PublishRenderInfo(long objects, long primitives, long draws)
    {
        Interlocked.Exchange(ref _renderObjects, objects); Interlocked.Exchange(ref _renderPrimitives, primitives); Interlocked.Exchange(ref _renderDrawCalls, draws);
    }
}
