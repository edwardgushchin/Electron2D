# RenderingDevice.TextureType

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.TextureType`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Two-dimensional ordinary-image and independently sampled array roles. The names omit redundant dimensional markers. Array textures have independent layer mip chains and require a sampled array shader. Storage arrays require a native subresource-view adapter. Max cannot be selected.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.TextureType.Texture;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.TextureType Max = 7` | Nonselectable upper bound. |
| `public const Electron2D.RenderingDevice.TextureType Texture = 1` | One two-dimensional image. |
| `public const Electron2D.RenderingDevice.TextureType TextureArray = 5` | An array of two-dimensional images. |

## Member descriptions

<a id="member-99cbfa520b44"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.TextureType Max = 7
```

Nonselectable upper bound.

<a id="member-91149063848e"></a>
### Texture

```csharp
public const Electron2D.RenderingDevice.TextureType Texture = 1
```

One two-dimensional image.

<a id="member-9e21aee52784"></a>
### TextureArray

```csharp
public const Electron2D.RenderingDevice.TextureType TextureArray = 5
```

An array of two-dimensional images.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
