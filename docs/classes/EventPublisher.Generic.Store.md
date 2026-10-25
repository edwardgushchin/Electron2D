# EventPublisher.Generic.Store

Last updated: 2026-10-26

- **Source:** [EventPublisher.cs](../../src/Core/Object/EventPublisher.cs)
- **Declaration:** `private sealed class Store : EventConnection.IPublisher`
- **Visibility:** Private implementation of [EventPublisher<T>](EventPublisher.Generic.md).

The store serializes delegate addition/removal/replacement and current-membership
queries. Volatile publication exposes immutable snapshots without taking the
mutation gate. Exact token wrappers register before the gate, preserving the
shared token-then-store lock order. Callback invocation remains outside this type.
It retains no source owner and introduces no public API.
