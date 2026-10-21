# VisualShaderPortDefault<T>

Last updated: 2026-10-22

Source: [`VisualShaderPortDefault.cs`](../../src/Scene/Resources/VisualShaderPortDefault.cs).

```csharp
public sealed class Electron2D.VisualShaderPortDefault<T>
```

Inherits: [VisualShaderPortDefault](VisualShaderPortDefault.md).

## Description

An immutable numeric input-default entry with a concrete value type.

## Example

```csharp
var value = new VisualShaderPortDefault<float>(1, .25f);
operation.SetDefaultInputValues(new VisualShaderPortDefault[] { value });
```

The operation must supply a scalar input at index one. The immutable collection can mix different canonical numeric entry types without exposing an untyped value accessor.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderPortDefault(System.Int32 port, T value)`](#member-746fb8b6d9b8) | Copies and validates an input-default value. |
| [`public T Value { get;  }`](#member-a1a969afe600) | Gets the copied input value. |

<a id="member-746fb8b6d9b8"></a>
### `.ctor`

```csharp
public VisualShaderPortDefault(System.Int32 port, T value)
```

Copies and validates an input-default value.

- `port`: The nonnegative input index.
- `value`: The finite numeric value.
- `System.ArgumentException`: The type or value is unsupported.
- `System.ArgumentOutOfRangeException`: The port is negative.

<a id="member-a1a969afe600"></a>
### `Value`

```csharp
public T Value { get;  }
```

Gets the copied input value. This immutable numeric value was validated at construction.

## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)

Matrix4x4 entries copy all sixteen components by value. Returned struct edits cannot mutate the stored snapshot; graph setters validate finiteness. Matrix fields use the matrix default/codec contract in [numeric matrix graphs](../components/visual-shaders.md#numeric-matrix-data).
