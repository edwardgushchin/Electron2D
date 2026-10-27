# RDPipelineSpecializationConstant\<T\>

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineSpecializationConstant<T>`. **Component:** [Pipeline specialization](../components/local-compute.md#typed-pipeline-specialization).

**Inherits:** [RDPipelineSpecializationConstant](RDPipelineSpecializationConstant.md).

## Description

Supplies a strongly typed scalar value for a pipeline specialization identifier.

Value initially has no override, preserving an unassigned description. Assign a value before passing it to a pipeline; omitted descriptions leave the module's defaults.

The identifier base permits a heterogeneous typed span. The sealed generic family selects bool, sbyte, byte, short, ushort, int, uint, long, ulong, Half, float or double. ConstantID starts at zero; Value starts at null and must be assigned before pipeline consumption. Factories copy values and preserve their scalar bits. Subsequent mutation/disposal has no effect on existing variants. Invalid scalar types, unassigned/duplicate/unknown/negative identifiers and incompatible module types reject before native creation. Inherited owner lifetime and typed property reversion remain available.

## Example

The surrounding consumer supplies an existing compiled shader RID and its declared identifier.

```csharp
using var count = new RDPipelineSpecializationConstant<uint> { ConstantID = 3, Value = 4 };
var pipeline = device.ComputePipelineCreate(shader, [count]);
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDPipelineSpecializationConstant()`](#member-b4ebc466d569) | Creates an unassigned typed specialization description. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-7f814d6b5cae) | Projects the nullable typed scalar value alongside inherited identifier metadata. |
| [`public Nullable<T> Value { get; set; }`](#member-0f4b112c573f) | Gets or sets the copied scalar override, or null when no value has been assigned. |

## Member descriptions

<a id="member-b4ebc466d569"></a>
### RDPipelineSpecializationConstant<T>()

```csharp
public RDPipelineSpecializationConstant()
```

Creates an unassigned typed specialization description.

- `T:System.NotSupportedException`: T has no executing scalar specialization profile.

<a id="member-7f814d6b5cae"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the nullable typed scalar value alongside inherited identifier metadata.

Inherited descriptors followed by the value and its unassigned default.

<a id="member-0f4b112c573f"></a>
### Value

```csharp
public Nullable<T> Value { get; set; }
```

Gets or sets the copied scalar override, or null when no value has been assigned.

Initially null. Scalar type and module type must agree when consumed.

## Behavior and verification

Scalar defaults/overrides and dependent expressions resolve before native creation; workgroups and fixed uniform/push arrays use specialized values. Pipelines own independent native variants and borrow public shader identity; uniform sets keep that shader identity. [The executable tests](../../tests/Electron2D.Tests/RenderingDeviceSpecializationTests.cs) cover constant operations, metadata/defaults, native compute values, graphics/canvas pixels, copying, errors and prepared zero managed allocation on Linux/Vulkan. Other scalar precision, foreign platform execution and native allocation totals retain the limits in [the component contract](../components/local-compute.md#typed-pipeline-specialization).

Native Double values, conversions and exact literal bits execute with the enabled Vulkan FP64 feature; [the precision contract](../components/local-compute.md#native-fp64-arithmetic-and-data) records raw data/push layouts, native checks and the pinned HLSL compiler boundary.

Native Int64/UInt64 values, width-correct operations and exact overrides use the enabled Vulkan integer feature. See [wide integer execution](../components/local-compute.md#native-64-bit-integer-arithmetic-and-data) for raw data, graphics transfer and compiler boundaries.

[Native Half specialization](../components/local-compute.md#native-half-arithmetic-and-packed-data) preserves two-byte scalar bits, width-only conversions and explicit arithmetic/storage prerequisites.

[Native Int16/UInt16 specialization](../components/local-compute.md#native-int16-and-uint16-arithmetic-and-packed-data) preserves exact two-byte values, signed literal extension and width-correct integer operations/conversions.

[Native byte specialization](../components/local-compute.md#native-int8-and-uint8-arithmetic-and-packed-data) preserves one-byte values, literal extension, width-correct operations and bounded layout extents.
