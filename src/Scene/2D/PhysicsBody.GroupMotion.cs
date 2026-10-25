namespace Electron2D;

public abstract partial class PhysicsBody
{
    private (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) _groupMotion;
    private long _groupMotionAttachment;
    internal bool IsGroupMotionAuthoring => Tree is { IsProcessingWorkerStage: true } tree &&
        ReferenceEquals(SceneTree.CurrentProcessingGroup?.Tree, tree);

    internal void PrepareGroupMotion()
    {
        PreparePendingServerMode();
        if (HasBackend)
        {
            var motion = Backend.GetRetainedSolverMotion();
            _groupMotion = (motion.LinearVelocity, motion.AngularVelocity, !Backend.IsAwake);
        }
        else _groupMotion = (Runtime.GetLinearVelocity(), Runtime.GetAngularVelocity(), Runtime.GetSleeping());
        _groupMotionAttachment = Backend.AttachmentVersion;
    }

    internal void EnsureMotionReadAccess()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        if (IsGroupMotionAuthoring) EnsureMotionWriteAccess();
    }

    internal void EnsureMotionMutable()
    {
        if (IsGroupMotionAuthoring) EnsureTransformMutable(); else EnsureMutable();
    }

    internal void EnsureMotionWriteAccess()
    {
        if (!IsGroupMotionAuthoring) { Space?.EnsureImplementationWriteAccess(); return; }
        Tree!.EnsureProcessingAccess(this);
        if (_groupMotionAttachment != Backend.AttachmentVersion)
            throw new InvalidOperationException("The physical motion snapshot belongs to an expired attachment.");
        Space?.EnsureSceneTransformWriteAccess(Tree);
    }

    internal Vector2 GroupLinearVelocity => _groupMotion.LinearVelocity;
    internal float GroupAngularVelocity => _groupMotion.AngularVelocity;
    internal bool GroupSleeping => _groupMotion.Sleeping;

    internal void ObserveGroupMotion(Vector2? linear, float? angular, bool? awake, bool? canSleep)
    {
        _groupMotion = (awake == false ? default : linear ?? _groupMotion.LinearVelocity,
            awake == false ? 0 : angular ?? _groupMotion.AngularVelocity,
            EffectiveBodyMode == PhysicsServer.BodyMode.Static || (awake is { } active ? !active : canSleep == false ? false : _groupMotion.Sleeping));
    }
}
