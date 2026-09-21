# KeyLocation

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEnums.cs`](../../src/Core/Input/InputEnums.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum KeyLocation`

> Identifies the physical side of a duplicated keyboard key.

## Description

Identifies the physical side of a duplicated keyboard key.

- Complete values: `Unspecified = 0`, `Left = 1`, `Right = 2`.
- Responsibility: distinguishes left/right physical variants when a binding requests it.
- Invariants/errors: InputEventKey rejects undefined assignments. Immutable allocation-free value.
- Verification: location validation, text, copy, and physical-key matching are covered.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = KeyLocation.Unspecified;
```

## Constants

| Member | Description |
| --- | --- |
| [`Unspecified = 0`](#f-electron2d-keylocation-unspecified) | The key is not side-specific. |
| [`Left = 1`](#f-electron2d-keylocation-left) | The key is on the left side. |
| [`Right = 2`](#f-electron2d-keylocation-right) | The key is on the right side. |

## Constant Descriptions

<a id="f-electron2d-keylocation-unspecified"></a>
### `Unspecified = 0`

The key is not side-specific.

<a id="f-electron2d-keylocation-left"></a>
### `Left = 1`

The key is on the left side.

<a id="f-electron2d-keylocation-right"></a>
### `Right = 2`

The key is on the right side.
