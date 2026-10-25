# EventPublisher.Generic

Last updated: 2026-10-26

- **Source:** [EventPublisher.cs](../../src/Core/Object/EventPublisher.cs)
- **Declaration:** `internal struct EventPublisher<T> where T : Delegate`
- **Visibility:** Internal implementation; no game-facing API.

## Description

An ordinary engine event retains one default slot in its owner. First nonnull
subscription lazily creates its private [store](EventPublisher.Generic.Store.md);
empty publishers allocate no store. Fields are never copied between owners.

Add and Remove preserve immutable .NET multicast delegate semantics, including
duplicates and last matching subsequence removal. Handlers obtains the current
immutable delegate snapshot or assigns an internal replacement/clear. An existing
snapshot remains callable after a later mutation. The owner retains its original
signal-blocking, callback/failure and disposal policy.

## Membership and locking

Each exact EventConnection emission wrapper observes the store before its
mutation gate is acquired. Queries take token then store locks, invoke no user
code and enumerate without allocating. The store does not capture its owner.
Terminal tokens release their observed publishers; clearing a store releases its
current handlers while accepted snapshots/deferred callbacks retain their rules.

Ordinary subscription/removal may allocate multicast delegates. This type does
not replace the prepared mutation buffers used by physical event publishers.
The [current qualification](../components/event-connections.md#ordinary-engine-publisher-qualification-2026-10-26) records actual profiles and pending gates.
