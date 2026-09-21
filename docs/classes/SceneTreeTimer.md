# SceneTreeTimer

Last updated: 2026-09-21

## Declaration

- Source: [`SceneTreeTimer.cs`](../../src/Scene/Main/SceneTreeTimer.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class SceneTreeTimer : ElectronObject`
- Domain: [Scene](../domains/scene.md)
- Component: [Scene tree](../components/scene-tree.md)

## Responsibility and ownership

`SceneTreeTimer` is a lightweight one-shot delay owned by the `SceneTree` that creates it. It advances after node callbacks in either the process or physics lane, raises one typed timeout event, and disposes itself. Tree disposal also disposes every still-active timer.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `double TimeLeft { get; set; }` | Finite non-negative seconds remaining; owner-thread mutation while attached |
| `bool ProcessAlways { get; }` | Whether tree pause is ignored |
| `bool ProcessInPhysics { get; }` | Whether the physics lane advances the timer |
| `event Action<SceneTreeTimer> Timeout` | Synchronous one-shot delivery at zero, before automatic disposal |

Construction is internal through `SceneTree.CreateTimer`. Inherited identity, notification, property, translation, and deterministic lifetime behavior comes from [`ElectronObject`](ElectronObject.md).

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `GetPropertyDescriptors()` | Appends typed remaining-time and processing-policy descriptors |
| `ValidateDisposal()` | Requires the owner thread while a tree still owns the timer |
| `Dispose(bool disposing)` | Removes the timer from its tree, clears timeout subscribers, and calls base cleanup |

## Lifecycle and error behavior

The timer starts with the requested delay. Matching frames subtract their supplied delta unless the tree is paused and `ProcessAlways` is false. A zero duration waits for the next matching frame. At zero, the tree removes the timer before invoking `Timeout`; disposal is attempted even when a handler throws. Timeout and disposal failures are combined. Manual disposal cancels future timeout delivery. Setting an invalid duration throws without changing the prior value.

## Threading guarantees and non-guarantees

Creation, mutation, expiration, and disposal use the tree owner thread. The timer does not synchronize reads or event subscription. It has no independent clock or background task.

## Dependencies and interactions

The timer depends on `SceneTree` for scheduling and ownership and on `ElectronObject` for lifetime. It has no SDL, renderer, input, audio, or physics-simulation dependency.

## Verification and known limitations

Executable checks cover process and physics lanes, pause policy, finite-duration validation, timeout order, automatic disposal, and continuation after a throwing timeout handler. Direct tree calls supply delta unchanged; [`Engine`](Engine.md) applies its time scale before Engine-driven delivery. There is no per-timer ignore-time-scale option, repeating mode, cancellation token, or wall-clock guarantee; use hierarchy-owned [`Timer`](Timer.md) when repeat, autostart, local pause, packing, or time-scale bypass is required.
