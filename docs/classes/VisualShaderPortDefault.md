# VisualShaderPortDefault

Last updated: 2026-10-21

Source: [`VisualShaderPortDefault.cs`](../../src/Scene/Resources/VisualShaderPortDefault.cs).

```csharp
public abstract class Electron2D.VisualShaderPortDefault
```

Inherited by: [VisualShaderPortDefault<T>](VisualShaderPortDefault-of-T.md).

## Description

An immutable typed input-default entry in a visual shader graph.

This collection element describes one numeric shader port. It has no untyped public value accessor.

## Member summary

| Member | Contract |
| --- | --- |
| [`public System.Int32 Port { get;  }`](#member-773aa5212f8d) | Gets the zero-based input port index. |
| [`public System.Type ValueType { get;  }`](#member-18d0b5a4feda) | Gets the exact CLR type of this entry's value. |

<a id="member-773aa5212f8d"></a>
### `Port`

```csharp
public System.Int32 Port { get;  }
```

Gets the zero-based input port index. Its value is the nonnegative index copied at construction.

<a id="member-18d0b5a4feda"></a>
### `ValueType`

```csharp
public System.Type ValueType { get;  }
```

Gets the exact CLR type of this entry's value. This is its concrete numeric value type.

## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
