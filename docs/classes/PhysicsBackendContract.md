# PhysicsBackendContract

Last updated: 2026-10-11

**Declaration:** `internal abstract class PhysicsBackendContract` · **Source:** [PhysicsBackendContract.cs](../../src/Servers/Physics/PhysicsBackendContract.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Defines every world, collider/joint creation, query, lifecycle and history operation consumed by the registered adapter. All operations use logical RIDs and typed engine-unit values. This contract is internal; it is not the missing public PhysicsServerBackend/PhysicsServerExtension family.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

## Internal operation family

| Declaration | Contract |
| --- | --- |
| `internal void Claim(RID space)` | Permanently claims one implementation context; repeated ownership rejects before use. |
| `internal abstract bool HasFailure { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool ResultsReady { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int PreparedBodyCapacity { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Initialize()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetActive(bool active)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareCheckpoint()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PreparePortableCapture()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void InvalidateCollisionExceptions(RID collider)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SynchronizeCollisionExceptions()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsBackendCheckpoint CreateCheckpoint()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void BeginPortableRestore()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int GetPortableOneWayCount()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract float ReadPortableSleepTime(RID collider)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int ReadDebugContacts(Span<Vector2> output)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetSleepSettings(in PhysicsSleepSettings settings)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetContactSettings(in PhysicsContactSettings settings)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetSolverIterations(int value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetConstraintDefaultBias(float value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareSolverCapacity(int bodies, int shapes, int joints)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareMonitoringCapacity(int bodies, int shapes)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsBackendStatistics ReadStatistics()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsBackendCollider CreateCollider(RID collider, bool area)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsBackendJoint CreateJoint(RID joint)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal virtual PhysicsDirectSpaceState CreateDirectSpaceState(RID space) => new(space)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal virtual PhysicsDirectBodyState CreateDirectBodyState(RID body) => new(PhysicsServer.Service.BodyRuntime(body), PhysicsServer.Service.GetSceneSpace(_space)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void BeginStep(double delta)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareBodies(double delta)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Solve(double delta)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SyncResults()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void CompleteBody(RID body)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void CollectContacts()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ScanAreas()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int GetAreaOverlapCount(RID area)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool AreaContainsPoint(RID area, Vector2 point, uint mask)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void EndStep(Exception? failure)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void FlushQueries()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void EndSync(Exception? failure)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, ReadOnlySpan<RID> excluded, bool collideWithAreas, bool collideWithBodies, bool hitFromInside)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Finish()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The test solver implements a bounded circle/spring model; it is not a production backend or a public-only consumer. Unexercised operation families and complete public registration/custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
