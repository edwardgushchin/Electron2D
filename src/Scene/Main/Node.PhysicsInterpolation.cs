namespace Electron2D;

public partial class Node
{
    /// <summary>Identifies a recursive request to reset the displayed physics transform to the current logical transform.</summary>
    public const int NotificationResetPhysicsInterpolation = 2001;

    private PhysicsInterpolationMode _physicsInterpolationMode;

    /// <summary>Gets or sets the inherited physics interpolation policy.</summary>
    /// <value>Inherit for ordinary nodes; Control starts Off.</value>
    /// <remarks>The effective policy applies only while the scene tree enables interpolation. A changed active policy
    /// resets affected canvas snapshots before subsequent presentation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined mode.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off-owner or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public PhysicsInterpolationMode PhysicsInterpolationMode
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _physicsInterpolationMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_physicsInterpolationMode == value) return;
            _physicsInterpolationMode = value;
            if (Tree is not null)
            {
                if (IsPhysicsInterpolated()) PropagateNotification(NotificationResetPhysicsInterpolation);
                else
                    foreach (var descendant in EnumerateDepthFirst())
                        if (descendant is Camera camera && !camera.IsDisposed)
                            camera.RefreshInterpolationProcessing();
                        else if (descendant is MultiMeshInstance instances && !instances.IsDisposed)
                            instances.RefreshInterpolation();
            }
        }
    }

    /// <summary>Reports the inherited node policy independently of scene-wide enablement.</summary>
    /// <returns>True when this node selects On or inherits an enabled ancestor; a root inherits On.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IsPhysicsInterpolated()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return _physicsInterpolationMode switch
        {
            PhysicsInterpolationMode.On => true,
            PhysicsInterpolationMode.Off => false,
            _ => Parent?.IsPhysicsInterpolated() ?? true,
        };
    }

    /// <summary>Reports whether physics interpolation currently affects this active node.</summary>
    /// <returns>False while detached or when the scene-wide setting or inherited policy is off.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IsPhysicsInterpolatedAndEnabled()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return Tree?.IsPhysicsInterpolationActive == true && IsPhysicsInterpolated();
    }

    /// <summary>Resets this node and descendants to their current logical transforms before rendering.</summary>
    /// <remarks>While interpolation is enabled, delivers <see cref="NotificationResetPhysicsInterpolation"/> in
    /// parent-first order. Detached or scene-wide disabled calls have no effect.</remarks>
    /// <exception cref="InvalidOperationException">An attached call is off the scene owner thread.</exception>
    /// <exception cref="AggregateException">A notification callback failed after later descendants were attempted.</exception>
    public void ResetPhysicsInterpolation()
    {
        EnsureMutable();
        if (Tree?.IsPhysicsInterpolationActive == true)
            PropagateNotification(NotificationResetPhysicsInterpolation);
    }
}
