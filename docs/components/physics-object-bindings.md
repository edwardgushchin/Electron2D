# Physics object associations

Last updated: 2026-10-09

## Public contract

`PhysicsServer.BodyAttachObject(body, instance)` and `AreaAttachObject` accept a live
ElectronObject or null. They are the typed C# projection of assigning object-instance
identity: they capture InstanceID and borrow the target weakly. BodyGetObjectInstanceID
and AreaGetObjectInstanceID return that retained number, including after target disposal;
null clears it. Wrong-kind/freed collider RIDs, disposed targets and owner/solver violations
reject before mutation. Scene objects initially identify themselves. Explicit bindings
survive fixture rebuild, body detach/reattach and role changes; they are not scene-packed.

The existing query-result Collider property remains a physical scene-collider convenience.
ColliderObject returns the complete assigned instance for PhysicsRayResult, PhysicsPointResult,
PhysicsShapeResult and PhysicsRestInfo. ColliderID is sampled together with the weak reference.
RayCast, ShapeCast, PhysicsTestMotionResult and KinematicCollision retain that same snapshot.
Rebinding does not retarget old results. Disposal/collection makes object access null while
IDs, RIDs and contact geometry stay readable. Physical shape-owner lookup still uses its
physical RID and logical slot. The existing direct-state GetContactColliderObject(index)
keeps its scene-collider return type; GetContactColliderObject<T>(index) returns the sampled
assigned object as any ElectronObject role. Motion object exclusions use the assigned ID.

One older ShapeCast assertion expected disposal to erase ColliderID. That was incorrect:
the pinned [result construction](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/physics/shape_cast_2d.cpp)
resolves the live object separately from its saved ID. The regression now checks a null
object and retained sampled identity.

## Ownership and events

ElectronObject lazily caches one BCL weak reference. Internal ObjectIdentity values copy
that reference and the ID; no global object registry or per-frame weak-reference creation
is required. Fixture tags borrow the collider adapter's current authored association;
contact publication copies it before user callbacks. Saved results never mutate that cache.

Scene monitors deduplicate object events by assigned instance and body/Area role, while
shape pairs retain physical RID and both indices. Existing Entity/Area/Node signal and
array types remain intact. Raw server callbacks report the sampled numeric ID even when
the target is detached or disposed. Several raw colliders may identify one Entity.
Removing one physical RID removes only its contributions.

A bound Node's tree exit/reentry emits scene exits/entries immediately without detaching
its raw bodies. Reverse membership observers exist only while the collider is attached,
are removed on rebind/detach, and do not make the collider retain its target. Physical
server-pair callbacks do not change merely because the associated Node leaves the tree.
Committed event state survives throwing handlers; later deliveries still run. Structural
tree changes may allocate callback snapshots, as other tree membership paths already do.

## GPU and verification

Resident ray/point records carry ObjectID in existing padding and remain 64 bytes. Shape
intersection/contact/rest/cast records now carry ObjectID explicitly and occupy 96 bytes
instead of 80: the additional 16 bytes preserve identity with each sampled result. Body
motion already carries object ID. These are authored identities, not portable network IDs.
Public GPU-world binding remains open.

PhysicsObjectBindingTests exercises all four direct query result families, motion exclusions,
scene ray/shape/movement caches, target replacement/disposal/collection, raw body/Area bindings,
wrong-kind/lifetime/thread/solver guards, contact snapshots, shared owners, immediate node
exit/reentry and throwing callbacks. It checks all 64 bits in resident ray/point and all four
shape-query modes. Existing physical scene-collider properties remain source-compatible.

On Linux/.NET 10 Release, 128 warmup and 128 owner changes plus complete SceneTree physics
steps and point queries measured p50/p95/p99 0.0041/0.0042/0.0042 ms with 0/0 owner/all-thread
managed bytes. The scene has one raw static body, one monitored rigid body and one Area;
this is not a large-world throughput or FPS claim. Evidence: `/tmp/e2d-objects-final-focused.log`.
Vulkan/NVIDIA GeForce RTX 3090 Ti shape-query measurement with 65,536 shapes and 256 rest
queries per batch, 96 warmup/128 samples, measured 0.1382/0.7367/0.8310 ms and zero owner/all-thread
managed bytes. Upload/readback was 20,488/25,608 B per batch; readback increased by 4,096 B
for the added sampled IDs. `/tmp/e2d-objects-shapes.log` also records the subsequently corrected
old ShapeCast expectation, so that run alone is not a complete-suite pass.

The final shared-collider CPU suite and complete GPU suite both passed on this implementation:
`/tmp/e2d-objects-final-cpu.log` and `/tmp/e2d-objects-final-gpu.log`. The GPU suite also checks
compute lifetime with both renderers. Release runtime compilation, compiled API coverage,
wiki tests/generation/check, shader generation checks and changed-file whitespace checks passed.

Use ELECTRON2D_TEST_PHYSICS_OBJECTS=1 or ELECTRON2D_TEST_PHYSICS_OBJECTS_GPU=1 with the Release
RID test runner. Public independent-GPU binding, tile collision generation/lookup, picking,
backend extensions, networking, full CPU/GPU scene comparison and window acceptance remain
open. Native allocation and other platforms are not established by these checks.
