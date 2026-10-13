namespace Electron2D;

public sealed partial class SceneTree
{
    private bool _physicsInterpolation;
    private bool _inPhysicsFrame;

    /// <summary>Gets or sets whether the scene presents eligible canvas transforms between physics ticks.</summary>
    /// <value>Sampled from <see cref="ProjectSettings.PhysicsInterpolation"/> at construction; false by default.</value>
    /// <remarks>Changing this while active resets presentation snapshots and leaves logical transforms unchanged.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The scene tree is finalized or disposed.</exception>
    /// <exception cref="AggregateException">A retained transform getter or reset callback failed after later resets and descendants were attempted.</exception>
    public bool PhysicsInterpolation
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _physicsInterpolation; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (_physicsInterpolation == value) return;
            _physicsInterpolation = value;
            List<Exception>? errors = null;
            try { RenderingServer.Service?.ResetLightingInterpolation(this); } catch (Exception error) { CollectException(errors ??= [], error); }
            try { RenderingServer.Service?.ResetCanvasItemInterpolation(this); } catch (Exception error) { CollectException(errors ??= [], error); }
            try { Root.PropagateNotification(Node.NotificationResetPhysicsInterpolation); } catch (Exception error) { CollectException(errors ??= [], error); }
            if (errors is not null) throw new AggregateException("Physics interpolation reset failed.", errors);
        }
    }

    internal bool IsInPhysicsFrame => _inPhysicsFrame;
    internal bool IsPhysicsInterpolationActive => _physicsInterpolation;

    private void CapturePhysicsInterpolation(bool start, ref List<Exception>? errors)
    {
        RenderingServer.Service?.LightingPhysicsTick(this, start);
        try { RenderingServer.Service?.CanvasItemPhysicsTick(this, start); } catch (Exception error) { CollectException(errors ??= [], error); }
        try { RenderingMultiMeshRegistry.PhysicsTick(start); } catch (Exception error) { CollectException(errors ??= [], error); }
        _scheduleTraversal.Clear();
        _scheduleTraversal.Add(Root);
        while (_scheduleTraversal.Count != 0)
        {
            var last = _scheduleTraversal.Count - 1;
            var node = _scheduleTraversal[last];
            _scheduleTraversal.RemoveAt(last);
            if (ReferenceEquals(node.Tree, this) && !node.IsDisposed)
            {
                try
                {
                    if (node is CanvasItem canvas)
                    {
                        if (start) canvas.BeginPhysicsInterpolationTick();
                        else canvas.EndPhysicsInterpolationTick();
                    }
                    else if (node is Viewport viewport)
                    {
                        if (start) viewport.BeginPhysicsInterpolationTick();
                        else viewport.EndPhysicsInterpolationTick();
                    }
                }
                catch (Exception error) { CollectException(errors ??= [], error); }
                for (var index = node.GetChildCount(includeInternal: true) - 1; index >= 0; index--)
                    _scheduleTraversal.Add(node.GetChild(index, includeInternal: true));
            }
        }
    }
}
