using Box2D.NET;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>Owner-thread CPU solver history for an unchanged set of authored object identities.</summary>
internal sealed class CPUPhysicsCheckpoint : IDisposable
{
    private readonly B2WorldId _id;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private B2World? _saved = new();
    private bool _valid;

    internal CPUPhysicsCheckpoint(B2WorldId world) { _id = world; Capture(); }

    internal void Capture()
    {
        var source = Check(); _valid = false;
        Copy(source, _saved!, true); Copy(source, _saved!, false); _valid = true;
    }

    internal void Restore()
    {
        var destination = Check();
        if (!_valid) throw new InvalidOperationException("The checkpoint has no completed capture.");
        var saved = _saved!;
        ValidateIdentities(saved, destination);
        if (saved.workerCount != destination.workerCount || saved.enqueueTaskFcn != destination.enqueueTaskFcn ||
            saved.finishTaskFcn != destination.finishTaskFcn || !ReferenceEquals(saved.userTaskContext, destination.userTaskContext) ||
            saved.frictionCallback != destination.frictionCallback ||
            saved.restitutionCallback != destination.restitutionCallback || saved.preSolveFcn != destination.preSolveFcn ||
            !ReferenceEquals(saved.preSolveContext, destination.preSolveContext) || saved.customFilterFcn != destination.customFilterFcn ||
            !ReferenceEquals(saved.customFilterContext, destination.customFilterContext))
            throw new InvalidOperationException("CPU checkpoint callbacks and task configuration must still match.");
        // Reserve every nested destination before overwriting any live logical state.
        Copy(saved, destination, true); Copy(saved, destination, false);
    }

    private B2World Check()
    {
        ObjectDisposedException.ThrowIf(_saved is null, this);
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("A CPU checkpoint requires its owner thread.");
        if (!b2World_IsValid(_id)) throw new ObjectDisposedException("CPU checkpoint world");
        var world = b2GetWorldFromId(_id);
        if (world.locked || world.activeTaskCount != 0) throw new InvalidOperationException("CPU checkpoints require a completed physics interval.");
        if (world.integrateBodyStage is not null || world.solveConstraints is not null || world.finalizeBodyStates is not null ||
            world.generateManifolds is not null || world.findBroadPhasePairs is not null || world.createBroadPhaseContacts is not null ||
            world.contactLinksChanged is not null || world.destroyDisjointContact is not null || world.finishContactRemovals is not null ||
            world.splitIsland is not null || world.beginIslandChanges is not null || world.finishIslandChanges is not null ||
            world.islandGraphChanged is not null || world.beginConstraintColors is not null || world.finishConstraintColors is not null ||
            world.selectConstraintColor is not null || world.changeContactIsland is not null || world.contactPairChanged is not null ||
            world.shapeFilterChanged is not null || world.shapeGeometryChanged is not null || world.jointFilterChanged is not null)
            throw new InvalidOperationException("CPU checkpoints cannot restore external stage-provider state.");
        return world;
    }

    private static void ValidateIdentities(B2World a, B2World b)
    {
        if (a.bodies.count != b.bodies.count || a.shapes.count != b.shapes.count || a.joints.count != b.joints.count || a.chainShapes.count != b.chainShapes.count) Reject();
        for (var i = 0; i < a.bodies.count; i++)
            if (a.bodies.data[i].id != b.bodies.data[i].id || a.bodies.data[i].generation != b.bodies.data[i].generation) Reject();
        for (var i = 0; i < a.shapes.count; i++)
            if (a.shapes.data[i].id != b.shapes.data[i].id || a.shapes.data[i].generation != b.shapes.data[i].generation) Reject();
        for (var i = 0; i < a.joints.count; i++)
            if (a.joints.data[i].jointId != b.joints.data[i].jointId || a.joints.data[i].generation != b.joints.data[i].generation) Reject();
        for (var i = 0; i < a.chainShapes.count; i++)
            if (a.chainShapes.data[i].id != b.chainShapes.data[i].id || a.chainShapes.data[i].generation != b.chainShapes.data[i].generation) Reject();
    }
    private static void Reject() => throw new InvalidOperationException("CPU checkpoint restore requires the captured body, shape, chain and joint identities.");

    public void Dispose()
    {
        if (_saved is null) return;
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("A CPU checkpoint requires its owner thread.");
        _saved = null; _valid = false;
    }

