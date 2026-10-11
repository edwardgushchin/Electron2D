# RDPipelineColorBlendStateAttachment

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineColorBlendStateAttachment`. **Source:** [RDPipelineColorBlendStateAttachment.cs](../../src/Servers/Rendering/Device/RDPipelineColorBlendStateAttachment.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned per-target blend description copied by RenderPipelineCreate. RGB and alpha operations/factors are separate. Channel write masks execute even when blending is disabled. SetAsMix enables source-alpha color mixing with accumulated alpha. Invalid factors/operations and integer-target blending reject before native creation.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var attachment = new RDPipelineColorBlendStateAttachment();
attachment.SetAsMix();
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDPipelineColorBlendStateAttachment()` | Creates the default configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the caller-owned configuration through inherited typed property metadata. |
| `public System.Void SetAsMix()` | Configures ordinary source-alpha color mixing with accumulated alpha. |
| `public Electron2D.RenderingDevice.BlendOperation AlphaBlendOp { get; set; }` | Gets or sets the alpha combination operation. |
| `public Electron2D.RenderingDevice.BlendOperation ColorBlendOp { get; set; }` | Gets or sets the RGB combination operation. |
| `public Electron2D.RenderingDevice.BlendFactor DstAlphaBlendFactor { get; set; }` | Gets or sets the destination alpha factor. |
| `public Electron2D.RenderingDevice.BlendFactor DstColorBlendFactor { get; set; }` | Gets or sets the destination RGB factor. |
| `public System.Boolean EnableBlend { get; set; }` | Gets or sets whether source and destination pixels blend. |
| `public Electron2D.RenderingDevice.BlendFactor SrcAlphaBlendFactor { get; set; }` | Gets or sets the source alpha factor. |
| `public Electron2D.RenderingDevice.BlendFactor SrcColorBlendFactor { get; set; }` | Gets or sets the source RGB factor. |
| `public System.Boolean WriteA { get; set; }` | Gets or sets whether alpha writes are enabled. |
| `public System.Boolean WriteB { get; set; }` | Gets or sets whether blue writes are enabled. |
| `public System.Boolean WriteG { get; set; }` | Gets or sets whether green writes are enabled. |
| `public System.Boolean WriteR { get; set; }` | Gets or sets whether red writes are enabled. |

## Member descriptions

<a id="member-17d20660538b"></a>
### RDPipelineColorBlendStateAttachment()

```csharp
public RDPipelineColorBlendStateAttachment()
```

Creates the default configuration.


<a id="member-3cf14bda5480"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the caller-owned configuration through inherited typed property metadata.

Inherited descriptors followed by this description's properties; configuration is copied when consumed.


<a id="member-3967f9fc3716"></a>
### SetAsMix()

```csharp
public System.Void SetAsMix()
```

Configures ordinary source-alpha color mixing with accumulated alpha.


<a id="member-7ee584e74eda"></a>
### AlphaBlendOp

```csharp
public Electron2D.RenderingDevice.BlendOperation AlphaBlendOp { get; set; }
```

Gets or sets the alpha combination operation.

Initially RenderingDevice.BlendOperation.Add; validated and copied by its device consumer.


<a id="member-a1b626c33bb5"></a>
### ColorBlendOp

```csharp
public Electron2D.RenderingDevice.BlendOperation ColorBlendOp { get; set; }
```

Gets or sets the RGB combination operation.

Initially RenderingDevice.BlendOperation.Add; validated and copied by its device consumer.


<a id="member-e5f4d86a4385"></a>
### DstAlphaBlendFactor

```csharp
public Electron2D.RenderingDevice.BlendFactor DstAlphaBlendFactor { get; set; }
```

Gets or sets the destination alpha factor.

Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.


<a id="member-f1b43d2124e7"></a>
### DstColorBlendFactor

```csharp
public Electron2D.RenderingDevice.BlendFactor DstColorBlendFactor { get; set; }
```

Gets or sets the destination RGB factor.

Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.


<a id="member-5eb5c2a7327e"></a>
### EnableBlend

```csharp
public System.Boolean EnableBlend { get; set; }
```

Gets or sets whether source and destination pixels blend.

Initially false; validated and copied by its device consumer.


<a id="member-78bc800a052b"></a>
### SrcAlphaBlendFactor

```csharp
public Electron2D.RenderingDevice.BlendFactor SrcAlphaBlendFactor { get; set; }
```

Gets or sets the source alpha factor.

Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.


<a id="member-e52951966c2f"></a>
### SrcColorBlendFactor

```csharp
public Electron2D.RenderingDevice.BlendFactor SrcColorBlendFactor { get; set; }
```

Gets or sets the source RGB factor.

Initially RenderingDevice.BlendFactor.Zero; validated and copied by its device consumer.


<a id="member-10d5a725b44b"></a>
### WriteA

```csharp
public System.Boolean WriteA { get; set; }
```

Gets or sets whether alpha writes are enabled.

Initially true; validated and copied by its device consumer.


<a id="member-8fc8286a03ab"></a>
### WriteB

```csharp
public System.Boolean WriteB { get; set; }
```

Gets or sets whether blue writes are enabled.

Initially true; validated and copied by its device consumer.


<a id="member-a170d26c0557"></a>
### WriteG

```csharp
public System.Boolean WriteG { get; set; }
```

Gets or sets whether green writes are enabled.

Initially true; validated and copied by its device consumer.


<a id="member-b895965ec092"></a>
### WriteR

```csharp
public System.Boolean WriteR { get; set; }
```

Gets or sets whether red writes are enabled.

Initially true; validated and copied by its device consumer.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDPipelineColorBlendStateAttachment.md) for absent roles.
