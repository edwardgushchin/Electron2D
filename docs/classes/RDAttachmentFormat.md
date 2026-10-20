# RDAttachmentFormat

Last updated: 2026-10-20

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDAttachmentFormat`. **Source:** [RDAttachmentFormat.cs](../../src/Servers/Rendering/Device/RDAttachmentFormat.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned color or depth/stencil attachment format description. FramebufferFormatCreate copies its encoding, required usage and sample count. ColorAttachment or DepthStencilAttachment selects the corresponding role; raster targets within each pass require the same exact sample count, while resolve destinations require one sample. Explicit ordered pass indices select render/depth/resolve/input/preserve roles. Multiview retains its native prerequisite.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var attachment = new RDAttachmentFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageFlags = RenderingDevice.TextureUsageBits.ColorAttachment };
var layout = device.FramebufferFormatCreate([attachment]);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDAttachmentFormat()` | Creates the default configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the caller-owned configuration through inherited typed property metadata. |
| `public Electron2D.RenderingDevice.DataFormat Format { get; set; }` | Gets or sets the color or depth/stencil attachment pixel format. |
| [`public Electron2D.RenderingDevice.TextureSamples Samples { get; set; }`](#member-99fbfc04c36f) | Gets or sets the exact attachment sample count. |
| `public Electron2D.RenderingDevice.TextureUsageBits UsageFlags { get; set; }` | Gets or sets the required attachment allocation and transfer roles. |

## Member descriptions

<a id="member-c82979f1c2c7"></a>
### RDAttachmentFormat()

```csharp
public RDAttachmentFormat()
```

Creates the default configuration.


<a id="member-adb9f50df96a"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the caller-owned configuration through inherited typed property metadata.

Inherited descriptors followed by this description's properties; configuration is copied when consumed.


<a id="member-393faf5a9fa1"></a>
### Format

```csharp
public Electron2D.RenderingDevice.DataFormat Format { get; set; }
```

Gets or sets the color or depth/stencil attachment pixel format.

Initially RenderingDevice.DataFormat.R8G8B8A8UNORM; validated and copied by its device consumer.


<a id="member-99fbfc04c36f"></a>
### Samples

```csharp
public Electron2D.RenderingDevice.TextureSamples Samples { get; set; }
```

Gets or sets the exact attachment sample count.

Initially RenderingDevice.TextureSamples.Samples1; validated and copied by its device consumer.
<a id="member-88c981d37175"></a>
### UsageFlags

```csharp
public Electron2D.RenderingDevice.TextureUsageBits UsageFlags { get; set; }
```

Gets or sets the required attachment allocation and transfer roles.

Initially 0; validated and copied by its device consumer.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDAttachmentFormat.md) for absent roles.

The three typed values are copied into single/multipass and layered framebuffer formats. Later edits or disposal of this description do not change those formats. The framebuffer factory owns the view count separately; see [layered multiview](../components/local-compute.md#layered-multiview).