    private static void Copy(B2World a, B2World b, bool prepare)
    {
        Records(a.bodies, ref b.bodies, CopyBody, prepare);
        Records(a.shapes, ref b.shapes, CopyShape, prepare);
        Records(a.joints, ref b.joints, CopyJoint, prepare);
        Records(a.contacts, ref b.contacts, CopyContact, prepare);
        Records(a.islands, ref b.islands, CopyIsland, prepare);
        Records(a.chainShapes, ref b.chainShapes, CopyChain, prepare);
        Records(a.sensors, ref b.sensors, CopySensor, prepare);
        Records(a.solverSets, ref b.solverSets, CopySet, prepare);
        Pool(a.bodyIdPool, ref b.bodyIdPool, prepare);
        Pool(a.shapeIdPool, ref b.shapeIdPool, prepare);
        Pool(a.jointIdPool, ref b.jointIdPool, prepare);
        Pool(a.chainIdPool, ref b.chainIdPool, prepare);
        Pool(a.contactIdPool, ref b.contactIdPool, prepare);
        Pool(a.islandIdPool, ref b.islandIdPool, prepare);
        Pool(a.solverSetIdPool, ref b.solverSetIdPool, prepare);
        Values(a.bodyMoveEvents, ref b.bodyMoveEvents, prepare);
        Values(a.sensorBeginEvents, ref b.sensorBeginEvents, prepare);
        Values(a.contactBeginEvents, ref b.contactBeginEvents, prepare);
        Values(a.contactHitEvents, ref b.contactHitEvents, prepare);
        Values(a.jointEvents, ref b.jointEvents, prepare);
        for (var i = 0; i < 2; i++)
        { Values(a.sensorEndEvents[i], ref b.sensorEndEvents[i], prepare); Values(a.contactEndEvents[i], ref b.contactEndEvents[i], prepare); }
        b.broadPhase ??= new();
        CopyBroadPhase(a.broadPhase, b.broadPhase, prepare);
        b.constraintGraph.colors ??= new B2GraphColor[a.constraintGraph.colors.Length];
        for (var i = 0; i < a.constraintGraph.colors.Length; i++)
        {
            ref var source = ref a.constraintGraph.colors[i]; ref var target = ref b.constraintGraph.colors[i];
            Bits(source.bodySet, ref target.bodySet, prepare);
            Records(source.contactSims, ref target.contactSims, CopyContactSim, prepare);
            Records(source.jointSims, ref target.jointSims, CopyJointSim, prepare);
        }
        if (prepare) return;
        b.stepIndex = a.stepIndex;
        b.splitIslandId = a.splitIslandId;
        b.gravity = a.gravity;
        b.hitEventThreshold = a.hitEventThreshold;
        b.restitutionThreshold = a.restitutionThreshold;
        b.maxLinearSpeed = a.maxLinearSpeed;
        b.contactSpeed = a.contactSpeed;
        b.contactHertz = a.contactHertz;
        b.contactDampingRatio = a.contactDampingRatio;
        b.sleepAngularThreshold = a.sleepAngularThreshold;
        b.timeToSleep = a.timeToSleep;
        b.solverIterations = a.solverIterations;
        b.contactRecycleRadius = a.contactRecycleRadius;
        b.contactMaxSeparation = a.contactMaxSeparation;
        b.contactBias = a.contactBias;
        b.contactAllowedPenetration = a.contactAllowedPenetration;
        b.contactBiasDuration = a.contactBiasDuration;
        b.inv_h = a.inv_h;
        b.inv_dt = a.inv_dt;
        b.enableSleep = a.enableSleep;
        b.enableWarmStarting = a.enableWarmStarting;
        b.enableContactSoftening = a.enableContactSoftening;
        b.enableContinuous = a.enableContinuous;
        b.enableSpeculative = a.enableSpeculative;
        b.endEventArrayIndex = a.endEventArrayIndex;
        b.frictionCallback = a.frictionCallback;
        b.restitutionCallback = a.restitutionCallback;
        b.preSolveFcn = a.preSolveFcn;
        b.preSolveContext = a.preSolveContext;
        b.customFilterFcn = a.customFilterFcn;
        b.customFilterContext = a.customFilterContext;
        b.workerCount = a.workerCount;
        b.enqueueTaskFcn = a.enqueueTaskFcn; b.finishTaskFcn = a.finishTaskFcn; b.userTaskContext = a.userTaskContext;
    }

