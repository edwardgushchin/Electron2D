namespace Electron2D;

public sealed partial class RenderingServer
{
    private long _changeRevision = 1;
    private long _submittedRevision;
    private bool _needsPresentation = true;
    private bool _continuousCanvas;

    internal static void NotifyRenderMutation() => Service?.MarkChanged();
    internal void MarkChanged() => Interlocked.Increment(ref _changeRevision);
    internal void RequireContinuousCanvas() => _continuousCanvas = true;

    /// <summary>Reports pending rendering changes or an active time-dependent canvas consumer.</summary>
    /// <returns>True until a successful presented frame consumes the changes, or while time-dependent drawing is active.</returns>
    /// <remarks>Owner-thread observation does not clear pending changes. Node mutations, resource changes and retained
    /// server writes conservatively invalidate output, including writes whose resulting pixels may be equal.
    /// Failed frames and changes made by completion callbacks remain pending. ForceDraw can bypass automatic skipping.</remarks>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or the caller is not its owner.</exception>
    public static bool HasChanged() => RequireService().HasChangedCore();

    private bool HasChangedCore()
    {
        EnsureOwner();
        return _needsPresentation || Volatile.Read(ref _changeRevision) != _submittedRevision ||
            _queuedFrameCallbacks.Count != 0 || _continuousCanvas || HasContinuousViewportUpdates() || HasInterpolationChanges() || HasContactChanges() ||
            AnimatedTexture.HasTimedChanges(this, System.Diagnostics.Stopwatch.GetTimestamp());
    }

    private bool HasContinuousViewportUpdates()
    {
        foreach (var viewport in _canvasFrames.Keys)
            if (!viewport.IsDisposed && ViewportActive(viewport) && UpdateMode(viewport) == ViewportUpdateMode.Always &&
                viewport is not Window { Visible: false } && TargetSize(viewport).X > 1 && TargetSize(viewport).Y > 1) return true;
        return false;
    }

    private bool HasContactChanges()
    {
        if (_window.Tree is not { DebugCollisionsHint: true } tree) return false;
        foreach (var runtime in tree.DebugPhysicsWorlds)
            if (runtime.ExistingSpace is { } space && (_contactCanvases.TryGetValue(runtime, out var canvas) ? canvas.Revision != space.DebugContactRevision : !space.DebugContacts.IsEmpty)) return true;
        return false;
    }

    private bool HasInterpolationChanges()
    {
        if (_window.Tree is not { PhysicsInterpolation: true }) return false;
        foreach (var item in _sceneCanvasItems) if (!item.IsDisposed && item.HasRenderingInterpolation) return true;
        foreach (var item in _ownedCanvasItems) if (!item.IsDisposed && item.HasRenderingInterpolation) return true;
        foreach (var viewport in _sceneViewports) if (!viewport.IsDisposed && viewport.HasRenderingInterpolation) return true;
        foreach (var light in _retainedLights.Values) if (light.Interpolation.Enabled && light.Interpolation.Previous != light.Interpolation.Current) return true;
        foreach (var occluder in _retainedOccluders.Values) if (occluder.Interpolation.Enabled && occluder.Interpolation.Previous != occluder.Interpolation.Current) return true;
        return false;
    }
}
