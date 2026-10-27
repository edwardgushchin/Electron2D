# RDFramebufferPass

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDFramebufferPass`. **Component:** [Local device](../components/local-compute.md#multisample-graphics-and-resolve).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Describes an ordered color/depth pass, its resolve destinations, inputs and preserved attachments.

Color indices follow shader output order; depth and resolve indices refer to the supplied attachment array. AttachmentUnused disables individual color, depth or resolve slots. Empty resolves select a stored render-only pass. Arrays copy writes and reads; native creation copies all fields. Ordered passes execute input loads and preservation across inactive intermediate passes. Inputs allow AttachmentUnused holes; duplicate preserves are idempotent. Sparse color output slots execute through private native output remapping. See [ordered passes](../components/local-compute.md#ordered-framebuffer-passes-and-inputs) for allocation and shader limits.

## Example

Executable configuration; [the executable public consumer](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) supplies device-owned images and shader modules.

```csharp
using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] };
var target = device.FramebufferCreateMultipass([multisampleColor, resolveColor], [pass]);
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const System.Int32 AttachmentUnused = -1`](#member-c9289044de4e) | Marks an unused color, depth, resolve or input slot. |
| [`public RDFramebufferPass()`](#member-ca2b8c572f89) | Creates the default configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-9dca4417ba9f) | Projects inherited metadata followed by the executing configuration fields. |
| [`public System.Int32[] ColorAttachments { get; set; }`](#member-1c5793321e5e) | Selects color attachment indices in logical shader output order; AttachmentUnused skips a location. |
| [`public System.Int32 DepthAttachment { get; set; }`](#member-fde6f81eda57) | Selects the depth/stencil attachment index, or AttachmentUnused. |
| [`public System.Int32[] ResolveAttachments { get; set; }`](#member-11934a339e55) | Selects one resolve destination index per color slot, or AttachmentUnused. |
| [`public System.Int32[] InputAttachments { get; set; }`](#member-input-attachments) | Selects input attachment indices in shader input-attachment-index order. |
| [`public System.Int32[] PreserveAttachments { get; set; }`](#member-preserve-attachments) | Selects attachments whose contents survive this pass without active use. |

## Member descriptions

<a id="member-c9289044de4e"></a>
### AttachmentUnused

```csharp
public const System.Int32 AttachmentUnused = -1
```

Marks an unused color, depth, resolve or input slot.

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

Selects color attachment indices in logical shader output order; AttachmentUnused skips a location.

Initially []; reads and writes copy. Active slots must exactly match fragment outputs; at least one color or depth target is required.

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


<a id="member-input-attachments"></a>
### InputAttachments

```csharp
public System.Int32[] InputAttachments { get; set; }
```

Selects input attachment indices in shader input-attachment-index order.

Initially empty. Reads and writes copy. AttachmentUnused leaves an unbound input slot.


<a id="member-preserve-attachments"></a>
### PreserveAttachments

```csharp
public System.Int32[] PreserveAttachments { get; set; }
```

Selects attachments whose contents survive this pass without active use.

Initially empty. Reads and writes copy. Indices are validated at creation; duplicate preserves are idempotent.

## Lifecycle, errors and verification

Descriptions borrow no native resources and can be disposed after creation. Invalid indices, repeated roles, mismatched formats/dimensions/sample counts and unavailable native counts reject before framebuffer or pipeline creation. Device RIDs retain owner-thread lifetime and active-pass feedback validation. RenderingDevicePassTests verifies single/multisample inputs, exact integer loads, preservation, copied metadata and errors plus prepared zero managed owner bytes; its canvas profile checks retained output pixels. [RenderingDeviceMultisampleTests](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) verifies native pixels, resolve/clear/canvas integration and warmed managed allocation on Linux/Vulkan. Foreign platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).

[Sparse output verification](../components/local-compute.md#sparse-framebuffer-outputs) also covers native output mapping and its ownership/allocation limits.

The internal [device cache](../components/device-resource-caches.md) readers borrow validated description spans for key comparisons. Copied cache keys never retain these description objects or expose their mutable storage.
