# VisualShaderNodeParameter

Last updated: 2026-10-22

Source: [VisualShaderNodeParameter.cs](../../src/Scene/Resources/VisualShaderNodeParameter.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeParameter
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

Inherited by: [VisualShaderNodeBooleanParameter](VisualShaderNodeBooleanParameter.md), [VisualShaderNodeColorParameter](VisualShaderNodeColorParameter.md), [VisualShaderNodeFloatParameter](VisualShaderNodeFloatParameter.md), [VisualShaderNodeIntParameter](VisualShaderNodeIntParameter.md), [VisualShaderNodeMatrixParameter](VisualShaderNodeMatrixParameter.md), [VisualShaderNodeUIntParameter](VisualShaderNodeUIntParameter.md), [VisualShaderNodeVec2Parameter](VisualShaderNodeVec2Parameter.md), [VisualShaderNodeVec3Parameter](VisualShaderNodeVec3Parameter.md), [VisualShaderNodeVec4Parameter](VisualShaderNodeVec4Parameter.md).

## Description

Declares a named typed value in an offline-compiled visual shader.

Definitions are borrowed by graphs. Name edits and explicit slot assignments emit Changed after commitment. AddNode makes names unique; import validates later edits, qualifiers and the complete symbol table.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeParameter()`](#member-6f2b8de7f3b7) | Initializes a parameter definition. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-92371aeb76e6) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Int32 InstanceIndex { get; set; }`](#member-5d71dc6938ec) | Gets or assigns the explicit per-item slot used by InstanceIndex. |
| [`public System.String ParameterName { get; set; }`](#member-1453986d069c) | Gets or assigns the authored symbol name. |
| [`public Electron2D.VisualShaderNodeParameter.QualifierEnum Qualifier { get; set; }`](#member-53d673b08a4e) | Gets or selects the value owner. |

<a id="member-6f2b8de7f3b7"></a>
### `.ctor`

```csharp
protected VisualShaderNodeParameter()
```

Initializes a parameter definition.

<a id="member-92371aeb76e6"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-5d71dc6938ec"></a>
### `InstanceIndex`

```csharp
public System.Int32 InstanceIndex { get; set; }
```

Gets or assigns the explicit per-item slot used by InstanceIndex.

Zero initially; valid slots are zero through fifteen. Other qualifiers retain this dormant value.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The slot is outside zero through fifteen.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-1453986d069c"></a>
### `ParameterName`

```csharp
public System.String ParameterName { get; set; }
```

Gets or assigns the authored symbol name.

Empty initially; AddNode assigns a valid unique name. Later edits must form a valid nonreserved symbol at import.

Exceptions:

- `T:System.ArgumentNullException`: The name is null.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-53d673b08a4e"></a>
### `Qualifier`

```csharp
public Electron2D.VisualShaderNodeParameter.QualifierEnum Qualifier { get; set; }
```

Gets or selects the value owner.

None initially. An unchanged selection is quiet. Matrix instance declarations reject at import.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The value is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
