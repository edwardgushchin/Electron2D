namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects completed aggregate canvas submission statistics.</summary>
    public enum RenderingInfo
    {
        /// <summary>Logical canvas drawing records in all viewports submitted by the last completed renderer frame.</summary>
        TotalObjectsInFrame = 0,
        /// <summary>Submitted canvas primitive/instance budgets in that frame.</summary>
        TotalPrimitivesInFrame = 1,
        /// <summary>Canvas geometry draw calls issued by the native backend in that frame.</summary>
        TotalDrawCallsInFrame = 2
    }
    private long _renderObjects, _renderPrimitives, _renderDrawCalls;

    /// <summary>Reads a completed aggregate canvas count without creating or synchronizing a renderer.</summary>
    /// <param name="info">An implemented aggregate selector.</param><returns>Zero without an active session; otherwise its last completed frame count.</returns>
    /// <remarks>Safe on any thread and allocation-free. Only successfully submitted viewport frames contribute;
    /// skipped/failed frames retain the prior aggregate. Publication precedes completion callbacks. Counts follow
    /// Viewport.GetRenderInfo, including indirect capacity budgets and exclusion of internal/replayed native passes.
    /// Several independent reads are not an atomic snapshot. Device memory and pipeline accounting have separate producers.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is not implemented.</exception>
    public static long GetRenderingInfo(RenderingInfo info)
    {
        if ((uint)info > 2) throw new ArgumentOutOfRangeException(nameof(info));
        var service = Service;
        return info switch
        {
            RenderingInfo.TotalObjectsInFrame => service is null ? 0 : Interlocked.Read(ref service._renderObjects),
            RenderingInfo.TotalPrimitivesInFrame => service is null ? 0 : Interlocked.Read(ref service._renderPrimitives),
            _ => service is null ? 0 : Interlocked.Read(ref service._renderDrawCalls)
        };
    }
    /// <summary>Reads the last completed canvas count of a live viewport RID.</summary>
    /// <param name="viewport">A live viewport identity in this renderer.</param><param name="type">Canvas rendering domain.</param>
    /// <param name="info">A supported canvas counter.</param><returns>The viewport's retained completed value.</returns>
    /// <remarks>Uses the same counters and completion boundary as Viewport.GetRenderInfo; requires the native owner thread.</remarks>
    /// <exception cref="ArgumentException">The identity or a selector is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer exists or this is not its owner thread.</exception>
    public static long ViewportGetRenderInfo(RID viewport, ViewportRenderInfoType type, ViewportRenderInfo info)
    { var service = RequireService(); return service.ServerViewport(viewport).GetRenderInfo(type, info); }

    private void PublishRenderInfo()
    {
        long objects = 0, primitives = 0, calls = 0;
        foreach (var frame in _activeFrames) if (frame.Drawn)
                checked { objects += frame.RenderObjects; primitives += frame.RenderPrimitives; calls += frame.RenderDrawCalls; }
        foreach (var frame in _activeFrames) if (frame.Drawn) frame.Viewport.PublishRenderInfo(frame.RenderObjects, frame.RenderPrimitives, frame.RenderDrawCalls);
        Interlocked.Exchange(ref _renderObjects, objects); Interlocked.Exchange(ref _renderPrimitives, primitives); Interlocked.Exchange(ref _renderDrawCalls, calls);
    }
}
