# Physics world activity decisions

Last updated: 2026-10-09

<a id="adr-0089"></a>
## ADR 0089: Global and local simulation activation

- Status: Accepted
- Scope: PhysicsServer activity policy, world-step integration and host scheduling
- Depends on: [0063](physics.md#adr-0063), [0070](physics.md#adr-0070), [0087](physics-joints.md#adr-0087), [0009](core-object-runtime.md#adr-0009), [0014](resources.md#adr-0014)

### Decision

- Expose PhysicsServer.SetActive(bool), SpaceSetActive(RID, bool) and SpaceIsActive(RID). The process server starts enabled. Newly created caller-owned spaces start inactive, matching the pinned server's [create/active-space contract](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp). SceneTree activates its lazily created world. This corrects the first host-step profile's implicit activation before public release; explicit simulation consumers must call SpaceSetActive(space, true).
- SpaceStep remains the explicit host operation for caller-owned worlds; SceneTree advances only its own world. Both enter the same PhysicsSpace.Step gate. Simulation requires local active=true, global active=true and a nonzero delta. Inactive intervals do not prepare/apply forces, run native solver/body callbacks, update contact/sensor snapshots or consume queued one-step force. They preserve native handles, body state, joints, cached direct views and previous interval data. Skipped time is not accumulated; reactivation integrates only the supplied current delta.
- Queries, configuration and resource cleanup remain available while inactive. Query preparation applies pending geometry/poses independently of simulation. Scene physics callbacks, timers/tweens, frame counters and input/render lanes continue according to existing SceneTree policy. SceneTree.Paused/process modes remain distinct: world suspension changes the solver lane, not scene scheduling.
- The global policy is an atomic process-wide value and can change on any thread, without touching native state. Each world samples it at its own interval boundary. A running interval completes, including its queued deliveries; a callback toggle affects later intervals/worlds. Local world reads/writes require that world's owner and reject while its solver is running. A post-solver body callback may change local policy for later intervals. Global suspension does not overwrite local flags.
- No public global activity getter or inactive compatibility state is added. No additional simulation loop, backlog timer or vendor change is introduced. ProcessInfo statistics remain a separate integration: publication over independently host-stepped worlds and active constraint-island/contact definitions need verified backend mapping before exposure.

### Verification and limits

PhysicsActivityTests checks default inactive/scene active, real motion and retained spring, direct queries/configuration during pause, pending force consumed once on resume, no elapsed-time catchup, local/global independence, any-thread global change, off-owner/local solver rejection, stale/wrong RIDs, scene callback/timer continuation and callback failure after committed solver/policy changes. Sixty-four warmed local/global policy cycles with skipped and active native frames allocate zero managed bytes on Linux/.NET 10. Native allocations, broad multiworld throughput, other platforms and owner visual acceptance remain unverified.

### World sleep settings

- Expose the three SpaceGet/SetBodyLinearVelocitySleepThreshold,
  SpaceGet/SetBodyAngularVelocitySleepThreshold and SpaceGet/SetBodyTimeToSleep
  operation pairs beside SpaceGet/SetConstraintDefaultBias. These are concrete typed
  branches of the pinned SpaceParameter dispatcher; remaining contact/iteration
  branches stay open. Public numeric selectors are unnecessary under ADR 0087.
- Sample typed Physics2DSleepThresholdLinear, Physics2DSleepThresholdAngular and
  Physics2DTimeBeforeSleep project settings at world creation, including feature
  overrides. Defaults are 2 scene units/s, 0.13962634 rad/s and 0.5 s. Existing worlds
  retain their values after project edits. CPU and independent GPU share one internal
  PhysicsSleepSettings authoring record and finite/nonnegative validation. Reject
  positive linear values that underflow the CPU unit conversion before mutation.
- Automatic dynamic sleep requires linear and absolute angular speed strictly below
  their separate thresholds for longer than the configured duration. This preserves
  the pinned [sleep test](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_body_2d.cpp).
  Shape radius does not change either threshold. Zero velocity thresholds disable
  automatic sleep; zero duration requires one eligible positive interval. Explicit
  sleep and CanSleep retain their existing policies; whole contact/joint components
  must be quiet. Backend correction motion can additionally defer sleep until the
  positional solve settles. This stability safeguard does not relax either speed
  threshold. Inactive and zero-time calls do not age timers.
- Equal writes preserve timers and sleep. Changed values wake current dynamic
  bodies and restart quiet timers; new attachments receive the current linear
  threshold. Reads and writes require the world owner outside solver execution.
  Post-solver callbacks may change settings for subsequent intervals; a throwing
  callback does not roll back its committed policy. World/body identity and borrowed
  view attachment versions remain unchanged.
- Independent GPU policy edits reuse ordered per-body wake commands, preserving
  last-write-wins explicit sleep/velocity/impulse semantics and making pending wakes
  visible to zero-time reads. Only authored policy changes generate these O(body
  count) commands; unchanged frames retain resident sleep/graph state. This is
  metadata journaling, not a CPU pose/velocity mirror. Its transfer cost is measured
  by PhysicsSleepPolicyTests; public independent-GPU world binding remains open.

PhysicsSleepPolicyTests checks project capture, separate speed/size behavior,
threshold and duration equality, zero values, invalid atomic writes, idle worlds,
owner/solver guards, post-solver failure, scene events, live view lifetime and warmed
allocation. Existing GPUPhysicsSleepStoreTests retains connected contact/joint
sleep, support-removal wake, generation and explicit-command coverage. See the
[component report](../components/physics-sleep.md).
