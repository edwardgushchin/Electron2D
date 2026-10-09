# ObjectIdentity

Last updated: 2026-10-09

**Declaration:** internal readonly record struct · **Source:** [ObjectIdentity.cs](../../src/Core/Object/ObjectIdentity.cs)

Pairs an immutable instance ID with a borrowed BCL weak reference. ElectronObject.BorrowIdentity
lazily caches the reference with atomic publication; repeated borrows copy the pair without
allocation. Target resolves only live objects; RawTarget is reserved for internal lifecycle
exit payloads while disposal is in progress. No finalizer or global object registry is added.

[Physics object associations](../components/physics-object-bindings.md) documents consumers,
thread/lifetime checks, snapshot behavior and verification.
