# RDFramebufferPass

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDFramebufferPass`. **Component:** [Local device](../components/local-compute.md#multisample-graphics-and-resolve).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Describes a copied color/depth pass and its multisample resolve destinations.

Color indices follow shader output order; depth and resolve indices refer to the supplied attachment array. AttachmentUnused disables depth or individual resolves. Empty resolves select a stored render-only pass. Arrays copy writes and reads; native creation copies all fields. Dense color slots execute; sparse color holes, input/preserve attachments and multiple subpasses require adapters.

## Example

Partial configuration; [the executable public consumer](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) supplies device-owned images and shader modules.

```csharp
using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] };
var target = device.FramebufferCreateMultipass([multisampleColor, resolveColor], [pass]);
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const System.Int32 AttachmentUnused = -1`](#member-c9289044de4e) | Marks an unused depth attachment or color resolve slot. |
| [`public RDFramebufferPass()`](#member-ca2b8c572f89) | Creates the default configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-9dca4417ba9f) | Projects inherited metadata followed by the executing configuration fields. |
| [`public System.Int32[] ColorAttachments { get; set; }`](#member-1c5793321e5e) | Selects color attachment indices in shader output order. |
| [`public System.Int32 DepthAttachment { get; set; }`](#member-fde6f81eda57) | Selects the depth/stencil attachment index, or AttachmentUnused. |
| [`public System.Int32[] ResolveAttachments { get; set; }`](#member-11934a339e55) | Selects one resolve destination index per color slot, or AttachmentUnused. |

## Member descriptions

<a id="member-c9289044de4e"></a>
### AttachmentUnused

```csharp
public const System.Int32 AttachmentUnused = -1
```

Marks an unused depth attachment or color resolve slot.

<a id="member-ca2b8c572f89"></a>
### RDFramebufferPass()

```csharp
public RDFramebufferPass()
```

Creates the default configuration.

<a id="member-9dca4417ba9f"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects inherited metadata followed by the executing configuration fields.

Typed configuration descriptors with their default values.

<a id="member-1c5793321e5e"></a>
### ColorAttachments

```csharp
public System.Int32[] ColorAttachments { get; set; }
```

Selects color attachment indices in shader output order.

Initially []; copied when the device consumes this configuration.

<a id="member-fde6f81eda57"></a>
### DepthAttachment

```csharp
public System.Int32 DepthAttachment { get; set; }
```

Selects the depth/stencil attachment index, or AttachmentUnused.

Initially -1; copied when the device consumes this configuration.

<a id="member-11934a339e55"></a>
### ResolveAttachments

```csharp
public System.Int32[] ResolveAttachments { get; set; }
```

Selects one resolve destination index per color slot, or AttachmentUnused.

Initially []; copied when the device consumes this configuration.

## Lifecycle, errors and verification

Descriptions borrow no native resources and can be disposed after creation. Invalid indices, repeated roles, mismatched formats/dimensions/sample counts and unavailable native counts reject before framebuffer or pipeline creation. Device RIDs retain owner-thread lifetime and feedback validation. [RenderingDeviceMultisampleTests](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) verifies native pixels, resolve/clear/canvas integration and warmed managed allocation on Linux/Vulkan. Foreign platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
