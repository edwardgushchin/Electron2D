using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal enum ObjectKind { Floor, Sensor, Ball, Box, Pin }
internal readonly record struct ObjectSpec(ulong ID, uint Generation, ObjectKind Kind, int Owner, uint ControlEpoch,
    Vector2 Spawn, ulong First = 0, ulong Second = 0);
internal readonly record struct PlayerInput(float Move, bool Jump);
internal readonly record struct InputCommand(ulong Tick, ulong ID, uint Generation, uint Epoch, PlayerInput Input);
internal readonly record struct GameEvent(ulong Sequence, ulong Tick, ulong ID, uint Generation, ulong Other, uint OtherGeneration, int Kind);
internal enum SimulationPhase { Authority, Prediction, Replay, Correction, Shutdown }

internal sealed class NetworkWorld : IDisposable
{
    internal const int Capacity = 16;
    internal const double FixedStep = 1d / 60;
    internal sealed class Actor(ObjectSpec spec, CollisionObject? body, Joint? joint)
    {
        internal ObjectSpec Spec = spec;
        internal readonly CollisionObject? Body = body;
        internal readonly Joint? Joint = joint;
        internal PlayerInput Held;
        internal ulong LastInputTick;
        internal RID RID => Body?.GetRID() ?? Joint!.GetRID();
        internal Vector2 Position => Body?.GlobalPosition ?? default;
    }
    internal readonly World World;
    internal readonly SubViewport Root;
    internal readonly SceneTree Tree;
    internal readonly PhysicsSnapshotMap Map;
    internal readonly Actor?[] Actors = new Actor?[Capacity];
    internal SimulationPhase Phase;
    internal Action<GameEvent>? Observed;
    internal ulong EventSequence;
    internal int Created, Removed, OwnershipChanges;
    private ulong _stepTick;
    private readonly RectangleShape _floor = new() { Size = new(640, 20) };
    private readonly RectangleShape _box = new() { Size = new(24, 24) };
    private readonly RectangleShape _region = new() { Size = new(120, 100) };
    private readonly CircleShape _circle = new() { Radius = 12 };
    private bool _disposed;
    internal ulong Tick => PhysicsServer.SpaceGetTick(World.Space);
    internal NetworkWorld(PhysicsServer.Backend backend)
    {
        World = new(backend); Root = new() { World = World, Size = new(640, 360) };
        Tree = new(Root) { MultiplayerPoll = false }; _ = Root.FindWorld(); Map = new(World.Space);
    }
    internal Actor? Find(ulong id)
    {
        foreach (var actor in Actors) if (actor?.Spec.ID == id) return actor;
        return null;
    }
    internal void Reconcile(ReadOnlySpan<ObjectSpec> specs)
    {
        if (specs.Length > Capacity) throw new InvalidDataException("World manifest exceeds the actor budget.");
        for (var i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            if (spec.ID == 0 || spec.Generation == 0 || spec.ControlEpoch == 0 || spec.Owner < 0 || !spec.Spawn.IsFinite() || (uint)spec.Kind > 4)
                throw new InvalidDataException("Invalid world object definition.");
            for (var j = 0; j < i; j++) if (specs[j].ID == spec.ID) throw new InvalidDataException("Duplicate world object ID.");
            if (spec.Kind == ObjectKind.Pin && (!HasBody(specs, spec.First) || !HasBody(specs, spec.Second) || spec.First == spec.Second))
                throw new InvalidDataException("A joint must reference two distinct bodies in the same manifest.");
        }
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] is not { } actor || (actor.Joint is not null) != (pass == 0)) continue;
                var found = false;
                foreach (var spec in specs) if (spec.ID == actor.Spec.ID && SameObject(spec, actor.Spec)) { found = true; break; }
                if (!found) Remove(i);
            }
        for (var pass = 0; pass < 2; pass++)
            foreach (var spec in specs)
            {
                if ((spec.Kind == ObjectKind.Pin) != (pass == 1)) continue;
                var actor = Find(spec.ID);
                if (actor is null) Add(spec);
                else
                {
                    if (actor.Spec.Owner != spec.Owner || actor.Spec.ControlEpoch != spec.ControlEpoch)
                    { OwnershipChanges++; actor.Held = default; actor.LastInputTick = 0; }
                    actor.Spec = spec;
                }
            }
    }
    private static bool HasBody(ReadOnlySpan<ObjectSpec> specs, ulong id)
    { foreach (var spec in specs) if (spec.ID == id) return spec.Kind is ObjectKind.Ball or ObjectKind.Box or ObjectKind.Floor; return false; }
    private static bool SameObject(ObjectSpec first, ObjectSpec second) => first.ID == second.ID && first.Generation == second.Generation &&
        first.Kind == second.Kind && first.Spawn == second.Spawn && first.First == second.First && first.Second == second.Second;
    private void Add(ObjectSpec spec)
    {
        var index = Array.FindIndex(Actors, static actor => actor is null);
        if (index < 0) throw new InvalidOperationException("World actor capacity exhausted.");
        var name = "Object" + spec.ID;
        CollisionObject? body = null; Joint? joint = null;
        switch (spec.Kind)
        {
            case ObjectKind.Floor:
                body = new StaticBody { Name = name, Position = spec.Spawn };
                body.AddChild(new CollisionShape { Shape = _floor }); break;
            case ObjectKind.Sensor:
                var area = new Area { Name = name, Position = spec.Spawn, Monitoring = true };
                area.AddChild(new CollisionShape { Shape = _region });
                area.BodyEntered += other => Contact(spec.ID, other, 2); body = area; break;
            case ObjectKind.Ball:
            case ObjectKind.Box:
                var rigid = new RigidBody
                {
                    Name = name,
                    Position = spec.Spawn,
                    Mass = 1,
                    CanSleep = true,
                    MaxContactsReported = 8,
                    ContactMonitor = true,
                    LinearDamp = .2f,
                    AngularDamp = .3f
                };
                rigid.AddChild(new CollisionShape { Shape = spec.Kind == ObjectKind.Ball ? _circle : _box });
                rigid.BodyEntered += other => Contact(spec.ID, other, 1); body = rigid; break;
            case ObjectKind.Pin:
                joint = new PinJoint { Name = name, Position = spec.Spawn, NodeA = "../Object" + spec.First, NodeB = "../Object" + spec.Second };
                break;
        }
        Root.AddChild((Node?)body ?? joint!);
        var actor = new Actor(spec, body, joint); Actors[index] = actor; Map.Bind(spec.ID, spec.Generation, actor.RID); Created++;
    }
    private void Remove(int index)
    {
        var actor = Actors[index]!; Map.Unbind(actor.Spec.ID);
        var node = (Node?)actor.Body ?? actor.Joint!; Root.RemoveChild(node); node.Dispose(); Actors[index] = null; Removed++;
    }
    internal int Describe(Span<ObjectSpec> destination)
    {
        var count = 0; foreach (var actor in Actors) if (actor is not null) destination[count++] = actor.Spec;
        destination[..count].Sort(static (a, b) => a.ID.CompareTo(b.ID)); return count;
    }
    internal void SetInput(InputCommand command)
    {
        var actor = Find(command.ID);
        if (actor is null || actor.Spec.Generation != command.Generation || actor.Spec.ControlEpoch != command.Epoch) return;
        actor.Held = command.Input; actor.LastInputTick = command.Tick;
    }
    internal void BeginTick(ulong tick, SimulationPhase phase) { Phase = phase; _stepTick = tick; }
    internal void Step(SimulationPhase phase)
    {
        Phase = phase; _stepTick = Tick + 1;
        foreach (var actor in Actors)
        {
            if (actor?.Body is not RigidBody body || actor.Spec.Owner == 0) continue;
            body.ApplyCentralForce(new((actor.Held.Move * 100 - body.LinearVelocity.X) * 12, 0));
            if (actor.Held.Jump && actor.LastInputTick == _stepTick && body.Position.Y > 270) body.ApplyCentralImpulse(new(0, -260));
        }
        Tree.PhysicsFrame(FixedStep);
        if (Tick != _stepTick) throw new InvalidOperationException("Fixed simulation did not advance exactly one tick.");
    }
    private void Contact(ulong id, Node other, int kind)
    {
        if (Phase is SimulationPhase.Correction or SimulationPhase.Shutdown) return;
        var first = Find(id); Actor? second = null;
        foreach (var actor in Actors) if (ReferenceEquals(actor?.Body, other)) { second = actor; break; }
        if (first is null || second is null || kind == 1 && id > second.Spec.ID && second.Body is RigidBody) return;
        var sequence = Phase == SimulationPhase.Authority ? ++EventSequence : 0;
        Observed?.Invoke(new(sequence, _stepTick, id, first.Spec.Generation, second.Spec.ID, second.Spec.Generation, kind));
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; Phase = SimulationPhase.Shutdown;
        Map.Dispose(); Tree.Dispose(); Root.Dispose(); World.Dispose(); _floor.Dispose(); _box.Dispose(); _region.Dispose(); _circle.Dispose();
    }
}
