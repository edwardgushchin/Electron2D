# CharacterBody

Last updated: 2026-10-14

[Character motion validation](../components/physics-snapshots.md#character-motion-validation)
rejects impossible slide encodings and self/Area/unbound platform references before
packet or world mutation. CPU/registered serial/separate cases preserve previous
state/events and verify real movement after valid platform correction.

[Portable contact/motion verification](../components/physics-snapshots.md#portable-contact-and-motion-history)
now captures/restores actual moved/query-prepared characters. Local replay keeps
the query flag with the retained solver pose; portable version 3 encodes it
explicitly. Corrected query geometry is visible immediately, and the next physics
interval still consumes pending movement through the ordinary target path.

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [CharacterBody.cs](../../src/Scene/2D/CharacterBody.cs), [CharacterBody.Motion.cs](../../src/Scene/2D/CharacterBody.Motion.cs)
- **Declaration:** `public partial class CharacterBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A caller-driven kinematic body that turns `Velocity` into safe travel and slides along body contacts. [MoveAndSlide](#moveandslide) classifies floor, wall and ceiling in grounded mode; floating mode treats every hit as a wall. The body borrows direct [CollisionShape](CollisionShape.md) or [CollisionPolygon](CollisionPolygon.md) children, shares the SceneTree physics space, and records caller-owned [KinematicCollision](KinematicCollision.md) snapshots. A call during a physics frame uses the tree's physical duration; every other call uses its process duration. The character need not receive its own callback. Before a lane's first frame, or after a zero-duration frame in that lane, velocity contributes zero travel. Initial penetration may still recover.

## Example

Partial snippet inside a character subclass's fixed physics callback:

```csharp
protected override void OnPhysicsProcess(double delta)
{
    Velocity += GetGravity() * (float)delta;
    MoveAndSlide();
}
```

The subclass needs a live collision child and `PhysicsProcessEnabled = true`. Gravity is queried; the character does not add it to `Velocity` automatically.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public CharacterBody()` | — | Grounded character with zero velocity. |
| `public Vector2 Velocity { get; set; }` | zero | Desired scene units per second; finite with finite length. |
| `public CharacterMotionMode MotionMode { get; set; }` | Grounded | Floor/ceiling classification or floating all-wall motion. |
| `public CharacterPlatformOnLeave PlatformOnLeave { get; set; }` | AddVelocity | Transfer platform point velocity when leaving it. |
| `public float SafeMargin { get; set; }` | 0.08 | Finite nonnegative contact recovery margin, scene units. |
| `public bool FloorStopOnSlope { get; set; }` | true | Stop downward velocity on floor contact. |
| `public bool FloorConstantSpeed { get; set; }` | false | Preserve lateral travel speed over floor slopes. |
| `public bool FloorBlockOnWall { get; set; }` | true | Block forward floor movement at a wall. |
| `public bool SlideOnCeiling { get; set; }` | true | Slide along upward ceiling contact. |
| `public int MaxSlides { get; set; }` | 4 | Positive cap on movement iterations. |
| `public float FloorMaxAngle { get; set; }` | π/4 | Maximum classified floor angle, radians. |
| `public float FloorSnapLength { get; set; }` | 1 | Finite nonnegative floor snap distance, scene units. |
| `public float WallMinSlideAngle { get; set; }` | π/12 | Floating-mode minimum wall slide angle, radians. |
| `public Vector2 UpDirection { get; set; }` | (0, -1) | Finite nonzero normalized grounded up direction. |
| `public uint PlatformFloorLayers { get; set; }` | all bits | Floor platform layers eligible for carry. |
| `public uint PlatformWallLayers { get; set; }` | zero | Wall platform layers eligible for carry. |
| `public bool MoveAndSlide()` | — | Move over current frame delta; return whether a contact was recorded. |
| `public void ApplyFloorSnap()` | — | Snap toward a qualifying floor when not already on one. |
| `public bool IsOnFloor()` / `IsOnWall()` / `IsOnCeiling()` | false | Last movement classification. |
| `public bool IsOnFloorOnly()` / `IsOnWallOnly()` / `IsOnCeilingOnly()` | false | Exclusive last movement classification. |
| `public Vector2 GetFloorNormal()` / `GetWallNormal()` | zero | Last classified global normals. |
| `public float GetFloorAngle(Vector2? upDirection = null)` | — | Positive angle to global up by default, radians. |
| `public Vector2 GetLastMotion()` | zero | Travel of the last slide, scene units. |
| `public Vector2 GetPositionDelta()` | — | Global translation since the last MoveAndSlide began; meaningful after a movement call. |
| `public Vector2 GetRealVelocity()` | zero | Actual displacement over that frame delta, scene units per second. |
| `public Vector2 GetPlatformVelocity()` | zero | Last contacted platform point velocity, scene units per second. |
| `public int GetSlideCollisionCount()` | 0 | Number of recorded motion contacts. |
| `public KinematicCollision GetSlideCollision(int index)` / `GetLastSlideCollision()` | — / null | Caller-owned contact snapshots. |
| `public void GetSlideCollision(int index, KinematicCollision result)` | — | Copies one indexed contact into a live reusable snapshot. |
| `public bool GetLastSlideCollision(KinematicCollision result)` | — | Copies the last contact; clears the destination and returns false on a miss. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores all query options, not transient contact state. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact CharacterBody type in PackedScene. |
| `protected override void OnEnterTree()` | — | Resets transient floor/wall/platform and slide state. |

## Property descriptions

`Velocity` is desired velocity. `MoveAndSlide()` multiplies it by the current frame delta and may remove a vertical component after floor or ceiling contact. It does not apply gravity automatically. Invalid or overflowing values reject before mutation. `MotionMode` takes [CharacterMotionMode](CharacterMotionMode.md); `PlatformOnLeave` takes [CharacterPlatformOnLeave](CharacterPlatformOnLeave.md). Undefined enum values reject. These named enums are separate types because C# cannot use `MotionMode` or `PlatformOnLeave` as both an enum and property identifier.

`SafeMargin` affects initial depenetration. `FloorMaxAngle` and `UpDirection` classify a contact as floor or ceiling; other contacts become walls. `UpDirection` normalizes finite nonzero input, and `GetFloorAngle` defaults to global `(0, -1)`. `FloorSnapLength` sets a minimum downward probe of at least `SafeMargin`. `FloorStopOnSlope`, `FloorConstantSpeed`, `FloorBlockOnWall`, `SlideOnCeiling`, `WallMinSlideAngle` and `MaxSlides` control the grounded/floating branches described below. `MaxSlides` must be at least one; angle settings must be finite, and snap/margin lengths must be nonnegative. Invalid assignments retain the prior value.

`PlatformFloorLayers` and `PlatformWallLayers` gate carry from a touched body's collision layer. A qualifying floor or wall contact stores that body's RID, layer and point velocity. At the next call, the character samples current velocity at its position, carries by velocity times delta while excluding the platform RID, and then performs its own movement. When contact is lost, `PlatformOnLeave` controls how much of the last platform velocity joins `Velocity`. Each of the 32 layer bits is supported.

## Method descriptions

<a id="moveandslide"></a>
### `MoveAndSlide()`

Requires an attached SceneTree and its owner thread. It prepares current body/Area fixtures before replacing the previous contact snapshot, so a preparation failure leaves the prior snapshot and pose intact. It first applies accepted platform carry, then sweeps up to `MaxSlides` times. Grounded mode classifies contacts against `UpDirection`, stops downward motion on a floor when configured, projects remainder along a surface, applies constant-speed slope adjustment and optionally snaps a previously grounded body back to a downward slope. Floating mode treats every hit as a wall and respects `WallMinSlideAngle`. A zero or blocked motion can still report initial recovery when `SafeMargin` reaches a body. The return value is true if any contact was recorded. A missing collision returns false and leaves the slide list empty.

The SceneTree prepares the visible character pose for direct queries immediately after movement. During the following fixed solver step, the selected backend receives a kinematic target measured from the preceding solved pose so its contact velocity remains available to other bodies. The character snapshot itself remains scene-owned and is not rewritten by backend integration. `GetPositionDelta()` reads current global position minus the start position of the last call; `GetRealVelocity()` uses the actual delta of that call. Before the selected tree lane's first frame its duration is zero. A zero duration yields zero velocity travel and zero real velocity; it never borrows another lane's time.

### `ApplyFloorSnap()` and contact getters

`ApplyFloorSnap()` does nothing when already on a floor. Otherwise it probes down by the larger of `FloorSnapLength` and `SafeMargin`, accepts a normal within `FloorMaxAngle`, stores floor/platform state and applies only the permitted up-axis travel. `IsOnFloor`, `IsOnWall`, `IsOnCeiling` and their `Only` variants describe the last completed movement, not future contacts. Floor/wall normals are global and copied. `GetSlideCollisionCount()` reports the stored list length; `GetSlideCollision(index)` rejects an invalid index, while `GetLastSlideCollision()` returns null on an empty list. Returned contact objects are independent caller-owned snapshots; later movement does not overwrite them. The destination overloads write into the supplied live caller-owned snapshot and create no new result object. An indexed read still rejects invalid indices; the final-contact overload clears stale fields on a miss. Null and disposed destinations reject. `CharacterBodyTests` verifies both outputs, independent copied snapshots and warmed zero allocation. Attached reads require the owner thread.

## Lifecycle, verification and limits

Tree entry resets transient collision and platform state while keeping configured options and `Velocity`. The body and its collision child obey the inherited unit-scale, zero-skew physics profile. [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks defaults, validation, PackedScene, floor/wall/ceiling and floating classification, snap, slope speed and angle, ceiling/wall controls, slide cap, floor/wall platform masks, all leave policies, Area gravity, immediate direct-query pose, fixed-lane backend sync, failure recovery and warmed zero-allocation calls on Linux/.NET 8. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) covers the shared initial-contact slope fix. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) also verifies sliding-ray floors and explicit non-sliding floor snap under [ADR 0068](../decisions/physics.md#adr-0068). Native allocator, other platforms, large-world throughput and owner visual acceptance remain unverified. See [ADR 0067](../decisions/physics.md#adr-0067).

## Disabled processing and physics

Inherited `DisableMode.MakeStatic` temporarily replaces the native kinematic role with static. Typed manual movement remains available; fixed-step static pose synchronization teleports without derived contact velocity. Enable/KeepActive restores kinematic participation. Remove detaches the body and its query fixtures until processing is enabled or the policy changes. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Typed server field policy

PhysicsServer BodySetGravityScale changes the selected Area/world field reported by GetGravity and PhysicsDirectBodyState.TotalGravity together. The method does not integrate that field into user Velocity. Body damping values/modes retain their shared runtime policy while kinematic inverse dynamics stays disabled. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) checks the non-rigid projection under [ADR 0076](../decisions/physics-mass.md#adr-0076).

PhysicsServer linear/angular state is low-level contact velocity, independent of
this body's desired movement Velocity. It supplies virtual surface motion beside
actual manual target travel and survives removal/reentry. It is not an additional
packed scene property. See [typed state](PhysicsServer.md#body-state).


[Shared public CPU/GPU scene checks](../components/physics-backends.md#public-scene-motion-conformance)
now run CharacterBodyTests with explicit backend selection. The linked record
separates verified motion/lifetime/allocation cases from remaining physics coverage.

The idle allocation fixture completes the shared double collection/finalization
preparation after 64 warm calls, then measures 64 calls at strict zero owner and
all-thread bytes. Optional calibrated tracing records the exact interval. The
macOS x64 aggregate at `ed0c3f4ef` failed the earlier unprepared all-thread gate
without a numeric delta; the prepared target fixture still requires verification.

All internal recovery, sweep and platform-motion queries use the [selected motion boundary](../components/physics-backends.md#selected-motion-result-publication). Returned geometry, fractions, logical slots and filtering are validated before that individual movement applies travel. A rejected query cannot move the body through malformed numerical output; earlier successful movements within a slide call are not rolled back.

The complete shared PhysicsSurfaceVelocityTests and PhysicsServerStateTests now execute on explicitly selected public CPU/GPU worlds. Platform carry retains the stationary surface channel; the configured movement Velocity remains independent of low-level body/contact velocity through detach/reentry. Broader CharacterBody and inherited, platform and final performance acceptance remain open.

[Portable contact and motion history](../components/physics-snapshots.md#portable-contact-and-motion-history)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Imported points/impulses and character slides retain destination peer/slot identity;
raw Area/contact history stays silent during correction and avoids repeated enters.
Malformed indices/platforms preserve state and events, and real movement retains
platform carry. Warm contact cycles require 0/0 bytes; cold motion validation has
no allocation claim. Hardware and wider geometry/joint/network gates remain open.

[Capture topology](../components/physics-snapshots.md#capture-after-topology-edits)
and [same-count authoring validation](../components/physics-snapshots.md#same-count-authoring-validation)
now pass CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Each capture profile retains 24 removal/reindex/readdition cases; each geometry
profile retains 14 incompatible/restored cases across both directions. Rejection
preserves complete packets, sampled values and events; real refresh/continuation
recovers. These cold structural checks have no allocation claim. Hardware and
broader geometry/joint/network acceptance remain open.


[Registered priority recovery](../components/physics-registered-adapter.md#registered-priority-recovery)
now exercises weighted motion recovery independently through the public API,
including finite/directed families, bounded contact planes, exclusions, one-way
controls, character movement and unchanged rigid momentum. The shared CPU/GPU/
registered profile covers five 128-warmup/128-measured full-frame brackets at zero
managed allocation. Wider registered contact settings and complete numerical,
native/network/platform/performance acceptance remain open.


[Moving one-way recovery](../components/gpu-resident-motion-queries.md#moving-one-way-recovery)
now includes kinematic/dynamic linear approach over the last physical step on
both production backends, including private/portable restoration. Static surface,
receding/tangent and angular-only velocities do not enlarge recovery depth.
PhysicsMovingOneWayTests checks raw/scene callers, roles, rotated slots, six finite
source families and eight 128-warmup/128-measured complete-frame brackets at zero
managed allocation. Native/platform/hardware/performance acceptance remains open.

## Automatic typed callbacks (2026-10-14)

Ready notification now enables each overridden process, physics, input, shortcut,
unhandled-key and unhandled-input callback before OnReady. Detached defaults are
false; OnReady and later setters can disable the lanes. Consumed ready does not
repeat on ordinary reentry; RequestReady re-arms it. Manual ready notification
runs the same preparation without consuming managed readiness. Inherited overrides
participate, while hidden non-virtual methods and plain nodes stay disabled.

The runtime inspects only six strongly bound virtual delegate targets on cold
first-type preparation and caches a bit mask under weak CLR Type keys. It retains
no node/delegate, performs no name lookup/assembly scan/reflection invocation and
adds no frame work. Ordinary typed dispatch, pause/membership/owner/error guards,
internal lanes and selected CPU/GPU numerical ownership stay in place.

PhysicsAutomaticCallbackTests uses real public character motion and all input
stages, detached/ready flags, inherited/individual/hidden overrides, live disabling,
reentry, requested/manual ready, packed reconstruction, pause and weak retirement.
A complete physics/idle/input bracket warms 128 and measures 128 at strict zero
owner/all-thread managed bytes. A public-only standalone consumer supports the
same compiled test in ordinary and NativeAOT hosts. Evidence and exact limits are
retained under bin/physics-auto-callbacks/2026-10-14; full inherited, native/foreign
and hardware/window/performance acceptance remains open.

[Processing-group motion](../components/physics-group-motion.md) permits current-group velocity/state authoring with ordered owner publication, prepared activity observations and retained failure/retry state. Numerical operations and other authoring families retain their individual access rules.
