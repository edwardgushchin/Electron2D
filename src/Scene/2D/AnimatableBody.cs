namespace Electron2D;

/// <summary>A manually moved kinematic body whose motion influences dynamic contacts.</summary>
/// <remarks>With <see cref="SyncToPhysics"/> enabled, a new transform is presented after the next nonzero
/// fixed step. The body borrows child collision shapes and its inherited surface material.</remarks>
public sealed class AnimatableBody : StaticBody
{
    private static readonly PropertyDescriptor[] AnimatableProperties =
    [
        new PropertyDescriptor<AnimatableBody, bool>(nameof(SyncToPhysics), body => body.SyncToPhysics,
            (body, value) => body.SyncToPhysics = value, _ => true, stored: true)
    ];

    private bool _syncToPhysics = true;
    private bool _applyingPhysicsPose;
    private bool _hasTarget;
    private Transform _lastValidTransform = Transform.Identity;
    private Transform _targetTransform = Transform.Identity;

    /// <summary>Creates a detached kinematic body with synchronized presentation enabled.</summary>
    public AnimatableBody() { }

    /// <summary>Gets or sets whether transform changes are presented after the next fixed physics step.</summary>
    /// <value>True by default. False shows a caller's transform change immediately.</value>
    /// <remarks>Both modes send the target through the kinematic solver, deriving contact velocity from
    /// the change over one fixed step. Attached access requires the scene owner thread.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The body has been disposed.</exception>
    public bool SyncToPhysics
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _syncToPhysics; }
        set
        {
            EnsureMutable();
            if (_syncToPhysics == value) return;
            if (!value && _hasTarget)
            {
                _applyingPhysicsPose = true;
                try { GlobalTransform = _targetTransform; }
                finally { _applyingPhysicsPose = false; }
            }
            _syncToPhysics = value;
            _hasTarget = false;
            _lastValidTransform = GlobalTransform;
            if (IsInsideTree) NotifyLocalTransformChanges = value;
        }
    }

    internal override PhysicsServer.BodyMode RequestedBodyMode => PhysicsServer.BodyMode.Kinematic;

    internal override void ApplySceneTransform(Vector2 position, float rotation) { }

    internal void PrepareMotion(double delta)
    {
        var target = _syncToPhysics && _hasTarget ? _targetTransform : GlobalTransform;
        if (!target.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(target.Skew))
            throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
        if (_syncToPhysics && !_hasTarget && target == _lastValidTransform && !PhysicsMadeStatic)
        {
            Backend.ClearVelocity();
            return;
        }
        if (PhysicsMadeStatic) Backend.SetPose(target);
        else Backend.SetTargetPose(target, delta);
    }

    internal void SyncPose()
    {
        if (!_syncToPhysics) { _hasTarget = false; return; }
        var pose = Backend.GetPose();
        var solved = new Transform(pose.Rotation, Vector2.One, 0, pose.Position);
        _hasTarget = false;
        _lastValidTransform = solved;
        if (GlobalTransform == solved) return;
        _applyingPhysicsPose = true;
        try { GlobalTransform = solved; }
        finally { _applyingPhysicsPose = false; }
        _lastValidTransform = GlobalTransform;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(AnimatableProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(AnimatableBody)
        ? CreateAnimatableBody : base.CreateSceneInstanceFactory();

    private static Node CreateAnimatableBody() => new AnimatableBody();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _lastValidTransform = GlobalTransform;
        _hasTarget = false;
        NotifyLocalTransformChanges = _syncToPhysics;
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        NotifyLocalTransformChanges = false;
        _hasTarget = false;
        base.OnExitTree();
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationLocalTransformChanged && _syncToPhysics && IsInsideTree)
        {
            if (_applyingPhysicsPose) return;
            var target = GlobalTransform;
            _applyingPhysicsPose = true;
            try { GlobalTransform = _lastValidTransform; }
            finally { _applyingPhysicsPose = false; }
            if (!target.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(target.Skew))
                throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
            _targetTransform = target;
            _hasTarget = true;
        }
        base.OnNotification(what);
    }
}
