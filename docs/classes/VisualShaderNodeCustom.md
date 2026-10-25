# VisualShaderNodeCustom

Last updated: 2026-10-25

Source: [VisualShaderNodeCustom.cs](../../src/Scene/Resources/VisualShaderNodeCustom.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeCustom
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Defines a compiled C# visual shader node for the offline canvas importer.

Register each concrete resource factory in the authoring/import/runtime hosts. Callbacks provide typed ports, defaults, metadata and GLSL source; no delegate or assembly is loaded from an archive. Definition discovery is lazy, after the derived constructor. Successful option edits rebuild callback defaults. Derived stored schema fields precede base descriptors, and derived resources retain the ordinary explicit copy hooks. Source callbacks run during import, never during material drawing. Editor UI remains a separate consumer.

## Example

This source assumes an initialized project resource root. Register the same factory in the compiled import and runtime hosts.

```csharp
using Electron2D;
using System.Collections.Generic;
ResourceFileTypes.RegisterResource("project.GainNode", static () => new GainNode());
using var node = new GainNode();
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/custom-source.e2dres", SaverFlags.BundleResources);

sealed class GainNode : VisualShaderNodeCustom
{
    public override int GetInputPortCount() => 1;
    public override int GetOutputPortCount() => 1;
    public override PortType GetOutputPortType(int port) => PortType.Vector3;
    public override VisualShaderPortDefault? GetInputPortDefaultValue(int port)
        => new VisualShaderPortDefault<float>(port, .5f);
    public override string GetCode(IReadOnlyList<string> inputs,
        IReadOnlyList<string> outputs, Shader.Mode mode, VisualShader.Type stage)
        => outputs[0] + "=vec3(" + (inputs[0].Length == 0 ? "0.0" : inputs[0]) + ");";
    protected override Resource CreateDuplicateInstance() => new GainNode();
}
```

Import through [the compiled provider workflow](../components/visual-shaders.md#compiled-custom-nodes), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public abstract class Electron2D.VisualShaderNodeCustom`](#member-3ec261a62001) | Defines a compiled C# visual shader node for the offline canvas importer. |
| [`protected VisualShaderNodeCustom()`](#member-565c4f62cce0) | Initializes an undiscovered custom definition without invoking derived callbacks. |
| [`public virtual System.String GetCategory()`](#member-03c641e3888d) | Returns the programmatic authoring category path. |
| [`public abstract System.String GetCode(System.Collections.Generic.IReadOnlyList<System.String> inputVariables, System.Collections.Generic.IReadOnlyList<System.String> outputVariables, Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)`](#member-8cd5ccf5674b) | Produces one GLSL statement block using opaque input and output symbols. |
| [`public override System.Int32 GetDefaultInputPort(Electron2D.VisualShaderNode.PortType type)`](#member-a97bc9ff4bff) | Returns the compiled provider connection target; the base policy selects input zero. |
| [`public virtual System.String GetDescription()`](#member-091b8118ab78) | Returns the authoring description. |
| [`public virtual System.String GetFuncCode(Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)`](#member-7de9a6c66b8e) | Produces one statement block at the beginning of each active graph stage function. |
| [`public virtual System.String GetGlobalCode(Electron2D.Shader.Mode mode)`](#member-c309e03b9d3c) | Produces shared GLSL declarations for the active shader module. |
| [`public virtual System.Int32 GetInputPortCount()`](#member-23ed832080e8) | Returns the active input count. |
| [`public virtual Electron2D.VisualShaderPortDefault GetInputPortDefaultValue(System.Int32 port)`](#member-421ed1ca1cec) | Provides a copied, strongly typed default for one input. |
| [`public virtual System.String GetInputPortName(System.Int32 port)`](#member-c90c3e227086) | Returns the input label supplied to programmatic authoring tools. |
| [`public virtual Electron2D.VisualShaderNode.PortType GetInputPortType(System.Int32 port)`](#member-4f76c48a9866) | Returns one input's value role. |
| [`public virtual System.String GetName()`](#member-499ff259c33c) | Returns the authoring caption. |
| [`public System.Int32 GetOptionIndex(System.Int32 option)`](#member-ec2806c1aae9) | Reads an active or retained dropdown selection. |
| [`public virtual System.Int32 GetOutputPortCount()`](#member-099c642bcae8) | Returns the active output count. |
| [`public virtual System.String GetOutputPortName(System.Int32 port)`](#member-988c9c995530) | Returns the output label supplied to authoring tools. |
| [`public virtual Electron2D.VisualShaderNode.PortType GetOutputPortType(System.Int32 port)`](#member-ce8da6d881bd) | Returns one output's value role. |
| [`public virtual System.Int32 GetPropertyCount()`](#member-4207f1fed715) | Returns the active dropdown-property count. |
| [`public virtual System.Int32 GetPropertyDefaultIndex(System.Int32 index)`](#member-7a33593a5de5) | Returns the initial selected index of one dropdown property. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-a6f8cb98a2e6) | Uses the inherited Resource construction or stored-property contract. |
| [`public virtual System.String GetPropertyName(System.Int32 index)`](#member-fe1b02d56efd) | Returns a dropdown property's authoring label. |
| [`public virtual System.String[] GetPropertyOptions(System.Int32 index)`](#member-ace63c88fb85) | Returns ordered dropdown labels. |
| [`public virtual System.Nullable<Electron2D.VisualShaderNode.PortType> GetReturnIconType()`](#member-87929b1f5f89) | Returns the optional authoring result-icon role. |
| [`public virtual System.Boolean IsAvailable(Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)`](#member-faed332ffad6) | Reports whether authoring tools should offer this provider in a context. |
| [`public virtual System.Boolean IsHighEnd()`](#member-08d2e8b85a6c) | Reports the author's advisory advanced-renderer mark. |
| [`protected override System.Void OnResetState()`](#member-bec7e6f18854) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Void RefreshDefinition()`](#member-faff03994600) | Regenerates metadata, ports and defaults after a compiled provider's own schema fields change. |
| [`public System.Void SetOptionIndex(System.Int32 option, System.Int32 index)`](#member-2dc3acccc8ac) | Changes a dropdown selection and regenerates ports and callback defaults atomically. |

<a id="member-3ec261a62001"></a>
### `VisualShaderNodeCustom`

```csharp
public abstract class Electron2D.VisualShaderNodeCustom
```

Defines a compiled C# visual shader node for the offline canvas importer.

Register each concrete resource factory in the authoring/import/runtime hosts. Callbacks provide typed ports, defaults, metadata and GLSL source; no delegate or assembly is loaded from an archive. Definition discovery is lazy, after the derived constructor. Successful option edits rebuild callback defaults. Derived stored schema fields precede base descriptors, and derived resources retain the ordinary explicit copy hooks. Source callbacks run during import, never during material drawing. Editor UI remains a separate consumer.

<a id="member-565c4f62cce0"></a>
### `.ctor`

```csharp
protected VisualShaderNodeCustom()
```

Initializes an undiscovered custom definition without invoking derived callbacks.

<a id="member-03c641e3888d"></a>
### `GetCategory`

```csharp
public virtual System.String GetCategory()
```

Returns the programmatic authoring category path.

Addons unless overridden.

<a id="member-8cd5ccf5674b"></a>
### `GetCode`

```csharp
public abstract System.String GetCode(System.Collections.Generic.IReadOnlyList<System.String> inputVariables, System.Collections.Generic.IReadOnlyList<System.String> outputVariables, Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)
```

Produces one GLSL statement block using opaque input and output symbols.

Use the supplied symbols without declaring replacements for them. They are opaque compiler names. The last authored connection to an input supplies its value, matching the ordinary graph connection policy. Loops, branches, return and fragment discard follow the ordinary expression scope contract.

Bounded strict UTF-8 GLSL; empty or literal null source emits no statements.

Parameters:

- `inputVariables`: Read-only symbols; an absent input/default is an empty string.
- `outputVariables`: Read-only numeric output symbols, initialized by the importer.
- `mode`: The executing shader domain.
- `type`: The executing graph stage.

<a id="member-a97bc9ff4bff"></a>
### `GetDefaultInputPort`

```csharp
public override System.Int32 GetDefaultInputPort(Electron2D.VisualShaderNode.PortType type)
```

Returns the compiled provider connection target. The base policy validates the concrete port role, discovers the definition lazily and selects input zero. A concrete provider may override that selection. Undefined roles, invalid metadata and disposed resources reject.

<a id="member-091b8118ab78"></a>
### `GetDescription`

```csharp
public virtual System.String GetDescription()
```

Returns the authoring description.

Empty unless overridden.

<a id="member-7de9a6c66b8e"></a>
### `GetFuncCode`

```csharp
public virtual System.String GetFuncCode(Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)
```

Produces one statement block at the beginning of each active graph stage function.

Empty initially; source is included once per concrete provider type and stage. Nonempty particle or Light setup activates that stage even without output assignments. Return empty for stages intended to stay inactive.

Parameters:

- `mode`: The shader domain.
- `type`: The target graph stage, including other stages when this provider is reachable.

<a id="member-c309e03b9d3c"></a>
### `GetGlobalCode`

```csharp
public virtual System.String GetGlobalCode(Electron2D.Shader.Mode mode)
```

Produces shared GLSL declarations for the active shader module.

Use ELECTRON2D_SHADER_STAGE for native stage-specific declarations. Shared fragment/light globals use stage one, and their bodies use one/two. Distinct provider types own their own symbol names.

Empty initially; source is included once per concrete reachable provider type.

Parameters:

- `mode`: The shader domain.

<a id="member-23ed832080e8"></a>
### `GetInputPortCount`

```csharp
public virtual System.Int32 GetInputPortCount()
```

Returns the active input count.

Zero unless overridden; discovery accepts at most 4096.

<a id="member-421ed1ca1cec"></a>
### `GetInputPortDefaultValue`

```csharp
public virtual Electron2D.VisualShaderPortDefault GetInputPortDefaultValue(System.Int32 port)
```

Provides a copied, strongly typed default for one input.

Discovery and option refresh consume these defaults. Subsequent ordinary typed default setters remain available.

A matching immutable entry for this index, or null for explicit absence. Samplers have no numeric default.

Parameters:

- `port`: An existing input index.

<a id="member-c90c3e227086"></a>
### `GetInputPortName`

```csharp
public virtual System.String GetInputPortName(System.Int32 port)
```

Returns the input label supplied to programmatic authoring tools.

in followed by the invariant port number unless overridden.

Parameters:

- `port`: An existing input index.

<a id="member-4f76c48a9866"></a>
### `GetInputPortType`

```csharp
public virtual Electron2D.VisualShaderNode.PortType GetInputPortType(System.Int32 port)
```

Returns one input's value role.

Scalar unless overridden.

Parameters:

- `port`: An existing input index.

<a id="member-499ff259c33c"></a>
### `GetName`

```csharp
public virtual System.String GetName()
```

Returns the authoring caption.

Unnamed unless overridden.

<a id="member-ec2806c1aae9"></a>
### `GetOptionIndex`

```csharp
public System.Int32 GetOptionIndex(System.Int32 option)
```

Reads an active or retained dropdown selection.

The retained selection, including explicit zero and temporarily inactive properties.

Parameters:

- `option`: A property index; unknown indices return zero.

Exceptions:

- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-099c642bcae8"></a>
### `GetOutputPortCount`

```csharp
public virtual System.Int32 GetOutputPortCount()
```

Returns the active output count.

Zero unless overridden; discovery accepts at most 4096.

<a id="member-988c9c995530"></a>
### `GetOutputPortName`

```csharp
public virtual System.String GetOutputPortName(System.Int32 port)
```

Returns the output label supplied to authoring tools.

out followed by the invariant port number unless overridden.

Parameters:

- `port`: An existing output index.

<a id="member-ce8da6d881bd"></a>
### `GetOutputPortType`

```csharp
public virtual Electron2D.VisualShaderNode.PortType GetOutputPortType(System.Int32 port)
```

Returns one output's value role.

Scalar unless overridden. Local sampler outputs reject during import.

Parameters:

- `port`: An existing output index.

<a id="member-4207f1fed715"></a>
### `GetPropertyCount`

```csharp
public virtual System.Int32 GetPropertyCount()
```

Returns the active dropdown-property count.

Zero unless overridden; discovery accepts at most 4096.

<a id="member-7a33593a5de5"></a>
### `GetPropertyDefaultIndex`

```csharp
public virtual System.Int32 GetPropertyDefaultIndex(System.Int32 index)
```

Returns the initial selected index of one dropdown property.

Zero unless overridden.

Parameters:

- `index`: An active property index.

<a id="member-a6f8cb98a2e6"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-fe1b02d56efd"></a>
### `GetPropertyName`

```csharp
public virtual System.String GetPropertyName(System.Int32 index)
```

Returns a dropdown property's authoring label.

prop unless overridden; descriptor identities also include the numeric property index.

Parameters:

- `index`: An active property index.

<a id="member-ace63c88fb85"></a>
### `GetPropertyOptions`

```csharp
public virtual System.String[] GetPropertyOptions(System.Int32 index)
```

Returns ordered dropdown labels.

Discovery copies the array. An empty list retains logical selection zero. The aggregate choice budget is 4096.

A fresh one-element Default list unless overridden.

Parameters:

- `index`: An active property index.

<a id="member-87929b1f5f89"></a>
### `GetReturnIconType`

```csharp
public virtual System.Nullable<Electron2D.VisualShaderNode.PortType> GetReturnIconType()
```

Returns the optional authoring result-icon role.

Null for no icon unless overridden; a concrete role excludes Max.

<a id="member-faed332ffad6"></a>
### `IsAvailable`

```csharp
public virtual System.Boolean IsAvailable(Electron2D.Shader.Mode mode, Electron2D.VisualShader.Type type)
```

Reports whether authoring tools should offer this provider in a context.

This is discovery metadata. Existing authored nodes still undergo ordinary source/stage validation at import.

True unless overridden.

Parameters:

- `mode`: The shader domain.
- `type`: The graph stage.

<a id="member-08d2e8b85a6c"></a>
### `IsHighEnd`

```csharp
public virtual System.Boolean IsHighEnd()
```

Reports the author's advisory advanced-renderer mark.

The actual renderer validates imported native capabilities independently.

False unless overridden.

<a id="member-bec7e6f18854"></a>
### `OnResetState`

```csharp
protected override System.Void OnResetState()
```

<a id="member-faff03994600"></a>
### `RefreshDefinition`

```csharp
public System.Void RefreshDefinition()
```

Regenerates metadata, ports and defaults after a compiled provider's own schema fields change.

Preserves selected and temporarily inactive options; resets the numeric default table from callbacks. Call this from schema-changing application setters. No virtual callback runs from the base constructor.

Exceptions:

- `T:System.InvalidOperationException`: Provider metadata is invalid or configuration mutation is reentrant.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-2dc3acccc8ac"></a>
### `SetOptionIndex`

```csharp
public System.Void SetOptionIndex(System.Int32 option, System.Int32 index)
```

Changes a dropdown selection and regenerates ports and callback defaults atomically.

Equal selections still regenerate and publish Changed and PropertyListChanged. Callback failure preserves engine configuration; arbitrary side effects in application callbacks are not rolled back. Invalidated graph edges remain caller-owned.

Parameters:

- `option`: An active property index.
- `index`: A valid choice; an empty choice list accepts logical zero.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The property or selection is invalid.
- `T:System.InvalidOperationException`: Definition discovery reenters configuration mutation or provider metadata is invalid.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, typed source/metadata limits, options, source/compiled archives and executable provider procedures. See [the operation contract](../components/visual-shaders.md#compiled-custom-nodes) for ports, pixel units, port mutation, scope, control flow and source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)

Nonempty GetFuncCode particle-stage setup activates that stage, including SDF contact queries and the Collide/Process branch, when its reserved output is empty. Return empty source for stages the provider does not define. The provider-only contact regression checks actual compute state rather than compilation alone.

## Authored canvas lights

See [canvas light graphs](../components/canvas-light-graphs.md) for the executing stage, typed inputs/outputs, fragment/vertex varying producers, shared native resource slots, custom provider activation, portable program validation and measured limits.
