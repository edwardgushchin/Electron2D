using Box2D.NET;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private static readonly b2TaskCallback CollectBodyContacts = PhysicsSpace.CollectBodyContactRange;
    private bool _hasKinematicBodies, _resultsReady, _nativeStepStarted;
    internal override bool ResultsReady => _resultsReady;
    internal override void BeginStep(double delta) { _resultsReady = false; EnsureAccess(); }
    internal override void PrepareWorld(double delta) => PrepareAreaFields();
    internal override void PrepareBodies(double delta) { _hasKinematicBodies = PrepareBodyStates(delta); PrepareInterval(); }
    internal override void Solve(double delta)
    {
        var world = b2GetWorldFromId(WorldID); world.contactBiasDuration = (float)delta;
        _nativeStepStarted = false;
        var warmStarting = world.enableWarmStarting;
        try { if (Space.PortableColdStep) world.enableWarmStarting = false; StepKinematicPaths(delta, _hasKinematicBodies); Space.PortableColdStep = false; }
        catch (Exception error) when (_nativeStepStarted && _solverFailure is null)
        {
            _solverFailure ??= error;
            try { _tasks.Drain(); }
            finally
            {
                world.locked = false;
                foreach (var arena in world.arena.AsSpan()) arena.Abort();
                world.reusableStepContext.Reset();
            }
            throw;
        }
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
    internal override void CollectContacts(ref List<Exception>? errors)
    {
        CaptureBodyMotions(); var world = b2GetWorldFromId(WorldID);
        _tasks.RunRanges(CollectBodyContacts, Space.Bodies.Count, Space.ForceSerialContactPublication ? 1 : world.workerCount, Space);
    }
    internal override void EndStep(Exception? failure) => PruneOneWayPairs();
}
