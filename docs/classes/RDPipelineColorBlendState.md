# RDPipelineColorBlendState

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineColorBlendState`. **Source:** [RDPipelineColorBlendState.cs](../../src/Servers/Rendering/Device/RDPipelineColorBlendState.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned container copied by RenderPipelineCreate. Attachments getters/setters copy the array while borrowing its elements until creation. The pipeline copies active attachment values and finite initial BlendConstant. Attachments cover logical color slots, including unused holes; unused slots and extra trailing entries are ignored. Active integer targets reject arithmetic blending unless EnableLogicOp is true. Copied LogicOp then selects one bitwise operation for all attachments, preserves their write masks and disables arithmetic blending. Floating-point and sRGB targets receive the source without a logical operation. See [logical color operations](../components/logic-operations.md) for feature negotiation and exact encoded semantics.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var attachment = new RDPipelineColorBlendStateAttachment();
attachment.SetAsMix();
using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDPipelineColorBlendState()` | Creates an empty attachment configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the inherited typed property metadata and this description's values. |
| `public Electron2D.RDPipelineColorBlendStateAttachment[] Attachments { get; set; }` | Gets or replaces the attachment list, copying its array and borrowing its elements. |
| `public Electron2D.Color BlendConstant { get; set; }` | Gets or sets the finite initial blend constant. |

| `public System.Boolean EnableLogicOp { get; set; }` | Enables the copied logical operation; false initially. |
| `public Electron2D.RenderingDevice.LogicOperation LogicOp { get; set; }` | Selects the copied operation; Clear initially. |

## Member descriptions

<a id="member-32ba1ac2dc76"></a>
### RDPipelineColorBlendState()

```csharp
public RDPipelineColorBlendState()
```

Creates an empty attachment configuration.


<a id="member-a1f80531672c"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the inherited typed property metadata and this description's values.


<a id="member-89ad2e5e2900"></a>
### Attachments

```csharp
public Electron2D.RDPipelineColorBlendStateAttachment[] Attachments { get; set; }
```

Gets or replaces the attachment list, copying its array and borrowing its elements.

At least one entry per logical framebuffer color slot. Unused slots and extra trailing entries are ignored.


<a id="member-5587000f8796"></a>
### BlendConstant

```csharp
public Electron2D.Color BlendConstant { get; set; }
```

Gets or sets the finite initial blend constant.

### EnableLogicOp

```csharp
public System.Boolean EnableLogicOp { get; set; }
```

Enables logical color operations; false initially. When true, pipeline creation requires the independently enabled native logicOp feature and the pinned adapter. Arithmetic blending is disabled for all attachments, including float and sRGB targets.

### LogicOp

```csharp
public Electron2D.RenderingDevice.LogicOperation LogicOp { get; set; }
```

Selects the copied bitwise operation; Clear initially. Effective only when EnableLogicOp is true. Component write masks still apply. Unknown, negative and Max values reject immediately. Inherited typed property descriptors preserve both defaults and reversion.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests and DeviceGraphicsStorage.Consumer check Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDPipelineColorBlendState.md) for absent roles.
