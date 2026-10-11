# RenderingDevice.SamplerFilter

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.SamplerFilter`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Nearest selects one texel/mip; Linear interpolates neighboring texels or adjacent mips. MinFilter and MagFilter govern sampling within a mip; MipFilter governs neighboring mip levels. Integer images require nearest filters and no anisotropy.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.SamplerFilter.Linear;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.SamplerFilter Linear = 1` | Interpolates neighboring texels or adjacent mip levels. |
| `public const Electron2D.RenderingDevice.SamplerFilter Nearest = 0` | Selects the nearest texel or mip level. |

## Member descriptions

<a id="member-247692d986a4"></a>
### Linear

```csharp
public const Electron2D.RenderingDevice.SamplerFilter Linear = 1
```

Interpolates neighboring texels or adjacent mip levels.

<a id="member-3aad44ca5e75"></a>
### Nearest

```csharp
public const Electron2D.RenderingDevice.SamplerFilter Nearest = 0
```

Selects the nearest texel or mip level.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
