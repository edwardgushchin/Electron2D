# VisualShaderNodeVec4Parameter

Last updated: 2026-10-22

Source: [VisualShaderNodeVec4Parameter.cs](../../src/Scene/Resources/VisualShaderNodeVec4Parameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVec4Parameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named Vector4 value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVec4Parameter()`](#member-6de0c0f64ba8) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-21cfa26ac4bd) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ab36eb77571d) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.Vector4 DefaultValue { get; set; }`](#member-dcab48935225) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-c0673edf8160) | Gets or changes whether import supplies the stored authored default. |

<a id="member-6de0c0f64ba8"></a>
### `.ctor`

```csharp
public VisualShaderNodeVec4Parameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-21cfa26ac4bd"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-ab36eb77571d"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-dcab48935225"></a>
### `DefaultValue`

```csharp
public Electron2D.Vector4 DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially Vector4.Zero. Floating components must be finite; disabling the default retains this value.

Exceptions:

- `T:System.ArgumentException`: A floating component is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-c0673edf8160"></a>
### `DefaultValueEnabled`

```csharp
public System.Boolean DefaultValueEnabled { get; set; }
```

Gets or changes whether import supplies the stored authored default.

False initially. Global values remain registry-owned and cannot use local authored defaults.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
