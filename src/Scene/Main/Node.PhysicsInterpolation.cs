namespace Electron2D;

public partial class Node
{
    /// <summary>Identifies a recursive request to reset the displayed physics transform to the current logical transform.</summary>
    public const int NotificationResetPhysicsInterpolation = 2001;

    private PhysicsInterpolationMode _physicsInterpolationMode;

    /// <summary>Gets or sets the inherited physics interpolation policy.</summary>
    /// <value>Inherit for ordinary nodes, Off for Control and On for Viewport, including Window and SubViewport.</value>
    /// <remarks>The effective policy applies only while the scene tree enables interpolation. A changed active policy
    /// republishes affected scene-item renderer policies and resets canvas snapshots before subsequent presentation.
    /// The owner or current processing group may mutate this metadata independently of numerical authoring.
    /// Reset callbacks run immediately, parent first; an inaccessible group branch rejects while later branches continue.
    /// Worker renderer policies and camera history operations publish in order on the owner after the joined stage.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined mode.</exception>
    /// <exception cref="InvalidOperationException">An attached read is outside the owner/active processing groups, or a mutation is outside the owner/current group or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="AggregateException">A retained transform getter or reset callback failed after later policy publications were attempted.</exception>
    public PhysicsInterpolationMode PhysicsInterpolationMode
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _physicsInterpolationMode; }
        set
        {
            EnsureProcessingAccess();
            RenderingServer.NotifyRenderMutation();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_physicsInterpolationMode == value) return;
            _physicsInterpolationMode = value;
            List<Exception>? errors = null;
            PublishInterpolationPolicies(ref errors);
            try
            {
                if (Tree is not null)
                {
                    if (IsPhysicsInterpolated()) PropagateNotificationCore(NotificationResetPhysicsInterpolation, stopAtWindows: false, processingScope: true);
                }
            }
            catch (Exception error) { (errors ??= []).Add(error); }
            if (errors is not null) throw new AggregateException("Physics interpolation policy publication failed.", errors);
        }
    }

    private void PublishInterpolationPolicies(ref List<Exception>? errors)
    {
        Interlocked.Increment(ref _notificationPropagationDepth);
        try
        {
            try { if (this is CanvasItem item) item.PublishInterpolationPolicy(); }
            catch (Exception error) { CollectException(ref errors, error); }
            try
            {
                if (Tree is not null)
                    if (this is Camera camera) camera.RefreshInterpolationProcessing();
                    else if (this is MultiMeshInstance instances) instances.RefreshInterpolation();
            }
            catch (Exception error) { CollectException(ref errors, error); }
            foreach (var child in _children)
                if (!child.IsDisposed && ReferenceEquals(child.Parent, this)) child.PublishInterpolationPolicies(ref errors);
        }
        finally { Interlocked.Decrement(ref _notificationPropagationDepth); }
    }

    /// <summary>Reports the inherited node policy independently of scene-wide enablement.</summary>
    /// <returns>True when this node selects On or inherits an enabled ancestor; a root inherits On.</returns>
    /// <exception cref="InvalidOperationException">An attached query is outside the scene owner or active processing groups.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IsPhysicsInterpolated()
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingReadAccess();
        return _physicsInterpolationMode switch
        {
            PhysicsInterpolationMode.On => true,
            PhysicsInterpolationMode.Off => false,
            _ => Parent?.IsPhysicsInterpolated() ?? true,
        };
    }

    /// <summary>Reports whether physics interpolation currently affects this active node.</summary>
    /// <returns>False while detached or when the scene-wide setting or inherited policy is off.</returns>
    /// <exception cref="InvalidOperationException">An attached query is outside the scene owner or active processing groups.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IsPhysicsInterpolatedAndEnabled()
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingReadAccess();
        return Tree?.IsPhysicsInterpolationActive == true && IsPhysicsInterpolated();
    }

    /// <summary>Resets this node and descendants to their current logical transforms before rendering.</summary>
    /// <remarks>While interpolation is enabled, delivers <see cref="NotificationResetPhysicsInterpolation"/> in
    /// parent-first order immediately on the owner/current processing group. Inaccessible group branches reject
    /// while later siblings continue. Camera history operations publish on the owner after a joined worker stage.
    /// Detached or scene-wide disabled calls have no effect.</remarks>
    /// <exception cref="InvalidOperationException">An attached call is outside the owner/current processing group or capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="AggregateException">A notification callback failed after later descendants were attempted.</exception>
    public void ResetPhysicsInterpolation()
    {
        EnsureProcessingAccess();
        RenderingServer.NotifyRenderMutation();
        if (Tree?.IsPhysicsInterpolationActive == true)
            PropagateNotificationCore(NotificationResetPhysicsInterpolation, stopAtWindows: false, processingScope: true);
    }
}
