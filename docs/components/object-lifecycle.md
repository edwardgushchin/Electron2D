# Object lifecycle component

Last updated: 2026-10-27

[Object lifetime monitors](object-lifetime-monitors.md) share the base construction identity without owning objects or changing disposal/factory provenance.

## Scope

This Core component defines identity, diagnostics, notifications, translation access, validity, and deterministic cleanup shared by engine-owned objects.

## Owned types

| Type | Source | Role |
| --- | --- | --- |
| [`ElectronObject`](../classes/ElectronObject.md) | [`ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs) | Abstract base for engine objects |
| [`WeakRef<T>`](../classes/WeakRef.Generic.md) | [`WeakRef.cs`](../../src/Core/Object/WeakRef.cs) | Typed non-owning reference to a live engine object |

## Current implementation status

Implemented and covered by the executable checks. The component includes notification dispatch, typed property-list/script-change hooks, and disposal precondition validation in addition to identity and lifetime.

## Dependencies

The component uses .NET threading and exception-dispatch primitives. Its `Tr`/`TrN` entry points call the [Localization component](localization.md); it has no SDL3-CS dependency.

## Runtime behavior

Each instance receives a non-zero process-local `ulong InstanceID` from a static counter using `Interlocked.Increment`. `ClassName` exposes `GetType().Name`; `ToString()` returns `<ClassName>#<InstanceID>`. `Notify(int)` dispatches to `OnNotification(int)` with ordinary direction. `Notify(int, bool reversed)` selects derived-first delivery; [ElectronObject.Notifications.cs](../../src/Core/Object/ElectronObject.Notifications.cs) owns its thread-local invocation context. IDs 0 and 1 are reserved for post-initialization and pre-delete notifications. Disposal sends pre-delete with reversed direction before resource cleanup; post-initialization 0 is not automatically emitted.

Lifetime follows this state transition:

```text
Alive --Dispose()--> PreDelete --accepted--> Disposing --Dispose(bool) succeeds--> Disposed
                         |                                      |
                  CancelDisposal                                Disposed event
                         |
                       Alive
```

Before attempting the atomic transition, each caller may run `ValidateDisposal()` so a derived type can reject that caller. `Node`, `MainLoop`, and `SceneTree` use it to enforce owner-thread or active-callback disposal rules. Concurrent callers can validate at the same time or race with another caller beginning disposal, so overrides must be side-effect-free and race-tolerant.

The first valid caller atomically changes the state from `Alive` to automatic pre-delete and records its thread. Concurrent and reentrant `Dispose()` calls return during an active attempt. Terminal calls return without repeating cleanup; a vetoed attempt may be retried later. `IsDisposed` becomes `true` when disposal starts. Guarded state remains readable through `ThrowIfDisposed()` only on the recorded thread while it runs pre-delete and derived cleanup callbacks; other threads are rejected. Without a veto, the base reaches `Disposed`, clears exactly its own `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. `Disposed` is raised once only when both pre-delete notification and `Dispose(bool)` succeed.

`PropertyListChanged` and `ScriptChanged` are synchronous typed C# events raised through protected methods. Trusted file replacement and prepared resource remapping raise source-change notifications through these methods after committing a different source and its ownership. [Source-publication ordering and failure rules](scripting.md#source-publication-notifications) apply; live SetScript remains open.

The separate [Typed event connections](event-connections.md) component can own, defer, or consume subscriptions to these events without changing their declarations.

`WeakRef<T>` uses a managed weak reference and returns the original target only while it is reachable and is neither in an active disposal attempt nor terminally disposed. Constructing it with null or a disposed target leaves it empty. Disposing the wrapper leaves the target untouched; disposing the target invalidates `GetRef()` immediately, even if the target is still strongly held elsewhere. The [typed weak-reference decision](../decisions/core-object-runtime.md#adr-0050) defines this lifetime boundary.

A failure in notification or cleanup is rethrown after state publication; simultaneous failures become `AggregateException`. A derived type controls when its own event lists are cleared and may leave derived cleanup incomplete if its override throws.

`ElectronObject.IsQueuedForDeletion` is a nonvirtual atomic observation inherited by every object role. SceneTree counts independent explicit requests until execution or discard; Node contributes its cancellable generation through claimed execution. Cancellation affects only Node work, and queued parents do not mark children. The observation stays true during pre-delete and cleanup, then reads false after terminal disposal. Direct disposal without a queue stays false. Readonly nonstored descriptor discovery adds no serialized state or revert value, scheduling API, numerical mirror or backend wait. The base lifetime state machine and its validation remain unchanged.

`CancelDisposal()` is a nonvirtual helper for the exact automatic pre-delete thread. It marks that attempt canceled without publishing live state during notification. On return, a canceled attempt restores Alive and its weak identity, retains subscribers/resources and skips all derived cleanup. Notification errors still propagate after restoration. Manual/outside/foreign calls do not arm cancellation, and committed cleanup cannot be canceled. Ordinary non-vetoed attempts keep the prior disposal order and errors. The state fits the existing lifetime word and adds no object field, backend read, native fence or managed allocation.

Weak identity stores distinguish temporary pre-delete closure from committed cleanup. Read-side unavailability never retires a veto-capable target. Sweep/registration cleanup retains physical/rendering RIDs, ResourcePath ownership, owner reverse records and unique namespaces, group membership and writer registrations until commit or collection. Name/path reservations stay occupied during notification; ordinary live lookup still hides the closed target. Registry probes cover in-notification lookups, 256-registration sweeps, restoration and terminal release.

## Custom typed iteration

Object iteration uses the existing C# compiler/BCL protocol under ADR 0005. A concrete compiled object or scene/resource subtype supplies its own typed enumerator. The first `MoveNext` combines initialization/end detection, later calls advance, and `Current` reads without advancing. Per-cursor state preserves independent/nested loops. No second engine value system, universal enumerable base, cursor field or dynamic method registry is added.

`ObjectIterationTests` is shared with the public-only registered-backend consumer. It checks empty/repeated/nested/explicit cursors, typed object identity, BCL interface consumers, early break, construction/move/current/cleanup/body errors and caller-owned lifetime guards. Real force and state callbacks enumerate a physical-body subtype and a borrowed typed owner during complete moving frames. All 18 final profiles pass: six managed and six strict NativeAOT public-consumer profiles, both complete default consumer regressions, and four CPU/GPU owner/separate root profiles. Sixteen 128-frame brackets measure 2048 complete moving intervals at 0/0 owner/all-thread managed bytes. Seven exact portable-PDB proofs match 2273 runtime, 166 consumer and 756 root documents; 3189 unique authored inputs are retained. Evidence: bin/physics-object-iteration/2026-10-18/validation.json; SHA-256 0c8e5b533fc235f2b1aced4cbed5b6536f10af6ab3b3f27a546f6b83273b7e6a. Initial fixture errors (invalid Action expression and an incorrect assumption of unwrapped callback errors) and a rejected wrapper selecting the older host are retained separately; they are not numerical runtime failures or accepted final evidence. This verifies the exercised C# projection; user collection concurrency and arbitrary cursor implementations retain their own policies.

## Notification direction

The [compiled construction boundary](#compiled-construction-notification-boundary)
clarifies why identity 0 is not an automatic project callback. Ordinary/reversed
delivery and the lifecycle checks below keep their existing behavior.

All 95 built-in overrides preserve their original normal base-call points, early returns and local exception aggregation. Reversed delivery completes local handling before forwarding to the base. A direct local function retains early-return boundaries where needed; handlers without such exits keep their ordinary method body. No reflection, dynamic code, callback registry or per-call delegate is added. Custom C# overrides use protected `IsNotificationReversed` and explicit base calls, as shown in the [class reference](../classes/ElectronObject.md#p-electron2d-electronobject-isnotificationreversed). Intentionally suppressing the base retains ordinary C# meaning.

Direction belongs to the current object, invocation and thread. Nested normal/reversed calls, calls on another object and failures restore the outer context; asynchronous work does not inherit it. An unhandled error stops unvisited ancestors without replacing the original exception. Existing handler-local aggregation remains intact. Numeric notifications remain independent of ordinary signal blocking, and direction does not relax disposal, owner-thread, solver or borrowed-state guards. Completed calls retain no target in thread-local state.

`tests/ObjectNotifications.Consumer` uses only public/protected runtime API and shares its checks with the root suite. It verifies custom two-level RigidBody ordering, the actual Node physics callback, Label invalidation versus Control.Resized, nested/concurrent/failing calls, cancelled/terminal pre-delete and weak collection. Complete physical frames alternate normal/reversed velocity mutations while preserving RID and advancing actual CPU/GPU motion. The fixture compares velocity and displacement within 0.0001 scene units, covering float accumulation over the short measured interval; identity, order, counts and allocation gates are exact. Eight public CPU/no-device and NVIDIA GPU owner/separate CLR/strict full-trim NativeAOT profiles pass, each with 128 active and 128 idle measured full frames at strict 0/0 owner/all-thread managed bytes (2048 total). Both GPU runtimes map the NVIDIA driver without software Vulkan; no new kernel Xid or payload mutation is observed. Evidence: `bin/physics-notification-direction/2026-10-26/`. Native heap, foreign platforms, full-suite/numerical/network/performance and human acceptance remain separate.

### Directional qualification on 2026-10-26

The final cohort has 19 successful immutable-payload processes: eight public CLR/strict NativeAOT backend/thread combinations, two root directional checks and nine existing loop, CPU/GPU scene physics, camera/navigation, button, viewport/input, animation, particle and audio regressions. The ten directional runs retain 128 active plus 128 idle measured frames each (2560 total), all at strict 0/0 owner/all-thread managed bytes. Existing normal-path physics regressions retain their numerical/contact/lifetime/allocation assertions. Five assembly/PDB identity and SHA checks match 2611 runtime, two public-consumer and 981 root authored source documents, with the CLR runtime identical in public/root payloads.

Receipts: `bin/physics-notification-direction/2026-10-26/validation.json` and `source-proof-final.json`. Runtime SHA-256: `392723d715291fd03253f5839881a3597a06e1221501e7d6c8ab145882023286`; public consumer: `8a4e0b1ae2c72778ddd9de8c679ef49a0cbf6c7466f523ce05b1dcfd5a1036fa`; root tests: `e0936520afb6e5e5eb0285db4516f94f27f00268bc74cdc040e4cc41dd94eb61`; NativeAOT executable: `cc19d8e297ba72894787b07ab24de0f0b1c8e4e88e2f0b895bf2cfb5032ce865`. Initial consumer compile mistakes, the earlier active-only cohort and the coverage test's stale Partial expectation are retained separately; the final cohort includes idle work and keeps automatic post-initialization explicitly Partial. No complete-suite, native-heap, foreign-platform, full numerical/network/performance or human acceptance is claimed.

## Compiled construction notification boundary

`NotificationPostInitialize` identifies the internal initialization boundary before
project code is attached. It is not an automatic post-constructor invocation of
compiled `OnNotification` overrides. Ordinary C# constructors establish engine
state without calling those overrides. Direct Node/Resource construction,
Node.Duplicate and PackedScene.Instantiate keep this boundary. An explicit
`Notify(0)` after construction remains supported, including reversed dispatch,
the original callback exception, context restoration and unchanged instance ID.

The pinned [Object.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Object.xml)
places identity 0 before script attachment. The
[C# construction bridge](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs)
requests native post-initialization before `TieManagedToUnmanaged`; the
[native dispatcher](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/object.cpp)
only forwards to a script when one is attached. The earlier callback audit
incorrectly required a later project-visible initialization event. This corrects
that requirement under [ADR 0005](../decisions/core-object-runtime.md#adr-0005);
it adds no constructor-time virtual call or implicit factory callback. Script
`_init`, live attachment/replacement and extension reload remain separate missing
capabilities under ADR 0091.

NotificationChecks now exercises direct construction, exact project-type
duplication, in-memory PackedScene construction and explicit zero callback errors
alongside its existing directional, concurrent, lifetime, GUI and full-frame
physics checks. A constructor-complete sentinel rejects premature virtual calls.
These checks do not establish live script attachment, hot reload or full physics
acceptance.

The source-bound 2026-10-27 cohort passes eight public CLR/strict NativeAOT
CPU/GPU owner/separate profiles and two root profiles. Each executes construction
checks plus 128 active and 128 idle measured complete physical frames: 2,560
frames at strict 0/0 owner/all-thread managed bytes. GPU maps confirm NVIDIA
without software Vulkan; CPU processes forbid GPU discovery and load no GPU
driver, and native processes have no CoreCLR mapping. PDB checks match 2,684
runtime, two public-consumer and 1,014 root authored C# documents; native compiler
inputs remain unchanged during publication. The initial static-lambda fixture
was rejected by the existing no-captured-target factory guard and was corrected
to a named static method, without changing that guard. Receipts and pinned source
hashes are under `bin/object-notification-contract/2026-10-27/`.

Only the `_notification` callback row changes to Implemented. Physics declarations
are **1359/1369 (99.27%)**, with **10 open** and 92 Excluded. Class-wide script
requirements, HDR, remaining diagnostics, the earlier native allocation
observations and complete numerical/network/platform/performance acceptance stay
open.

## Threading

- Instance ID allocation is safe under concurrent construction.
- Entry into disposal is safe under concurrent `Dispose()` calls and cleanup runs at most once.
- `IsDisposed` uses volatile reads.
- The disposing-thread allowance supports teardown inspection only; arbitrary re-entrant mutation is not promised safe.
- The component does not make members introduced by derived classes thread-safe.
- Concurrent event subscription and disposal have ordinary C# event semantics; no stronger ordering guarantee is provided.

## Ownership rules

- Derived types release owned managed resources from `Dispose(bool)` when `disposing` is `true` and then call the base implementation.
- Types owning native SDL resources must wrap native handles in `SafeHandle`-derived types.
- `ElectronObject` has no finalizer because it directly owns no unmanaged handle.

## Exclusions

The component does not provide object lookup, public manual reference counting, weak-event infrastructure, a service container, metadata, script attachment/runtime, dynamic method calls, or string-addressed signals. A future Resources-domain asset manager may count internal disposable leases to control shared native payload retention without changing `ElectronObject` lifetime. Deferred execution belongs to `SceneTree`.

## Verification

The executable check in `tests/Electron2D.Tests/Program.cs` covers:

- 10,000 parallel constructions with unique non-zero IDs;
- runtime class name and diagnostic string;
- 1,000 concurrent calls to `Dispose()`;
- one derived cleanup and one `Disposed` event;
- guarded state access during pre-delete on the disposing thread;
- `ObjectDisposedException` after cleanup through `ThrowIfDisposed()`.
- arbitrary and pre-delete notification delivery.
- property-list and script-change notifier delivery.
- per-object translation flags and domains through the Localization domain.
- disposal precondition rejection before the disposed state begins, through Scene tests.

The current directional qualification uses .NET SDK 10.0.101 targeting .NET 10; these focused runs do not establish a complete-suite pass.

Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.

## Per-object ordinary event blocking

The inherited [ElectronObject delivery policy](../classes/ElectronObject.md#ordinary-event-delivery) applies at publication, including direct C# subscribers and EventConnection tokens. A blocked publisher drops new ordinary emissions without removing listeners, consuming one-shot tokens or submitting deferred calls. Existing accepted deferred work retains its usual cancellation rules. Each emission selects once; changing the flag inside a handler affects the next emission. Unblocking does not replay suppressed transitions.

Numeric notifications, virtual callbacks, operation callbacks, committed state and resource revisions remain active. Deterministic Disposed cleanup remains independent. Physical handler buffers select an empty snapshot while blocked, preserving existing payloads, per-handler eligibility, exception policy and subscription capacity. Resource geometry/material polling still observes committed edits. Static facades use their retained service object's policy.

## Public property-list notification

ElectronObject.NotifyPropertyListChanged is public for every inherited node, body and resource. It synchronously publishes PropertyListChanged on the caller thread with the existing typed sender, ordinary multicast subscription/error behavior and signal blocking. It does not mutate properties, advance physics, wake bodies or emit Resource.Changed. Observers can request a fresh validated GetPropertyList snapshot without changing an earlier snapshot.

A notification grants no additional access to a subscriber. Exact teardown-thread inspection remains available before cleanup; completed disposal and foreign teardown-thread access reject. Owned one-shot/deferred EventConnection subscriptions retain accepted deliveries when later emissions are blocked. The complete ScriptChanged assignment/detachment contract retains its separate live-script binding prerequisite.

BackendChecks.RunPropertyList exercises public calls on Node, RigidBody, Area, CircleShape and PhysicsMaterial, synchronous thread/sender identity, blocking, duplicate subscriptions, deferred one-shot delivery, dynamic descriptor discovery, original callback errors, teardown and actual grouped physical nodes. Warm measurements include metadata emissions, a numerical step and a real moving-collider point query. A body at one scene unit per second with zero damping must travel 1.28 units during 128 steps of 0.01 seconds, within 0.001 scene unit; the bound permits accumulated float rounding while detecting a lost or overwritten velocity. Evidence: bin/physics-main-acceptance/2026-10-18/a3fff00e/publisher-validation.json. Numerical/native/foreign and whole-goal acceptance remain separate.

## Compiled source observation

[ElectronObject.GetScript](../classes/ElectronObject.md#m-electron2d-electronobject-getscript) exposes the borrowed source selected by actual typed construction. The owner releases its reference after committed disposal and retains it through veto; it never disposes the asset. Source text edits and explicit asset disposal leave compiled execution intact. Copy, archive, cache and scene-local rules belong to [the scripting component](scripting.md#compiled-owner-source-identity). Initial association adds no synthetic signal. [Source publication](scripting.md#source-publication-notifications) reports subsequent file/remapping changes; compiled initialization uses ordinary project constructors. Live reassignment remains open.
