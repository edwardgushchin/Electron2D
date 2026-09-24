# PhysicsInterpolationMode

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [PhysicsInterpolationMode.cs](../../src/Scene/Main/PhysicsInterpolationMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum PhysicsInterpolationMode`

## Description

Selects whether a [`Node`](Node.md) presents its canvas transform between physics ticks when [`SceneTree.PhysicsInterpolation`](SceneTree.md#p-electron2d-scenetree-physicsinterpolation) is enabled. It does not change logical transforms or callback timing. An inherited root resolves On; [`Control`](Control.md) starts Off.

## Example

Inside a scene callback with a `Sprite sprite` reference:

```csharp
sprite.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
sprite.ResetPhysicsInterpolation();
```

## Values

| Member | Contract |
| --- | --- |
| [`Inherit = 0`](#inherit) | Uses the nearest ancestor policy; a root resolves On. |
| [`On = 1`](#on) | Enables interpolation when the scene tree enables it. |
| [`Off = 2`](#off) | Disables interpolation for this node and inheriting descendants. |

## Value descriptions

<a id="inherit"></a>
### `Inherit = 0`

Uses the nearest ancestor policy; a root resolves On.

<a id="on"></a>
### `On = 1`

Enables interpolation when the scene tree enables it.

<a id="off"></a>
### `Off = 2`

Disables interpolation for this node and inheriting descendants.

## Verification and limits

[PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) checks numeric identities, inheritance, Control default and opt-in, validation and packed state. [Native tests](../../tests/Electron2D.Tests/PhysicsInterpolationNativeTests.cs) check canvas and camera pixels on Linux dummy compatibility and Wayland compatibility/GPU. Other native targets and owner visual acceptance remain unverified.