    private static void Values<T>(in B2Array<T> a, ref B2Array<T> b, bool prepare) where T : struct
    {
        b2Array_Reserve(ref b, a.count);
        if (!prepare) { a.data.AsSpan(0, a.count).CopyTo(b.data); b.count = a.count; }
    }
    private static void Records<T>(in B2Array<T> a, ref B2Array<T> b, Action<T, T, bool> copy, bool prepare) where T : class, new()
    {
        b2Array_Reserve(ref b, a.count);
        for (var i = 0; i < a.count; i++) copy(a.data[i], b.data[i], prepare);
        if (!prepare) b.count = a.count;
    }
    private static void Data<T>(T[]? a, ref T[] b, int count, bool prepare)
    {
        if (b is null || b.Length < count) Array.Resize(ref b, count);
        if (!prepare) a.AsSpan(0, count).CopyTo(b);
    }
    private static void Pool(B2IdPool a, ref B2IdPool b, bool prepare)
    {
        b ??= new(); Values(a.freeArray, ref b.freeArray, prepare);
        if (!prepare) b.nextIndex = a.nextIndex;
    }
    private static void Bits(in B2BitSet a, ref B2BitSet b, bool prepare)
    {
        Data(a.bits, ref b.bits, a.blockCount, prepare);
        if (!prepare) { b.blockCount = a.blockCount; b.blockCapacity = b.bits.Length; }
    }
    private static void Hash(in B2HashSet a, ref B2HashSet b, bool prepare)
    {
        Data(a.items, ref b.items, a.capacity, prepare);
        if (!prepare) { b.capacity = a.capacity; b.count = a.count; }
    }
    private static void CopyBroadPhase(B2BroadPhase a, B2BroadPhase b, bool prepare)
    {
        b.trees ??= new B2DynamicTree[a.trees.Length];
        for (var i = 0; i < a.trees.Length; i++)
        {
            var source = a.trees[i]; var target = b.trees[i] ??= new();
            Data(source.nodes, ref target.nodes, source.nodeCapacity, prepare);
            if (!prepare)
            {
                target.root = source.root; target.nodeCount = source.nodeCount; target.nodeCapacity = source.nodeCapacity;
                target.freeList = source.freeList; target.proxyCount = source.proxyCount;
            }
        }
        Hash(a.moveSet, ref b.moveSet, prepare); Hash(a.pairSet, ref b.pairSet, prepare);
        Values(a.moveArray, ref b.moveArray, prepare); Values(a.boundaries, ref b.boundaries, prepare);
    }
    private static void CopySet(B2SolverSet a, B2SolverSet b, bool prepare)
    {
        Records(a.bodySims, ref b.bodySims, CopyBodySim, prepare);
        Records(a.bodyStates, ref b.bodyStates, CopyBodyState, prepare);
        Records(a.contactSims, ref b.contactSims, CopyContactSim, prepare);
        Records(a.jointSims, ref b.jointSims, CopyJointSim, prepare);
        Records(a.islandSims, ref b.islandSims, CopyIslandSim, prepare);
        if (!prepare) b.setIndex = a.setIndex;
    }
    private static void CopyChain(B2ChainShape a, B2ChainShape b, bool prepare)
    {
        Data(a.shapeIndices, ref b.shapeIndices, a.shapeIndices?.Length ?? 0, prepare); Data(a.materials, ref b.materials, a.materials?.Length ?? 0, prepare);
        if (prepare) return;
        b.id = a.id; b.bodyId = a.bodyId; b.nextChainId = a.nextChainId; b.count = a.count; b.materialCount = a.materialCount; b.generation = a.generation;
    }
    private static void CopySensor(B2Sensor a, B2Sensor b, bool prepare)
    {
        Values(a.hits, ref b.hits, prepare); Values(a.overlaps1, ref b.overlaps1, prepare); Values(a.overlaps2, ref b.overlaps2, prepare);
        if (!prepare) b.shapeId = a.shapeId;
    }
    private static void CopyBodySim(B2BodySim a, B2BodySim b, bool prepare) { if (!prepare) b.CopyFrom(a); }
    private static void CopyBodyState(B2BodyState a, B2BodyState b, bool prepare) { if (!prepare) b.CopyFrom(a); }
    private static void CopyContactSim(B2ContactSim a, B2ContactSim b, bool prepare) { if (!prepare) b.CopyFrom(a); }
    private static void CopyJointSim(B2JointSim a, B2JointSim b, bool prepare) { if (!prepare) b.CopyFrom(a); }
    private static void CopyIslandSim(B2IslandSim a, B2IslandSim b, bool prepare) { if (!prepare) b.CopyFrom(a); }

