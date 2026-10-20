# RenderingDevice.UniformType

Last updated: 2026-10-20

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.UniformType`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Descriptor-category values used by RDUniform. UniformBuffer, StorageBuffer, Image, SamplerWithTexture, Texture, Sampler and InputAttachment execute in their shader-stage roles. Separate Texture/Sampler bindings may use independent sets and reusable filters; see [sampled bindings](../components/local-compute.md#independent-sampled-resource-bindings). TextureBuffer and SamplerWithTextureBuffer execute indexed reads; ImageBuffer also executes compute writes and supported 32-bit atomics through [formatted buffers](../components/local-compute.md#formatted-texel-buffers). Fixed one-dimensional arrays execute for the eight image/sampler/texel/input categories with ordered IDs; combined categories repeat sampler/resource pairs. Ordinary uniform/storage buffers borrow one RID. See [device arrays](../components/local-compute.md#fixed-device-descriptor-arrays). Numeric vocabulary alone does not establish an executing role.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.UniformType Image = 3` | A writable image descriptor. |
| `public const Electron2D.RenderingDevice.UniformType ImageBuffer = 6` | One formatted image buffer for indexed reads, compute stores and supported 32-bit atomics. |
| `public const Electron2D.RenderingDevice.UniformType InputAttachment = 9` | A framebuffer input attachment. |
| `public const Electron2D.RenderingDevice.UniformType Sampler = 0` | A sampler descriptor. |
| `public const Electron2D.RenderingDevice.UniformType SamplerWithTexture = 1` | A combined sampled texture descriptor. |
| `public const Electron2D.RenderingDevice.UniformType SamplerWithTextureBuffer = 5` | A sampler followed by one formatted indexed buffer; fetches do not apply filtering. |
| `public const Electron2D.RenderingDevice.UniformType StorageBuffer = 8` | A read/write storage buffer. |
| `public const Electron2D.RenderingDevice.UniformType StorageBufferDynamic = 11` | A storage buffer with dynamic offset. |
| `public const Electron2D.RenderingDevice.UniformType Texture = 2` | A sampled texture descriptor. |
| `public const Electron2D.RenderingDevice.UniformType TextureBuffer = 4` | One formatted buffer for indexed shader reads. |
| `public const Electron2D.RenderingDevice.UniformType UniformBuffer = 7` | A constant uniform buffer. |
| `public const Electron2D.RenderingDevice.UniformType UniformBufferDynamic = 10` | A uniform buffer with dynamic offset. |

## Member descriptions

<a id="member-3ee38a0fbd4d"></a>
### Image

```csharp
public const Electron2D.RenderingDevice.UniformType Image = 3
```

A writable image descriptor.

<a id="member-5011cc50953c"></a>
### ImageBuffer

```csharp
public const Electron2D.RenderingDevice.UniformType ImageBuffer = 6
```

One formatted image buffer for indexed reads, compute stores and supported 32-bit atomics.

<a id="member-6215fceea5e9"></a>
### InputAttachment

```csharp
public const Electron2D.RenderingDevice.UniformType InputAttachment = 9
```

A framebuffer input attachment.

<a id="member-6b3f5907b5b9"></a>
### Sampler

```csharp
public const Electron2D.RenderingDevice.UniformType Sampler = 0
```

A sampler descriptor.

<a id="member-941bf5500329"></a>
### SamplerWithTexture

```csharp
public const Electron2D.RenderingDevice.UniformType SamplerWithTexture = 1
```

A combined sampled texture descriptor.

<a id="member-a31393963fdc"></a>
### SamplerWithTextureBuffer

```csharp
public const Electron2D.RenderingDevice.UniformType SamplerWithTextureBuffer = 5
```

A sampler followed by one formatted indexed buffer; fetches do not apply filtering.

<a id="member-c7118daed1f4"></a>
### StorageBuffer

```csharp
public const Electron2D.RenderingDevice.UniformType StorageBuffer = 8
```

A read/write storage buffer.

<a id="member-97eba755e977"></a>
### StorageBufferDynamic

```csharp
public const Electron2D.RenderingDevice.UniformType StorageBufferDynamic = 11
```

A storage buffer with dynamic offset.

<a id="member-e4f66517ccdd"></a>
### Texture

```csharp
public const Electron2D.RenderingDevice.UniformType Texture = 2
```

A sampled texture descriptor.

<a id="member-abd2f4d02c33"></a>
### TextureBuffer

```csharp
public const Electron2D.RenderingDevice.UniformType TextureBuffer = 4
```

One formatted buffer for indexed shader reads.

<a id="member-2461d0a521a0"></a>
### UniformBuffer

```csharp
public const Electron2D.RenderingDevice.UniformType UniformBuffer = 7
```

A constant uniform buffer.

<a id="member-1670b9ab27e3"></a>
### UniformBufferDynamic

```csharp
public const Electron2D.RenderingDevice.UniformType UniformBufferDynamic = 10
```

A uniform buffer with dynamic offset.

## Verification and limits

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous texture transfer APIs and additional platforms are not established by this slice.
