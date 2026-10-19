# RDPipelineColorBlendState

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineColorBlendState`. **Source:** [RDPipelineColorBlendState.cs](../../src/Servers/Rendering/Device/RDPipelineColorBlendState.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned container copied by RenderPipelineCreate. Attachments getters/setters copy the array while borrowing its elements until creation. The pipeline copies active attachment values and finite initial BlendConstant. Attachments cover logical color slots, including unused holes; unused slots and extra trailing entries are ignored. Active integer targets reject enabled blending. Logic operations require native backend support and have no inert public fields.

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

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDPipelineColorBlendState.md) for absent roles.
