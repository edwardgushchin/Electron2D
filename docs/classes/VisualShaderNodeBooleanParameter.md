# VisualShaderNodeBooleanParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeBooleanParameter.cs](../../src/Scene/Resources/VisualShaderNodeBooleanParameter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeBooleanParameter
```

Inherits: [VisualShaderNodeParameter](VisualShaderNodeParameter.md).

## Description

Declares a named bool value for an imported canvas shader.

Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require reimport. Material, global and per-item values use their existing typed runtime APIs.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeBooleanParameter()`](#member-61e12fe9fa6b) | Creates the definition with its initial stored value and the authored default disabled. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-fbebcc05cef7) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-5be9a7990259) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Boolean DefaultValue { get; set; }`](#member-6afcb3320c3d) | Gets or replaces the copied authored default. |
| [`public System.Boolean DefaultValueEnabled { get; set; }`](#member-815ffe156f39) | Gets or changes whether import supplies the stored authored default. |

<a id="member-61e12fe9fa6b"></a>
### `.ctor`

```csharp
public VisualShaderNodeBooleanParameter()
```

Creates the definition with its initial stored value and the authored default disabled.

<a id="member-fbebcc05cef7"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-5be9a7990259"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-6afcb3320c3d"></a>
### `DefaultValue`

```csharp
public System.Boolean DefaultValue { get; set; }
```

Gets or replaces the copied authored default.

Initially false. Disabling the default retains this value.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-815ffe156f39"></a>
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
