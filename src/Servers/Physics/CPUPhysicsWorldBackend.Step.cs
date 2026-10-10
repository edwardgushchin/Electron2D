using Box2D.NET;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private bool _hasKinematicBodies, _resultsReady;
    internal override bool ResultsReady => _resultsReady;
    internal override void BeginStep(double delta) { _resultsReady = false; EnsureAccess(); }
    internal override void PrepareWorld(double delta) => Space.PrepareAreaFields();
    internal override void PrepareBodies(double delta) { _hasKinematicBodies = Space.PrepareBodyStates(delta); PrepareInterval(); }
    internal override void Solve(double delta)
    {
        var world = b2GetWorldFromId(WorldID); world.contactBiasDuration = (float)delta;
        var warmStarting = world.enableWarmStarting;
        try { if (Space.PortableColdStep) world.enableWarmStarting = false; Space.StepKinematicPaths(delta, _hasKinematicBodies); Space.PortableColdStep = false; }
        finally { world.enableWarmStarting = warmStarting; }
        _resultsReady = true; Space.AdvanceStepTick();
    }
    internal override void SyncResults() => EnsureAccess();
    internal override void CompleteBody(PhysicsServerCollider body) => body.CompleteMotion();
    internal override void CompleteBody(PhysicsBody body)
    {
        body.CompleteBackend();
        if (body is AnimatableBody animatable) animatable.SyncPose();
        else if (body is CharacterBody character) character.CaptureSolverPose();
    }
    internal override void CollectContacts()
    {
        Space.CaptureBodyMotions(); var world = b2GetWorldFromId(WorldID); var count = Space.Bodies.Count;
        if (world.workerCount == 1 || count < 256) PhysicsSpace.CollectBodyContactRange(0, count, 0, Space);
        else
        {
            var end = count * (world.workerCount - 1) / world.workerCount;
            var task = _tasks.Enqueue(PhysicsSpace.CollectBodyContacts, end, end / (world.workerCount - 1), Space, Space);
            try { PhysicsSpace.CollectBodyContactRange(end, count, 0, Space); }
            finally { if (task is not null) _tasks.Finish(task, Space); }
        }
    }
    internal override void ScanAreas() { Space.ScanAreas(); Space.ScanAreaMonitors(); }
    internal override void EndStep(Exception? failure) => Space.PruneOneWayPairs();
}
