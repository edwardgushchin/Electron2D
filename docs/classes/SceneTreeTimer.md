# SceneTreeTimer

Last updated: 2026-09-21

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Scene/Main/SceneTreeTimer.cs`](../../src/Scene/Main/SceneTreeTimer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class SceneTreeTimer : ElectronObject`

> Provides a lightweight one-shot timer processed by a [`SceneTree`](SceneTree.md).

## Description

Provides a lightweight one-shot timer processed by a [`SceneTree`](SceneTree.md).

`SceneTreeTimer` is a lightweight one-shot delay owned by the `SceneTree` that creates it. It advances after node callbacks in either the process or physics lane, raises one typed timeout event, and disposes itself. Tree disposal also disposes every still-active timer.

The owning tree updates the timer after node callbacks in the selected frame lane. The timer is automatically
disposed after timeout delivery. Keeping a managed reference does not extend its lifetime. Time advances only from
delivered frame deltas; [`Engine`](Engine.md) scales those deltas when it drives the tree. The timer has no internal
clock or independent time-scale bypass.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
SceneTreeTimer timer = tree.CreateTimer(0.5);
timer.Timeout += _ => Console.WriteLine("Elapsed");
```

## Properties

| Member | Description |
| --- | --- |
| [`public double TimeLeft { get; set; }`](#p-electron2d-scenetreetimer-timeleft) | Gets or sets the remaining delay in seconds. |

## Methods

| Member | Description |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-scenetreetimer-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-scenetreetimer-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-scenetreetimer-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<SceneTreeTimer> Timeout`](#e-electron2d-scenetreetimer-timeout) | Occurs once when the remaining delay reaches zero. |

## Property Descriptions

<a id="p-electron2d-scenetreetimer-timeleft"></a>
### `public double TimeLeft { get; set; }`

Gets or sets the remaining delay in seconds.

**Value:** A finite non-negative duration. It reaches zero before [`SceneTreeTimer.Timeout`](SceneTreeTimer.md#e-electron2d-scenetreetimer-timeout) is raised.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is negative, NaN, or infinite.
- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Changing the value does not change the timer's selected frame lane or pause policy. Zero expires on the next matching frame.

## Method Descriptions

<a id="m-electron2d-scenetreetimer-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends this class's remaining-time and processing-policy descriptors.

<a id="m-electron2d-scenetreetimer-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: The caller is not the owner thread.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Requires the owner thread while this timer remains owned by a scene tree.

<a id="m-electron2d-scenetreetimer-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Removes the timer from its tree, clears timeout subscribers, and calls the base implementation.

## Event Descriptions

<a id="e-electron2d-scenetreetimer-timeout"></a>
### `public event Action<SceneTreeTimer> Timeout`

Occurs once when the remaining delay reaches zero.

**Remarks:** Delivery is synchronous on the tree owner thread after node callbacks and before deferred work. The timer is
disposed after the event invocation returns or throws. A handler failure propagates through the owning frame as part of an
`AggregateException`.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `GetPropertyDescriptors()` | Appends typed remaining-time and processing-policy descriptors |
| `ValidateDisposal()` | Requires the owner thread while a tree still owns the timer |
| `Dispose(bool disposing)` | Removes the timer from its tree, clears timeout subscribers, and calls base cleanup |

## Lifecycle and error behavior

The timer starts with the requested delay. Matching frames subtract their supplied delta unless the tree is paused and the timer is not configured to process while paused. A zero duration waits for the next matching frame. At zero, the tree removes the timer before invoking `Timeout`; disposal is attempted even when a handler throws. Timeout and disposal failures are combined. Manual disposal cancels future timeout delivery. Setting an invalid duration throws without changing the prior value.

## Threading guarantees and non-guarantees

Creation, mutation, expiration, and disposal use the tree owner thread. The timer does not synchronize reads or event subscription. It has no independent clock or background task.

## Dependencies and interactions

The timer depends on `SceneTree` for scheduling and ownership and on `ElectronObject` for lifetime. It has no SDL, renderer, input, audio, or physics-simulation dependency.

## Verification and known limitations

Executable checks cover process and physics lanes, pause policy, finite-duration validation, timeout order, automatic disposal, and continuation after a throwing timeout handler. Direct tree calls supply delta unchanged; [`Engine`](Engine.md) applies its time scale before Engine-driven delivery. There is no per-timer ignore-time-scale option, repeating mode, cancellation token, or wall-clock guarantee; use hierarchy-owned [`Timer`](Timer.md) when repeat, autostart, local pause, packing, or time-scale bypass is required.
