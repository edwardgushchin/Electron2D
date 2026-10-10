using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private int _preparedBodyCapacity, _preparedSleepCapacity;
    // Keep this conservative flag across private restores and retired attachments.
    private bool _maySleep;
    internal bool MaySleep => _maySleep;
    internal void PrepareSleepCapacity() { _maySleep = true; PrepareSolverCapacity(); }
    internal override int PreparedBodyCapacity => _preparedBodyCapacity;
    internal override void PrepareMonitoringCapacity()
    {
        EnsureAccess();
        var world = b2GetWorldFromId(WorldID);
        _fieldAreas.EnsureCapacity(Space.Areas.Count + Space.ServerColliders.Count);
        foreach (var sensor in world.sensors.data.AsSpan(0, world.sensors.count))
        {
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.hits, world.shapes.count);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.overlaps1, world.shapes.count);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.overlaps2, world.shapes.count);
        }
    }

    internal override void PrepareSolverCapacity()
    {
        EnsureAccess();
        var world = b2GetWorldFromId(WorldID);
        var count = world.bodyIdPool.nextIndex;
        var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(8, count));
        if (capacity <= _preparedBodyCapacity && (!_maySleep || count <= _preparedSleepCapacity)) return;
        var sleeping = 0;
        if (_maySleep)
            foreach (var body in world.bodies.data.AsSpan(0, world.bodies.count))
                if (body.id >= 0 && body.type == B2BodyType.b2_dynamicBody && body.enableSleep) sleeping++;
        var sleepCapacity = sleeping == 0 ? 0 : (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)sleeping);
        if (capacity <= _preparedBodyCapacity && sleepCapacity <= _preparedSleepCapacity) return;
        _preparedBodyCapacity = Math.Max(_preparedBodyCapacity, capacity);
        _preparedSleepCapacity = Math.Max(_preparedSleepCapacity, sleepCapacity);
        capacity = _preparedBodyCapacity; sleepCapacity = _preparedSleepCapacity;
        Space.ReserveBodyMotionCapacity(capacity);
        // Dormant island storage follows bodies that can sleep; active stress particles need no dormant copies.
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.solverSets, sleepCapacity + 3);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.solverSetIdPool.freeArray, sleepCapacity + 3);
        while (world.solverSets.count < sleepCapacity + 3)
        {
            var index = world.solverSetIdPool.nextIndex++;
            var set = new B2SolverSet { setIndex = B2_NULL_INDEX };
            Box2D.NET.B2Arrays.b2Array_Push(ref world.solverSets, set);
            Box2D.NET.B2IdPools.b2FreeId(world.solverSetIdPool, index);
        }
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        Box2D.NET.B2Arrays.b2Array_Reserve(ref awake.bodyStates, capacity);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.bodyMoveEvents, capacity);
        // ponytail: Four contacts per body is the prepared graph budget; larger topologies need explicit capacity preparation.
        var contacts = checked(capacity * 4);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.contacts, contacts);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.contactIdPool.freeArray, contacts);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.islands, capacity);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.islandIdPool.freeArray, capacity);
        for (var i = 0; i < world.solverSets.count; i++)
        {
            var set = world.solverSets.data[i];
            // ponytail: Dormant budget is 16 bodies/32 contacts; larger island topologies need explicit preparation.
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.bodySims, i < 3 ? capacity : 16);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.contactSims, i < 3 ? contacts : 32);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.jointSims, 4);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.islandSims, i < 3 ? capacity : 1);
        }
        foreach (var task in world.taskContexts.data.AsSpan(0, world.taskContexts.count))
        {
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.contactStateBitSet, contacts);
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.enlargedSimBitSet, capacity);
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.awakeIslandBitSet, capacity);
        }
        for (var index = 0; index < world.constraintGraph.colors.Length; index++)
        {
            ref var color = ref world.constraintGraph.colors[index];
            // A regular color contains at most one constraint per dynamic body; overflow has no such bound.
            Box2D.NET.B2Arrays.b2Array_Reserve(ref color.contactSims,
                index == Box2D.NET.B2ConstraintGraphs.B2_OVERFLOW_INDEX ? contacts : capacity);
            if (index != Box2D.NET.B2ConstraintGraphs.B2_OVERFLOW_INDEX && color.bodySet.blockCount < (capacity + 63) / 64)
                Box2D.NET.B2BitSets.b2GrowBitSet(ref color.bodySet, (capacity + 63) / 64);
        }
        if (capacity >= 256) PrepareArenaCapacity(world, capacity, contacts);
    }

    private static void PrepareArenaCapacity(B2World world, int bodies, int contacts)
    {
        var arena = world.arena;
        arena.GetOrCreateFor<int>().Reserve(checked(bodies * 9 + 96));
        arena.GetOrCreateFor<B2MoveResult>().Reserve(bodies + 32);
        arena.GetOrCreateFor<B2MovePair>().Reserve(checked(bodies * 32));
        arena.GetOrCreateFor<B2ContactSim>().Reserve(contacts + 128);
        arena.GetOrCreateFor<B2ContactConstraintSIMD>().Reserve(contacts / B2Cores.B2_SIMD_WIDTH + 128);
        arena.GetOrCreateFor<B2ContactConstraint>().Reserve(contacts);
        arena.GetOrCreateFor<B2SolverBlock>().Reserve((bodies + contacts * 2) / 32 + 512);
        arena.GetOrCreateFor<B2SolverStage>().Reserve(256);
    }

}
