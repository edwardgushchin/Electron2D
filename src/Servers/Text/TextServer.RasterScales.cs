namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Retains one viewport or canvas-item reference to a quantized raster scale other than one.</summary>
    /// <param name="oversampling">Finite positive scale in 1/64 increments.</param>
    /// <remarks>The engine delivers existing references when this implementation is selected or first draws a font.
    /// It delivers later changes until removal or disposal, retaining the creator of opaque font sources.
    /// Scale one is the permanent base cache. Native defaults use the shared raster cache registry.</remarks>
    protected virtual void OnReferenceOversamplingLevel(float oversampling) { }
    /// <summary>Releases one raster-scale reference; the last release permits retiring that scale's font caches.</summary>
    /// <param name="oversampling">The same quantized scale received by the retain callback.</param>
    /// <remarks>Callbacks run outside registry, scale and font locks. Recursive scale changes reject before mutation.
    /// Callback failure leaves the reference change committed and does not prevent other retirements.</remarks>
    protected virtual void OnUnreferenceOversamplingLevel(float oversampling) { }
    internal void NotifyRasterScale(float scale, bool reference, bool retiring)
    {
        if (IsDisposed)
        { if (retiring && !reference) OnUnreferenceOversamplingLevel(scale); return; }
        using var consumer = retiring ? default : TextServerManager.BeginConsumerRead(this);
        using var call = EnterImplementation();
        if (reference) OnReferenceOversamplingLevel(scale); else OnUnreferenceOversamplingLevel(scale);
    }
}