    private static void CopyBody(B2Body a, B2Body b, bool prepare)
    {
        if (prepare) return;
        b.name = a.name;
        b.userData = a.userData;
        b.setIndex = a.setIndex;
        b.localIndex = a.localIndex;
        b.headContactKey = a.headContactKey;
        b.contactCount = a.contactCount;
        b.headShapeId = a.headShapeId;
        b.shapeCount = a.shapeCount;
        b.headChainId = a.headChainId;
        b.headJointKey = a.headJointKey;
        b.jointCount = a.jointCount;
        b.islandId = a.islandId;
        b.islandPrev = a.islandPrev;
        b.islandNext = a.islandNext;
        b.mass = a.mass;
        b.inertia = a.inertia;
        b.sleepThreshold = a.sleepThreshold;
        b.sleepTime = a.sleepTime;
        b.bodyMoveIndex = a.bodyMoveIndex;
        b.id = a.id;
        b.flags = a.flags;
        b.type = a.type;
        b.generation = a.generation;
        b.enableSleep = a.enableSleep;
    }

    private static void CopyContact(B2Contact a, B2Contact b, bool prepare)
    {
        if (prepare) return;
        b.setIndex = a.setIndex;
        b.colorIndex = a.colorIndex;
        b.localIndex = a.localIndex;
        b.shapeIdA = a.shapeIdA;
        b.shapeIdB = a.shapeIdB;
        b.contactId = a.contactId;
        b.edges = a.edges;
        b.islandPrev = a.islandPrev;
        b.islandNext = a.islandNext;
        b.islandId = a.islandId;
        b.flags = a.flags;
        b.generation = a.generation;
    }

    private static void CopyShape(B2Shape a, B2Shape b, bool prepare)
    {
        if (prepare) return;
        b.id = a.id;
        b.bodyId = a.bodyId;
        b.prevShapeId = a.prevShapeId;
        b.nextShapeId = a.nextShapeId;
        b.sensorIndex = a.sensorIndex;
        b.type = a.type;
        b.material = a.material;
        b.density = a.density;
        b.customSolverBias = a.customSolverBias;
        b.aabb = a.aabb;
        b.fatAABB = a.fatAABB;
        b.localCentroid = a.localCentroid;
        b.proxyKey = a.proxyKey;
        b.filter = a.filter;
        b.userData = a.userData;
        b.manifoldOverride = a.manifoldOverride;
        b.us = a.us;
        b.generation = a.generation;
        b.enableSensorEvents = a.enableSensorEvents;
        b.enableContactEvents = a.enableContactEvents;
        b.enableCustomFiltering = a.enableCustomFiltering;
        b.enableHitEvents = a.enableHitEvents;
        b.enablePreSolveEvents = a.enablePreSolveEvents;
        b.enlargedAABB = a.enlargedAABB;
    }

    private static void CopyJoint(B2Joint a, B2Joint b, bool prepare)
    {
        if (prepare) return;
        b.userData = a.userData;
        b.setIndex = a.setIndex;
        b.colorIndex = a.colorIndex;
        b.localIndex = a.localIndex;
        b.edges = a.edges;
        b.jointId = a.jointId;
        b.islandId = a.islandId;
        b.islandPrev = a.islandPrev;
        b.islandNext = a.islandNext;
        b.drawScale = a.drawScale;
        b.type = a.type;
        b.generation = a.generation;
        b.collideConnected = a.collideConnected;
    }

    private static void CopyIsland(B2Island a, B2Island b, bool prepare)
    {
        if (prepare) return;
        b.setIndex = a.setIndex;
        b.localIndex = a.localIndex;
        b.islandId = a.islandId;
        b.headBody = a.headBody;
        b.tailBody = a.tailBody;
        b.bodyCount = a.bodyCount;
        b.headContact = a.headContact;
        b.tailContact = a.tailContact;
        b.contactCount = a.contactCount;
        b.headJoint = a.headJoint;
        b.tailJoint = a.tailJoint;
        b.jointCount = a.jointCount;
        b.constraintRemoveCount = a.constraintRemoveCount;
    }
}
