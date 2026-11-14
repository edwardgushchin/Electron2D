namespace Electron2D;

internal abstract partial class CanvasBackend
{
    internal long RenderObjects, RenderPrimitives, RenderDrawCalls;
    private long _canvasPipelineCompilations;
    internal long CanvasPipelineCompilations => Interlocked.Read(ref _canvasPipelineCompilations);
    protected void RecordCanvasPipelineCompilation() => Interlocked.Increment(ref _canvasPipelineCompilations);
    protected void ResetRenderInfo() => RenderObjects = RenderPrimitives = RenderDrawCalls = 0;
    protected void RecordCanvasDraw(CanvasBatch batch, long calls = 1)
    {
        if (batch.Operation != CanvasOperation.Draw || calls == 0) return;
        var instances = batch.Particles is { } particles ? particles.Source.Amount : batch.GPU.State is not null
            ? batch.GPU.Indirect ? batch.GPU.Capacity : batch.GPU.Visible < 0 ? batch.GPU.Capacity : batch.GPU.Visible : Math.Max(1, batch.InstanceCount);
        var elements = batch.GPU.State is not null && batch.GPU.Indices is not null ? batch.GPU.IndexCount : batch.Count;
        var primitives = batch.Primitive switch
        {
            MeshPrimitiveType.Points => elements,
            MeshPrimitiveType.Lines => elements / 2,
            MeshPrimitiveType.LineStrip => Math.Max(0, elements - 1),
            MeshPrimitiveType.TriangleStrip => Math.Max(0, elements - 2),
            _ => elements / 3
        };
        checked { RenderObjects += batch.ObjectCount; RenderPrimitives += (long)primitives * instances; RenderDrawCalls += calls; }
    }
}
