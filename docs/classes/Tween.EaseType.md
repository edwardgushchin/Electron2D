# Tween.EaseType

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Tween.EaseType`

> Selects where acceleration and deceleration occur within a transition.

## Description

Selects where acceleration and deceleration occur within a transition.

This nested enum chooses transition direction: `In = 0` accelerates, `Out = 1` decelerates, `InOut = 2` is slowest at both ends, and `OutIn = 3` is fastest at both ends. InOut is the Tween default. Linear produces the same value for every direction. Undefined values are rejected without state change. Tests cover stable identities, endpoint behavior, manual interpolation, and invalid values.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Tween.EaseType.In;
```

## Constants

| Member | Description |
| --- | --- |
| [`In = 0`](#f-electron2d-tween-easetype-in) | Starts slowly and accelerates. |
| [`Out = 1`](#f-electron2d-tween-easetype-out) | Starts quickly and decelerates. |
| [`InOut = 2`](#f-electron2d-tween-easetype-inout) | Starts and ends slowly. |
| [`OutIn = 3`](#f-electron2d-tween-easetype-outin) | Starts and ends quickly. |

## Constant Descriptions

<a id="f-electron2d-tween-easetype-in"></a>
### `In = 0`

Starts slowly and accelerates.

<a id="f-electron2d-tween-easetype-out"></a>
### `Out = 1`

Starts quickly and decelerates.

<a id="f-electron2d-tween-easetype-inout"></a>
### `InOut = 2`

Starts and ends slowly.

<a id="f-electron2d-tween-easetype-outin"></a>
### `OutIn = 3`

Starts and ends quickly.
