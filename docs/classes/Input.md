# Input

Last updated: 2026-09-21

## Declaration

- Source: [`Input.cs`](../../src/Core/Input/Input.cs)
- Declaration: `public sealed class Input : ElectronObject`
- Domain/component: [Input](../domains/input.md) / [Input runtime](../components/input-runtime.md)

## Responsibility and ownership

`Input` is the non-disposable process-wide owner of raw keyboard/mouse/controller state, mapped action contributions, and independent process/physics transition windows. It never owns submitted events or native devices.

## Complete public/protected API

| Member | Current behavior |
| --- | --- |
| `const int MaxEventsPerAction = 32` | Binding/source index ceiling |
| `static Input Instance` | Process-lifetime singleton |
| `MouseButtonMask MouseButtonMask` | Current held non-wheel buttons |
| `Vector2 LastMouseVelocity`, `LastMouseScreenVelocity` | Latest local and screen-space velocities |
| `IsKeyPressed`, `IsPhysicalKeyPressed`, `IsKeyLabelPressed` | Logical, physical, and label held-state queries |
| `IsMouseButtonPressed` | Held state for one non-wheel button; wheel directions always return false |
| `IsJoyButtonPressed(button, device)`, `GetJoyAxis(axis, device)` | Per-controller raw state |
| `IsAnythingPressed()` | Keys, mouse/controller buttons, or mapped actions |
| `IsActionPressed`, `IsActionJustPressed`, `IsActionJustReleased` | Current and lane-latched action state with optional exact matching |
| `IsActionJustPressedByEvent`, `IsActionJustReleasedByEvent` | Transition identity against the submitted event instance |
| `GetActionStrength`, `GetActionRawStrength` | Maximum mapped contribution after/before deadzone remapping |
| `GetAxis`, `GetVector` | Signed two-action axis and circularly deadzoned four-action vector; vector composition starts from raw action strengths |
| `ActionPress`, `ActionRelease` | Synthetic action source without event dispatch |
| `ParseInputEvent` | Non-reentrant active-loop validation, state update, then synchronous dispatch; an unindexed direct action is rejected when all 32 source slots are occupied |
| `ReleasePressedEvents` | Clears raw inputs/sources and latches releases without emitting events |
| `GetPropertyDescriptors()` | Adds read-only mouse state descriptors |
| `ValidateDisposal()` | Always rejects disposal |

Inherited identity, notification, translation, typed property, and string behavior is documented by [`ElectronObject`](ElectronObject.md).

## Lifecycle, ordering, and errors

The singleton exists for the process lifetime. Parsing first validates any active MainLoop's owner thread and idle-running lifecycle, then fully resolves mappings before mutation, commits state, and dispatches. Wrong-thread, nested-frame/lifecycle, source-capacity, and parse re-entry failures occur before state mutation. Scene callback failure propagates without rolling committed state back. Invalid names/devices/enums/non-finite strengths/deadzones throw typed C# exceptions; unregistered actions throw `KeyNotFoundException`.

Each process/physics callback sees its own transition latch. That lane clears in MainLoop `finally`; a failed frame cannot leak a just transition into its next frame. Outside a callback, transition queries use the process lane.

## Threading and invariants

State/configuration queries are lock-serialized. Event parsing is serialized and callback delivery runs on the caller thread; an active MainLoop therefore requires its owner thread and rejects calls made inside another loop callback. Returned value snapshots need no lifetime management. The warmed mapped parse/traversal path is allocation-free.

## Dependencies, verification, and limitations

Depends on InputMap, typed event classes, Engine/MainLoop, SceneTree, and core math. Managed tests cover all behavior named above, including failure/re-entry/allocation. Hardware APIs are absent; their exact implementation triggers are in [ADR 0038](../decisions/input.md#deferred-coverage-and-exact-implementation-triggers).
