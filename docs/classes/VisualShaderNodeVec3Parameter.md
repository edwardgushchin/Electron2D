# VisualShaderNodeVec3Parameter

Last updated: 2026-10-22

Source: [VisualShaderNodeVec3Parameter.cs](../../src/Scene/Resources/VisualShaderNodeVec3Parameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVec3Parameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named Vector3 value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVec3Parameter()`](#member-2606a4fe137a) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-8a2d08a10274) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-5d3ffc98a4d5) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.Vector3 DefaultValue { get; set; }`](#member-10a78f9c0f0c) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-fbc61db96312) | Gets or changes whether import supplies the stored authored default. |

<a id="member-2606a4fe137a"></a>
### `.ctor`

```csharp
public VisualShaderNodeVec3Parameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-8a2d08a10274"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-5d3ffc98a4d5"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-10a78f9c0f0c"></a>
### `DefaultValue`

```csharp
public Electron2D.Vector3 DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially Vector3.Zero. Floating components must be finite; disabling the default retains this value.

Exceptions:

- `T:System.ArgumentException`: A floating component is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-fbc61db96312"></a>
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
