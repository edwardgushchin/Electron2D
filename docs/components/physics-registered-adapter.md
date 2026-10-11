# Registered numerical solver adapter

Last updated: 2026-10-11

## Scope and ownership

The internal [RegisteredPhysicsWorldBackend](../classes/RegisteredPhysicsWorldBackend.md) connects the complete [PhysicsServerBackend](../classes/PhysicsServerBackend.md) to shared scene/raw identity, authoring, views, callbacks, monitors and history. The public contract has numerical [collider](../classes/PhysicsBackendCollider.md) and [joint](../classes/PhysicsBackendJoint.md) attachments and typed observation records. It carries no PhysicsSpace, PhysicsBodyRuntime, PhysicsColliderBackend or scene storage through implementation operation parameters.

## Runtime flow

A fresh claimed implementation initializes before activation. Attachment creation receives engine-assigned RIDs. Shared preparation supplies complete authored integration/force/Area policy and logical shape slots. The implementation solves its own state; common completion publishes scene/raw results, contact/Area observations, statistics and callbacks. Ordinary direct views dispatch through the same selected attachment. Motion output uses the reusable public result construction family and existing completed-output validation.

Each user implementation call borrows owner-thread lifetime. Factories cannot reuse one world, collider, joint or history object. Captured numerical output validates before consumption. Failed contact publication retains the receiver snapshot and aggregates errors after later deliveries. Started solve/synchronization failure retires the world without hidden CPU/GPU replay. Cleanup attempts Finish after all attachments, including common-space retirement and partial creation failures.

Same-backend private history stays with the implementation. Portable state transfers engine-authored bounded values; local one-way RIDs map to network IDs only in the common adapter. Explicit and joint exceptions are supplied as current-world typed RID pairs. Observer metadata and identities remain engine sampled.

## Verification boundary

The separate [public-only consumer](../../tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj) implements a bounded circle/spring model, with actual integration, queries, overlap and contact output and spring response. It is not a production physics option. The fixture runs 64 warmup and 64 measured complete PhysicsFrame cycles, including raw state callbacks, and measures zero owner/all-thread managed bytes. Scene/raw mass2/4 and constant forces12/24 both yield .1 scene units/s after the first 1/60-second interval within .000001 float tolerance; retained checkpoint and portable poses compare exactly. Contact/Area events, real circle queries, bounded spring response, receiver-failure continuation, reuse rejection and terminal solve failure cover their named paths. The fixture proves these exercised adapter paths. All currently applicable built-in shape/body/Area/joint behavior still requires its existing shared CPU/GPU checks.

PhysicsServerManager.RegisterServer and SetDefaultServer now execute through a retained static facade. Factories return fresh PhysicsServerBackend objects, typically derived from PhysicsServerExtension. Duplicate/reserved names, null/disposed/reused implementations and recursive creation reject. DEFAULT uses strictly greater priorities; World(string) captures its selection before lazy startup, while SpaceCreate(string) initializes an owned inactive space immediately. Resource aliases retain one runtime. Requested and actual names accompany Backend.Extension; explicit CPU/GPU and parameterless CPU remain independent of the default. Factories execute outside the lock, including registration from another thread. World release calls Finish and disposes the implementation; direct caller disposal of a bound implementation rejects.

The public immutable configuration/result family includes body integration/forces, Area fields, joints, sleep/contact policy, logical shape slots, resolved fields, mass properties, overlap/shape contacts, statistics and one-way episodes. Collider and joint objects are fresh per attachment; checkpoint objects provide private history hooks. Query exclusion spans are passed explicitly and borrowed only during the synchronous call. The consumer compiles without internal access and executes both SceneTree frames and explicitly owned SpaceStep.

Arbitrary custom geometry and every extension operation's acceptance remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103). Backend rows stay Partial or Blocked where their named path is absent; the bounded test solver does not certify the full production contract. Native allocator, cross-platform, real-rendered output and final performance acceptance are unverified here.

Initialization/Finish/disposal failure injection retains all three errors after retirement. Scene diagnostic capture executes the selected ReadDebugContacts hook after synchronization; invalid count or nonfinite points reject before diagnostic publication and fail the started interval.

Status and capacity hooks also borrow the owner context; recursive world inspection from the status hook rejects and a later normal query recovers. The separate consumer checks this guard.

[Custom geometry](physics-custom-geometry.md) now executes with an external annulus/circle implementation: canonical data, exact mass, standalone/selected queries, body contacts, Area observations, server mutation and private/portable replay. Generic custom format file codecs/editor authoring, unexercised extension operations, native/foreign acceptance and final performance remain open.

Portable one-way capture is now bounded by the current declared count, independently of retained array capacity. Shared validation rejects self/unbound/disabled/invalid-piece/duplicate episodes before encoding. The public-only consumer restores a rejected episode into a separately created explicit world with different local RIDs and verifies pass-through at 60 units/second versus a real impulse to 30 for the accepted state. Repeated restore reinstates the decision; 64 warmed capture/apply cycles allocate zero managed bytes.


## Initial collider metadata

The adapter publishes retained object/canvas IDs and collision priority immediately
after successful numerical Attach and before logical shapes are prepared. Earlier
code sent only live edits, so a registered solver's first query could miss a canvas
association authored while detached. Reattachment to another selected world now
receives current values through the same hooks. Body and Area identity share this
protocol; scene CanvasLayer entry/exit retains its ordinary live updates.

`BackendChecks.IdentityChecks` in the public-only consumer detects the prior bug
before any physics tick. It exercises body/Area detached/live/clear assignment,
weak sampled object identity, exact 64-bit point membership, two-world transfer,
scene body/Area hooks, failed initial identity/priority hooks with retry, owner and
borrow guards, and zero owner/all-thread managed allocation over 128 edited queries
after 64 warmup iterations. Allocation counters are initialized before warmup;
the first uninitialized-counter measurement reported 0/1,008 owner/all-thread
bytes, while initialized counters followed by warmup produced 0/0 with the same
queries. Zero-byte assertions remain exact. Queries use the external circle model,
not a built-in solver delegate.

Coverage pairs all ten reference object/canvas/priority operations with their typed
attachment/facade projections under ADR 0103. These rows remain Partial: identity
publication is verified, while complete extension-operation acceptance and general
third-party numerical priority response remain open. No runtime declaration or
public singleton instance accessor is added. Native allocator, other platforms,
final performance and the separately recorded sporadic GPU window failure remain
unverified here.


The initial-hook reentry check also exposed missing guards: an identity hook could
recursively assign canvas metadata, and a not-yet-committed raw attachment had a
world reference before its public SpaceRID was assigned. Shared collider metadata
writes now reject a borrowed extension scope before assignment; raw lifetime
preflight uses the live world reference to reject release/transfer during that
scope. Both body and Area initial hooks exercise these paths. Typed immutable
result construction and supported nested query scratch keep their existing paths.
