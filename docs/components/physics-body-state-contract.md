# Direct-body state contract audit

Last updated: 2026-10-25

This audit covers PhysicsDirectBodyState and PhysicsDirectBodyStateExtension as complete declared type families. The pinned input at `ed1daf0bf001b61586d9930840f2f1394092c079` contains 28 base methods, 14 base properties and 48 extension operations, without deprecated declarations. General inherited ElectronObject gaps retain their own declaring coverage rows. Type completion does not certify the complete physics goal or an arbitrary external solver.

ADRs 0070/0103 retain one live view per body attachment and a separate caller-created extension role. The compiled API, class pages, selected factories and public-only consumers supply the reverse audit. The 48 reference extension operations map to 38 typed hooks: eleven contact value/object operations share one validated immutable PhysicsBodyContact hook. Contact count retains its own hook. No field is omitted by this projection.

| Declared family | Public contract | Extension projection |
| --- | --- | --- |
| AngularVelocity, LinearVelocity | Finite current selected motion; assignment wakes dynamic bodies; static/kinematic surface channels retain their role | Get/SetAngularVelocityCore, Get/SetLinearVelocityCore |
| CenterOfMass, CenterOfMassLocal | Origin-relative mass offsets in global axes and body-local axes respectively | GetCenterOfMassCore, GetCenterOfMassLocalCore |
| InverseMass, InverseInertia | Reciprocal engine-unit dynamic mass/inertia; zero for nondynamic or rotation-locked roles | GetInverseMassCore, GetInverseInertiaCore |
| Sleeping, Step | Dynamic sleep/wake policy and last nonzero outer duration; zero duration before the first step | IsSleepingCore, SetSleepStateCore, GetStepCore |
| TotalGravity, TotalLinearDamp, TotalAngularDamp | Last resolved body/Area/world fields; finite signed damping remains permitted | GetTotalGravityCore, GetTotalLinearDampCore, GetTotalAngularDampCore |
| CollisionLayer, CollisionMask, Transform | All 32 filter bits; finite rigid global pose and existing kinematic target policy | Get/SetCollisionLayerCore, Get/SetCollisionMaskCore, Get/SetTransformCore |
| Get/SetConstantForce, Get/SetConstantTorque, AddConstantCentralForce, AddConstantForce, AddConstantTorque | Shared persistent authoring, positioned moments relative to the actual mass center, next-interval force duration | Corresponding seven constant-force/torque hook operations |
| ApplyCentralForce, ApplyForce, ApplyTorque, ApplyCentralImpulse, ApplyImpulse, ApplyTorqueImpulse | One-interval accumulators versus instantaneous mass/inertia-scaled impulse; finite positioned input | Corresponding six apply hook operations |
| GetContactCount and eleven contact value/object methods | Bounded completed contacts: physical RID, sampled association ID/object, both logical shape indices, global points/normal/point velocities and full normal/tangent impulse | GetContactCountCore and GetContactCore |
| GetSpaceState, GetVelocityAtLocalPosition, IntegrateForces | Matching live world; origin-relative global-axis point offset; explicit gravity then damping for each call, without consuming forces | GetSpaceStateCore, GetVelocityAtLocalPositionCore, IntegrateForcesCore |

The nongeneric contact-object convenience method retains its physical scene CollisionObject role. The generic method returns the sampled weak association, including a TileMapLayer or an assigned raw-body object. Collider RID and association ID are separate identities. Released objects expire without invalidating sampled contact values; attachment retirement permanently invalidates the view itself.

## State, publication and lifetime

Owner, world failure, solver phase, disposal and attachment generation checks precede implementation dispatch. Pending mode/pose/motion/mass publication is prepared through the common body path. Equal writes do not bypass numerical borrowing guards. Nested extension calls borrow the same attachment until finally releases their context. Wrong-owner access, recursive stepping, checkpoints and attachment/body/world release reject while borrowed; ordinary post-solver integration callbacks keep their documented writes.

Selected factories validate world/body/generation identity before publishing cached views. Failed construction publishes no view or tick; a later retry creates one current view. Consumer disposal permits cache recreation. Detach/transfer/reentry never revives an old view, while independent extension binding does not reopen or replace the cache.

Cached factory contacts publish after numerical/worker completion on the owner, before monitors and gameplay callbacks. Values remain immutable through later authoring and association edits. An invalid projected count/contact or user exception preserves the previous receiver snapshot while the committed interval and other receivers continue. Caller-created views invoke their contact hooks directly. Contact-cap assignment clears the cached count immediately, including equal assignment; depth policy affects the next publication. Numerical terminal failures keep their separate failed-world policy without replay.

## Executable qualification

The standalone [body extension consumer](../../tests/PhysicsBodyExtension.Consumer/Program.cs) executes all 38 hooks, doubles a real impulse, checks complete scene contacts, validates bad scalar/vector/transform/count/space/identity results and rejects borrowed structural operations. Transfer, same-world reentry, collider release, weak identity, retired contact generation, wrong thread and failed GPU world have explicit controls.

[BodyFrameChecks](../../tests/PhysicsBodyExtension.Consumer/BodyFrameChecks.cs) adds 128 changing complete frames after 128 warm frames, separately for scene/raw bodies. Every frame edits pose, motion, angular speed, filters, persistent forces/torque and nonzero accumulators/impulses, then solves, dispatches four owner callbacks and refreshes both complete contact samples. Both bodies execute all 38 hooks. The measured interval contains 512 callbacks and 256 contact projections at strict zero owner/all-thread managed bytes. The momentum residual includes all applied force and reported impulse, with a .02 scene-unit impulse bound; identities and sampled values are exact. This permits float/solver accumulation error without allowing a missing physical impulse.

The initial new fixture repeatedly measured 0/1008 bytes because its second forced collection did not wait for finalizers. Initializing the counters earlier did not resolve it. Waiting after that collection did; the consumer now reuses AllocationWarmupCheck.Collect for both brackets. The measured work, 128 frames and zero-byte threshold are unchanged. This is measurement preparation, not a change to numerical behavior.

The selected public backend consumer checks factory cache identity/failure/retry, all four body roles, asymmetric compound centers/point velocity and completed transient/capped contact impulses on CPU, NVIDIA GPU and the independent registered implementation. The root body-contract selector exercises the complete shared body-state, force, contact-impulse and report-only suites. Ordinary and separate numerical modes remain distinct profiles; numerical state is never inferred from a facade-only round trip.

Evidence is under `bin/physics-body-contract/2026-10-25/`: pinned metadata, exact commands, source/payload manifests, CLR/strict NativeAOT summaries and per-process receipts. GPU processes must map NVIDIA and reject software ICDs; CPU processes run with dummy video and unavailable Vulkan ICDs. NativeAOT processes reject CoreCLR mappings. All 56 profiles passed: 38 CLR and 18 strict NativeAOT, including CPU/GPU tile owners, cached contact publication and selected ownership/failure checks. Final binary identities and all receipts are retained in `qualification.json`.

Only the two type rows close in this audit; their 90 previously implemented member rows are not counted again. Wider inherited API, full-suite qualification, cross-process networking, native allocation, foreign platforms, target mass-scene advantage and real-window FPS retain their own open gates. These functional two-body frames are not a mass-performance result.
