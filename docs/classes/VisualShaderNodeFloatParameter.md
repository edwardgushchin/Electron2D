# VisualShaderNodeFloatParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeFloatParameter.cs](../../src/Scene/Resources/VisualShaderNodeFloatParameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeFloatParameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named float value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeFloatParameter()`](#member-8d3c3bc1bd73) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-878b63be3bd0) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-aef55e879c7b) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Single DefaultValue { get; set; }`](#member-2d8eaf054dba) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-2255081b2c2b) | Gets or changes whether import supplies the stored authored default. |
| [`public Electron2D.VisualShaderNodeFloatParameter.HintEnum Hint { get; set; }`](#member-43fb60f4ad54) | Gets or selects advisory numeric metadata; this does not clamp runtime values. |
| [`public System.Single Max { get; set; }`](#member-5a0ad7ae8bab) | Gets or changes the advisory max value. |
| [`public System.Single Min { get; set; }`](#member-d9e7496f14d7) | Gets or changes the advisory min value. |
| [`public System.Single Step { get; set; }`](#member-ddd04169fd0c) | Gets or changes the advisory step value. |

<a id="member-8d3c3bc1bd73"></a>
### `.ctor`

```csharp
public VisualShaderNodeFloatParameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-878b63be3bd0"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-aef55e879c7b"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-2d8eaf054dba"></a>
### `DefaultValue`

```csharp
public System.Single DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially 0f. Floating components must be finite; disabling the default retains this value.

Exceptions:

- `T:System.ArgumentException`: A floating component is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-2255081b2c2b"></a>
### `DefaultValueEnabled`

```csharp
public System.Boolean DefaultValueEnabled { get; set; }
```

Gets or changes whether import supplies the stored authored default.

False initially. Global values remain registry-owned and cannot use local authored defaults.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-43fb60f4ad54"></a>
### `Hint`

```csharp
public Electron2D.VisualShaderNodeFloatParameter.HintEnum Hint { get; set; }
```

Gets or selects advisory numeric metadata; this does not clamp runtime values.

None initially. An unchanged selection is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The value is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-5a0ad7ae8bab"></a>
### `Max`

```csharp
public System.Single Max { get; set; }
```

Gets or changes the advisory max value.

Initially one; retained independently of the selected hint.

Exceptions:

- `T:System.ArgumentException`: A floating value is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-d9e7496f14d7"></a>
### `Min`

```csharp
public System.Single Min { get; set; }
```

Gets or changes the advisory min value.

Initially zero; retained independently of the selected hint.

Exceptions:

- `T:System.ArgumentException`: A floating value is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-ddd04169fd0c"></a>
### `Step`

```csharp
public System.Single Step { get; set; }
```

Gets or changes the advisory step value.

Initially 0.1; retained independently of the selected hint.

Exceptions:

- `T:System.ArgumentException`: A floating value is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
