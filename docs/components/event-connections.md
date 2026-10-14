# Typed event connections component

Last updated: 2026-10-14

## Scope

This Core component adds deterministic connection ownership, one-shot consumption, and deferred scheduling to ordinary typed C# events. It does not replace those events with a dynamic signal bus.

## Owned type

| Type | Role |
| --- | --- |
| [`EventConnection`](../classes/EventConnection.md) | Disposable wrapper around one typed event subscription |

## Runtime flow

`EventConnection.Subscribe` receives typed add/remove accessors and a matching handler. It creates one wrapper, records how to remove that wrapper, attaches it, and returns the ownership token. With no scheduler, accepted emissions invoke synchronously. With a scheduler, each accepted emission captures its typed arguments and submits an action for later execution.

One-shot connections atomically stop accepting emissions and detach before invoking or scheduling the first handler. A pending deferred one-shot can be cancelled by disposing its token. Reusable deferred connections can queue multiple emissions; disposal makes queued callbacks inert until the scheduler discards them through execution.

## Dependencies

The component depends only on .NET delegates, `IDisposable`, locking, and exception-dispatch support. It has no runtime dependency on Scene, SDL3-CS, serialization, reflection, or an external reactive library. `SceneTree.Defer` is the standard current scheduler supplied by callers needing frame-boundary delivery.

## Invariants

- The same wrapper delegate is supplied to both event accessors.
- A one-shot connection accepts at most one emission under recursion and concurrency.
- User accessors and handlers never run while the connection-state lock is held.
- Disposal is idempotent and terminal even when removal throws.
- Terminal transitions clear retained accessor, handler, and scheduler references.
- Deferred argument values are captured when the event is emitted.
- Disposal cancels scheduled callbacks that have not started; it does not wait for running callbacks.
- Source identity and payload shape remain explicit in the event's typed signature.

## Current implementation status

Implemented and covered by executable positive, negative, failure-injection, re-entrancy, and concurrency checks for the zero- through four-argument overloads.

## Exclusions

There is no dynamic event lookup, generic event registry, untyped argument list, weak subscription, priority ordering or automatic subscriber-lifetime discovery for ordinary tokens. Persistent authored scene subscriptions use the separate [SceneConnectionBinding](../classes/SceneConnectionBinding.md) component under [ADR 0010](../decisions/core-object-runtime.md#adr-0010): compiled factories and typed endpoints retain cleanup and packed storage. Ordinary runtime subscriptions are still omitted from scene packing.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies ordinary and one-shot connections, recursive/concurrent emission, deferred ordering and cancellation, exception policy, accessor rollback, removal failures, scheduler failures, null validation, native duplicate handler behavior, and sender-first two-argument preservation.

Prepared reusable synchronous subscriptions with zero through four arguments create no per-emission capture. Prepared standard-scene delivery uses typed invocation slots; arbitrary custom schedulers retain their independent argument capture paths. No zero-allocation claim is made for subscription/accessor work or allocating user handlers. This preserves argument values, lock boundaries, detach-before-handler ordering, cancellation and exception policy under ADR 0010.

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) verifies all five prepared immediate arities and full selected CPU/GPU frames: 128 warm/128 measured frames, strict zero owner/all-thread managed bytes, 128 emissions of each arity and 64 Area body enters/exits. Identity, disposed subscriptions and collected callback-error recovery remain observable. Cold two-argument deferred/one-shot checks cover value capture, ordering, cancellation and recursion; the existing EventConnection suite retains concurrency, accessor rollback/removal/scheduler errors and all overloads. GPU execution uses CPU-only Vulkan; hardware/native/network/performance acceptance stays open.

[Extended event arities](../classes/EventConnection.md) cover current 3-argument picking and 4-argument body/Area shape signatures under ADR 0010 without altering event payloads. The shared physics fixture now retains 128 measured emissions of each arity, 64 body and 64 shape enters/exits and exact RID/owner/disabled-first shape indices at strict 0/0 warm bytes on selected CPU/GPU serial/separate profiles. Deferred/one-shot calls retain their separate capture allocation paths.

Prepared reusable deferred zero-argument subscriptions now retain one immutable callback in the token and submit it for every emission. Each submission remains a separate queue entry; multiplicity, cancellation, scheduler errors and handler state checks are unchanged. There is no shared mutable argument slot or coalescing. With prepared SceneTree queue capacity and nonallocating handlers, the full physical frame retains zero owner/all-thread managed bytes. Custom-scheduler payload capture, subscription and first capacity growth remain separate allocating paths.

The shared EventConnectionPhysicsTests now verifies a three-emission deferred batch, 128 warm/128 measured prepared emit/drain cycles, pending cancellation and 128 real SceneTree.Defer callbacks inside measured CPU/GPU physical frames, alongside all five immediate arities, Area body/shape transitions and picking/lifetime/error cases. GPU profiles use CPU-only Vulkan; no hardware performance/native/platform acceptance is inferred.

The exact SceneTree.Defer scheduler and SceneTree.SetDeferred now use a Scene-owned typed invocation provider. Each accepted call retains independent typed values in a slot and submits its immutable callback into the existing single FIFO queue. Slots recycle only after callback completion or discard, clearing argument/callback references even after failure. Work emitted during a captured batch waits for the next flush. Pool operations share the lifetime/work gate; user handlers run outside it. A weak-owner Core registry recognizes only the exact registered delegate, so Core has no Scene runtime dependency. Public signatures, borrowed reference ownership, event multiplicity and cancellation remain unchanged.

Prepared standard-scene delivery and typed setters have zero invocation allocation, including prepared one-shot first delivery. First type/capacity growth, subscription and allocations performed by handlers/accessors remain separate; native multicast removal with multiple listeners can allocate. Arbitrary custom schedulers preserve their independent captured Actions and may allocate. Deferred delivery never extends a borrowed input event lifetime.

EventConnectionPhysicsTests verifies mixed typed/untyped FIFO order, next-batch work, failures followed by later work, closed-scheduler subscription/emission policy, 256 concurrent producers, reference release on execution/failure/finalization/activation rollback, and 640 first one-shot callbacks for immediate and scene-deferred profiles with singleton accessors. Selected CPU/GPU full frames retain 128 callbacks per immediate and deferred arity plus body/shape transitions at strict zero owner/all-thread bytes. Real picking and legacy custom-scheduler/concurrency/error checks remain included. Software Vulkan is functional evidence; hardware/native/foreign/network/performance acceptance stays open.
