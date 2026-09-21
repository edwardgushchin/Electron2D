# Tween.TransitionType

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Tween.TransitionType`

> Selects the interpolation curve family.

## Description

Selects the interpolation curve family.

This nested enum chooses the normalized interpolation family: `Linear = 0`, `Sine = 1`, `Quint = 2`, `Quart = 3`, `Quad = 4`, `Expo = 5`, `Elastic = 6`, `Cubic = 7`, `Circ = 8`, `Bounce = 9`, `Back = 10`, and `Spring = 11`. Linear is the Tween default. `EaseType` determines direction. Elastic, Bounce, Back, and Spring may overshoot; extrapolation outside the duration follows the equation and may yield non-finite values for a mathematically undefined region. Undefined values are rejected. Tests cover stable identities, exact endpoints, linear values, the distinct Expo/Elastic/Back in-out equations, and invalid enums; they do not numerically certify every point of every curve against another implementation.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Tween.TransitionType.Linear;
```

## Constants

| Member | Description |
| --- | --- |
| [`Linear = 0`](#f-electron2d-tween-transitiontype-linear) | Uses constant interpolation speed. |
| [`Sine = 1`](#f-electron2d-tween-transitiontype-sine) | Uses a sinusoidal curve. |
| [`Quint = 2`](#f-electron2d-tween-transitiontype-quint) | Uses a fifth-power curve. |
| [`Quart = 3`](#f-electron2d-tween-transitiontype-quart) | Uses a fourth-power curve. |
| [`Quad = 4`](#f-electron2d-tween-transitiontype-quad) | Uses a quadratic curve. |
| [`Expo = 5`](#f-electron2d-tween-transitiontype-expo) | Uses an exponential curve. |
| [`Elastic = 6`](#f-electron2d-tween-transitiontype-elastic) | Uses an oscillating elastic curve. |
| [`Cubic = 7`](#f-electron2d-tween-transitiontype-cubic) | Uses a cubic curve. |
| [`Circ = 8`](#f-electron2d-tween-transitiontype-circ) | Uses a circular curve. |
| [`Bounce = 9`](#f-electron2d-tween-transitiontype-bounce) | Uses a bouncing curve. |
| [`Back = 10`](#f-electron2d-tween-transitiontype-back) | Uses an overshooting back curve. |
| [`Spring = 11`](#f-electron2d-tween-transitiontype-spring) | Uses a damped spring curve. |

## Constant Descriptions

<a id="f-electron2d-tween-transitiontype-linear"></a>
### `Linear = 0`

Uses constant interpolation speed.

<a id="f-electron2d-tween-transitiontype-sine"></a>
### `Sine = 1`

Uses a sinusoidal curve.

<a id="f-electron2d-tween-transitiontype-quint"></a>
### `Quint = 2`

Uses a fifth-power curve.

<a id="f-electron2d-tween-transitiontype-quart"></a>
### `Quart = 3`

Uses a fourth-power curve.

<a id="f-electron2d-tween-transitiontype-quad"></a>
### `Quad = 4`

Uses a quadratic curve.

<a id="f-electron2d-tween-transitiontype-expo"></a>
### `Expo = 5`

Uses an exponential curve.

<a id="f-electron2d-tween-transitiontype-elastic"></a>
### `Elastic = 6`

Uses an oscillating elastic curve.

<a id="f-electron2d-tween-transitiontype-cubic"></a>
### `Cubic = 7`

Uses a cubic curve.

<a id="f-electron2d-tween-transitiontype-circ"></a>
### `Circ = 8`

Uses a circular curve.

<a id="f-electron2d-tween-transitiontype-bounce"></a>
### `Bounce = 9`

Uses a bouncing curve.

<a id="f-electron2d-tween-transitiontype-back"></a>
### `Back = 10`

Uses an overshooting back curve.

<a id="f-electron2d-tween-transitiontype-spring"></a>
### `Spring = 11`

Uses a damped spring curve.
