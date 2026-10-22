# VisualShaderNodeUIntParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeUIntParameter.cs](../../src/Scene/Resources/VisualShaderNodeUIntParameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeUIntParameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named uint value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeUIntParameter()`](#member-27efc18b3e51) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-166265cc9052) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-3ed6a23f55b3) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.UInt32 DefaultValue { get; set; }`](#member-31641d1fe56e) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-0abfe31c465d) | Gets or changes whether import supplies the stored authored default. |

<a id="member-27efc18b3e51"></a>
### `.ctor`

```csharp
public VisualShaderNodeUIntParameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-166265cc9052"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-3ed6a23f55b3"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-31641d1fe56e"></a>
### `DefaultValue`

```csharp
public System.UInt32 DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially 0u. Disabling the default retains this value.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-0abfe31c465d"></a>
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
