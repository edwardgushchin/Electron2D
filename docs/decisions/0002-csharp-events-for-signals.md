# 0002: Represent game signals with typed C# events

Last updated: 2026-09-20

- Status: Accepted; connection lifecycle extended by [0010](0010-typed-event-connections.md)
- Scope: Public event and callback APIs

## Context

Game objects need decoupled notifications such as death, health changes, collisions, and tree membership changes. Godot's core signal implementation supports dynamic names, `Callable`, `Variant` arguments, editor connections, deferred delivery, and one-shot flags. Electron2D has chosen a typed C# API without Variant.

## Decision

Discrete game notifications use typed C# events, normally `event Action` or `event Action<T...>`. Subscription uses `+=` and removal uses `-=`. Concrete types declare their own events; `ElectronObject` is not a string-addressed signal registry.

Core contracts that are known to be required may be declared before their producer component when their semantics are stable. `ElectronObject.ScriptChanged` is such a contract: the protected notifier exists now because scripting is confirmed for a later phase, while script attachment and initialization remain explicitly unimplemented.

Per-frame work does not use events. Implemented scene and fixed-step processing use direct `SceneTree` calls and virtual `Node` callbacks; future rendering, input routing, and other traversal follow the same direct-call rule.

## Lifetime rule

A shorter-lived subscriber must unsubscribe from a longer-lived publisher as part of its lifecycle. Direct `+=` subscriptions require matching `-=` cleanup; ADR 0010 adds `EventConnection` as the preferred owned token when deterministic cleanup, one-shot, or deferred delivery is needed. `ElectronObject` and `Node` clear the event subscriber lists they own during disposal, but this cannot remove a disposed subscriber from a different longer-lived publisher. Automatic weak events are not implemented.

## Consequences

- Signal names and arguments are compile-time checked.
- Renames are refactorable and discoverable by the IDE.
- There is no `Connect`, `Disconnect`, `EmitSignal`, `SignalName`, or `Callable` compatibility layer.
- Original decision: deferred and one-shot delivery were not signal features. ADR 0010 supersedes this point with typed `EventConnection` wrappers while deferred scheduling remains a separate `SceneTree` responsibility under ADR 0006.
- Exceptions from handlers follow normal multicast delegate behavior.

## Rejected alternatives

- Recreate Godot's dynamic signal bus: rejected because it requires the Variant/string infrastructure intentionally excluded by ADR 0001.
- Use events for every frame: rejected because direct calls make hot control flow and ordering explicit.
- Add Reactive Extensions: rejected because standard C# events cover the current requirement without another dependency.
