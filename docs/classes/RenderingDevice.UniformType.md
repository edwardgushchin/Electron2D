# RenderingDevice.UniformType

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.UniformType`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Descriptor-category values used by RDUniform. Numeric identities are retained; local compute accepts UniformBuffer and StorageBuffer bindings and explicitly rejects other categories.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.UniformType Image = 3` | A writable image descriptor. |
| `public const Electron2D.RenderingDevice.UniformType ImageBuffer = 6` | A writable image buffer descriptor. |
| `public const Electron2D.RenderingDevice.UniformType InputAttachment = 9` | A framebuffer input attachment. |
| `public const Electron2D.RenderingDevice.UniformType Sampler = 0` | A sampler descriptor. |
| `public const Electron2D.RenderingDevice.UniformType SamplerWithTexture = 1` | A combined sampled texture descriptor. |
| `public const Electron2D.RenderingDevice.UniformType SamplerWithTextureBuffer = 5` | A sampled texture buffer descriptor. |
| `public const Electron2D.RenderingDevice.UniformType StorageBuffer = 8` | A read/write storage buffer. |
| `public const Electron2D.RenderingDevice.UniformType StorageBufferDynamic = 11` | A storage buffer with dynamic offset. |
| `public const Electron2D.RenderingDevice.UniformType Texture = 2` | A sampled texture descriptor. |
| `public const Electron2D.RenderingDevice.UniformType TextureBuffer = 4` | A texture buffer descriptor. |
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

A writable image buffer descriptor.

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

A sampled texture buffer descriptor.

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

A texture buffer descriptor.

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

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous transfer APIs and additional platforms are not established by this slice.
