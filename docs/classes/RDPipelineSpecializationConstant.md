# RDPipelineSpecializationConstant

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.RDPipelineSpecializationConstant`. **Component:** [Pipeline specialization](../components/local-compute.md#typed-pipeline-specialization).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Identifies a caller-owned typed pipeline specialization override.

Use the generic description to select Boolean, signed/unsigned 32/64-bit integer, single- or double-precision data. Pipelines copy descriptions and retain their shader identity.

The identifier base permits a heterogeneous typed span. The sealed generic family selects bool, int, uint, long, ulong, float or double. ConstantID starts at zero; Value starts at null and must be assigned before pipeline consumption. Factories copy values and preserve their scalar bits. Subsequent mutation/disposal has no effect on existing variants. Invalid scalar types, unassigned/duplicate/unknown/negative identifiers and incompatible module types reject before native creation. Inherited owner lifetime and typed property reversion remain available.

## Example

The surrounding consumer supplies an existing compiled shader RID and its declared identifier.

```csharp
using var count = new RDPipelineSpecializationConstant<uint> { ConstantID = 3, Value = 4 };
var pipeline = device.ComputePipelineCreate(shader, [count]);
```

## API

| Declaration | Contract |
| --- | --- |
| [`protected RDPipelineSpecializationConstant()`](#member-2d51577589e9) | Initializes a typed specialization description. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-518667cbc837) | Projects the specialization identifier through typed object metadata. |
| [`public System.Int32 ConstantID { get; set; }`](#member-86c2d390d02c) | Gets or sets the scalar specialization identifier declared by the module. |

## Member descriptions

<a id="member-2d51577589e9"></a>
### RDPipelineSpecializationConstant()

```csharp
protected RDPipelineSpecializationConstant()
```

Initializes a typed specialization description.

<a id="member-518667cbc837"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the specialization identifier through typed object metadata.

Inherited descriptors followed by the copied identifier field.

<a id="member-86c2d390d02c"></a>
### ConstantID

```csharp
public System.Int32 ConstantID { get; set; }
```

Gets or sets the scalar specialization identifier declared by the module.

Initially zero; nonnegative identities are validated when a pipeline consumes the description.

## Behavior and verification

Scalar defaults/overrides and dependent expressions resolve before native creation; workgroups and fixed uniform/push arrays use specialized values. Pipelines own independent native variants and borrow public shader identity; uniform sets keep that shader identity. [The executable tests](../../tests/Electron2D.Tests/RenderingDeviceSpecializationTests.cs) cover constant operations, metadata/defaults, native compute values, graphics/canvas pixels, copying, errors and prepared zero managed allocation on Linux/Vulkan. Other scalar precision, foreign platform execution and native allocation totals retain the limits in [the component contract](../components/local-compute.md#typed-pipeline-specialization).

Native Double values, conversions and exact literal bits execute with the enabled Vulkan FP64 feature; [the precision contract](../components/local-compute.md#native-fp64-arithmetic-and-data) records raw data/push layouts, native checks and the pinned HLSL compiler boundary.

Native Int64/UInt64 values, width-correct operations and exact overrides use the enabled Vulkan integer feature. See [wide integer execution](../components/local-compute.md#native-64-bit-integer-arithmetic-and-data) for raw data, graphics transfer and compiler boundaries.
