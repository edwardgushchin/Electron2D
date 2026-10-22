# VisualShaderNodeIntParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeIntParameter.cs](../../src/Scene/Resources/VisualShaderNodeIntParameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeIntParameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named int value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeIntParameter()`](#member-6f82eb077bc1) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-6fe9dec302a4) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-84db432d0017) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Int32 DefaultValue { get; set; }`](#member-138fb26ef144) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-646b5abd30b9) | Gets or changes whether import supplies the stored authored default. |
| [`public System.String[] EnumNames { get; set; }`](#member-d068b460bbfd) | Gets an independent copy or replaces the named integer choices. |
| [`public Electron2D.VisualShaderNodeIntParameter.HintEnum Hint { get; set; }`](#member-0abf1814b523) | Gets or selects advisory numeric metadata; this does not clamp runtime values. |
| [`public System.Int32 Max { get; set; }`](#member-27b1cc4f91b6) | Gets or changes the advisory max value. |
| [`public System.Int32 Min { get; set; }`](#member-9370cad47843) | Gets or changes the advisory min value. |
| [`public System.Int32 Step { get; set; }`](#member-c7b5146e6802) | Gets or changes the advisory step value. |

<a id="member-6f82eb077bc1"></a>
### `.ctor`

```csharp
public VisualShaderNodeIntParameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-6fe9dec302a4"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-84db432d0017"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-138fb26ef144"></a>
### `DefaultValue`

```csharp
public System.Int32 DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially 0. Disabling the default retains this value.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-646b5abd30b9"></a>
### `DefaultValueEnabled`

```csharp
public System.Boolean DefaultValueEnabled { get; set; }
```

Gets or changes whether import supplies the stored authored default.

False initially. Global values remain registry-owned and cannot use local authored defaults.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-d068b460bbfd"></a>
### `EnumNames`

```csharp
public System.String[] EnumNames { get; set; }
```

Gets an independent copy or replaces the named integer choices.

Empty initially. Order and explicit label/value syntax are retained.

Exceptions:

- `T:System.ArgumentNullException`: The array or one of its labels is null.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-0abf1814b523"></a>
### `Hint`

```csharp
public Electron2D.VisualShaderNodeIntParameter.HintEnum Hint { get; set; }
```

Gets or selects advisory numeric metadata; this does not clamp runtime values.

None initially. An unchanged selection is quiet.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The value is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-27b1cc4f91b6"></a>
### `Max`

```csharp
public System.Int32 Max { get; set; }
```

Gets or changes the advisory max value.

Initially 100; retained independently of the selected hint.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-9370cad47843"></a>
### `Min`

```csharp
public System.Int32 Min { get; set; }
```

Gets or changes the advisory min value.

Initially zero; retained independently of the selected hint.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-c7b5146e6802"></a>
### `Step`

```csharp
public System.Int32 Step { get; set; }
```

Gets or changes the advisory step value.

Initially one; retained independently of the selected hint.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
