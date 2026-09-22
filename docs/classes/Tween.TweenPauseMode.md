# Tween.TweenPauseMode

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Tween.TweenPauseMode`

> Selects how tree pause affects a tween.

## Description

Selects how tree pause affects a tween.

This nested enum controls [`Tween`](Tween.md) eligibility while `SceneTree.Paused` is true. `Bound = 0` follows a bound node's resolved `CanProcess()` policy and otherwise behaves like Stop; `Stop = 1` pauses with the tree; `Process = 2` ignores tree pause. Bound is the default. Undefined values are rejected without state change and mutation uses the owner thread. Tests cover unbound tree pause and pause-independent processing; SceneNode processing tests cover the inherited policies used by Bound. This is a scheduling policy, not `Tween.Pause()` state.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Tween.TweenPauseMode.Bound;
```

## Constants

| Member | Description |
| --- | --- |
| [`Bound = 0`](#f-electron2d-tween-tweenpausemode-bound) | Uses the bound node's effective process policy, or stops with the tree when no node is bound. |
| [`Stop = 1`](#f-electron2d-tween-tweenpausemode-stop) | Stops while the owning scene tree is paused. |
| [`Process = 2`](#f-electron2d-tween-tweenpausemode-process) | Continues regardless of the owning scene tree's pause state. |

## Constant Descriptions

<a id="f-electron2d-tween-tweenpausemode-bound"></a>
### `Bound = 0`

Uses the bound node's effective process policy, or stops with the tree when no node is bound.

<a id="f-electron2d-tween-tweenpausemode-stop"></a>
### `Stop = 1`

Stops while the owning scene tree is paused.

<a id="f-electron2d-tween-tweenpausemode-process"></a>
### `Process = 2`

Continues regardless of the owning scene tree's pause state.
