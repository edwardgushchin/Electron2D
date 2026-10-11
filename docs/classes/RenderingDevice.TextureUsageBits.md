# RenderingDevice.TextureUsageBits

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.TextureUsageBits`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Bit flags describing allocation, sampling, storage and transfer roles. Sampling and Storage are checked against the native format. CPURead or CanCopyFrom permits readback; CanUpdate permits a complete layer update. Regional copy/clear remain follow-up implementation. Attachment/atomic/subpass roles are rejected by the current local compute profile.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.TextureUsageBits.CPURead;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.TextureUsageBits CPURead = 32` | Allows explicit synchronous or queued CPU readback. |
| `public const Electron2D.RenderingDevice.TextureUsageBits CanCopyFrom = 128` | Allows use as a copy source or CPU readback source. |
| `public const Electron2D.RenderingDevice.TextureUsageBits CanCopyTo = 256` | Allows use as a copy destination; regional copies require copy integration. |
| `public const Electron2D.RenderingDevice.TextureUsageBits CanUpdate = 64` | Allows replacement of one complete layer mip chain. |
| `public const Electron2D.RenderingDevice.TextureUsageBits ColorAttachment = 2` | Requires a color attachment; local graphics integration is not implemented. |
| `public const Electron2D.RenderingDevice.TextureUsageBits DepthResolveAttachment = 4096` | Requires depth resolve attachment integration. |
| `public const Electron2D.RenderingDevice.TextureUsageBits DepthStencilAttachment = 4` | Requires depth/stencil attachment and transfer integration. |
| `public const Electron2D.RenderingDevice.TextureUsageBits InputAttachment = 512` | Requires graphics subpass input integration. |
| `public const Electron2D.RenderingDevice.TextureUsageBits Sampling = 1` | Allows shader sampling with a sampler. |
| `public const Electron2D.RenderingDevice.TextureUsageBits Storage = 8` | Allows read/write compute access where the format supports simultaneous access. |
| `public const Electron2D.RenderingDevice.TextureUsageBits StorageAtomic = 16` | Requires atomic image access not exposed by this backend profile. |

## Member descriptions

<a id="member-9542be8239f3"></a>
### CPURead

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits CPURead = 32
```

Allows explicit synchronous or queued CPU readback.

<a id="member-45d8e3902c32"></a>
### CanCopyFrom

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits CanCopyFrom = 128
```

Allows use as a copy source or CPU readback source.

<a id="member-e056f14aca55"></a>
### CanCopyTo

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits CanCopyTo = 256
```

Allows use as a copy destination; regional copies require copy integration.

<a id="member-6981f07b351a"></a>
### CanUpdate

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits CanUpdate = 64
```

Allows replacement of one complete layer mip chain.

<a id="member-3f66836e470f"></a>
### ColorAttachment

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits ColorAttachment = 2
```

Requires a color attachment; local graphics integration is not implemented.

<a id="member-7e65576ba991"></a>
### DepthResolveAttachment

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits DepthResolveAttachment = 4096
```

Requires depth resolve attachment integration.

<a id="member-9fd462bf597b"></a>
### DepthStencilAttachment

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits DepthStencilAttachment = 4
```

Requires depth/stencil attachment and transfer integration.

<a id="member-be3f7afb9750"></a>
### InputAttachment

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits InputAttachment = 512
```

Requires graphics subpass input integration.

<a id="member-0fdac8edb6d3"></a>
### Sampling

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits Sampling = 1
```

Allows shader sampling with a sampler.

<a id="member-e4ea073db371"></a>
### Storage

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits Storage = 8
```

Allows read/write compute access where the format supports simultaneous access.

<a id="member-46c497bed907"></a>
### StorageAtomic

```csharp
public const Electron2D.RenderingDevice.TextureUsageBits StorageAtomic = 16
```

Requires atomic image access not exposed by this backend profile.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
