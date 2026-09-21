# 0010: Add owned one-shot and deferred wrappers for typed events

Last updated: 2026-09-21

- Status: Accepted
- Scope: Typed event subscription lifecycle and delivery policy
- Supersedes: The statement in [0002](0002-csharp-events-for-signals.md) that deferred and one-shot delivery are not event features

## Context

ADR 0002 selected ordinary typed C# events and intentionally rejected a string-addressed signal registry. Native events provide synchronous multicast delivery and duplicate handler entries, but they do not provide a reusable ownership token, atomic one-shot consumption, cancellation of queued callbacks, or a standard way to route a handler through the scene safe point.

These capabilities are useful independently of scripting and serialization. They must preserve compile-time event signatures and must not introduce dynamic calls, reflection-based invocation, or untyped argument lists.

## Decision

- `EventConnection` wraps typed event add/remove accessors and implements `IDisposable`.
- Public overloads support the zero-, one-, and two-argument event shapes used by the engine.
- `oneShot: true` atomically consumes and disconnects the wrapper before invoking or scheduling the handler.
- An optional `Action<Action>` scheduler provides deferred delivery without making Core depend on Scene; callers normally pass `SceneTree.Defer`.
- Disposing a connection cancels deferred callbacks that have not begun, but does not wait for running callbacks.
- Engine events with payloads use sender-first typed signatures. Events carrying only their source use that source as their sole argument.
- Native duplicate delegate semantics are retained instead of adding a reference-count option.
- Persistent connections remain deferred until a typed schema can represent stable publisher, event, subscriber, and handler identities. ADR 0023's in-memory packed scenes intentionally omit runtime delegate subscriptions.

## Consequences

- Immediate, deferred, one-shot, and combined deferred one-shot delivery remain statically typed.
- One connection token owns one wrapper and gives subscribers an explicit lifecycle boundary.
- The scheduler determines delivery thread, safe point, ordering, and exception aggregation.
- Failed custom event removal can leave an inert wrapper in the publisher; the terminal token prevents further user-handler calls.
- Existing child-related `Node` events now pass the publishing parent first and the affected child second.
- There is still no `Connect`, `Disconnect`, event-name lookup, callable rebinding, or flags enum.

## Rejected alternatives

- Add a dynamic signal bus and connection flags: rejected because it duplicates C# events and conflicts with ADR 0001.
- Put deferred scheduling directly in Core: rejected because the scene safe point belongs to `SceneTree`.
- Depend on Reactive Extensions: rejected because the required lifecycle and scheduling policy is small and covered by the standard library.
- Serialize delegates or closures: rejected because they do not provide stable scene endpoint identities.
