# RenderingDevice.SamplerRepeatMode

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.SamplerRepeatMode`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Normalized-coordinate addressing. Repeat wraps each interval; MirroredRepeat alternates its direction; ClampToEdge selects the nearest edge texel. ClampToBorder uses the native Vulkan adapter. MirrorClampToEdge mirrors negative coordinates once, then clamps; it requires the enabled native extension. See [mirror-clamp sampling](../components/sampler-mirror-clamp.md). Max cannot be selected.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.SamplerRepeatMode.ClampToBorder;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode ClampToBorder = 3` | Uses the configured border color outside the image; requires the native Vulkan sampler adapter. |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode ClampToEdge = 2` | Uses the closest edge texel outside the image. |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode Max = 5` | Nonselectable upper bound. |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode MirrorClampToEdge = 4` | Mirrors negative coordinates once, then clamps; requires the negotiated Vulkan mirror-clamp extension. |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode MirroredRepeat = 1` | Repeats the image with alternating mirrored intervals. |
| `public const Electron2D.RenderingDevice.SamplerRepeatMode Repeat = 0` | Repeats the image in every normalized coordinate interval. |

## Member descriptions

<a id="member-95cb1f1b2282"></a>
### ClampToBorder

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode ClampToBorder = 3
```

Uses the configured border color outside the image; requires the native Vulkan sampler adapter.

<a id="member-4c0b98d709ef"></a>
### ClampToEdge

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode ClampToEdge = 2
```

Uses the closest edge texel outside the image.

<a id="member-cae9659c9161"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode Max = 5
```

Nonselectable upper bound.

<a id="member-b4e2a386c052"></a>
### MirrorClampToEdge

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode MirrorClampToEdge = 4
```

Mirrors negative coordinates once, then clamps; requires the negotiated Vulkan mirror-clamp extension.

<a id="member-9698d5250477"></a>
### MirroredRepeat

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode MirroredRepeat = 1
```

Repeats the image with alternating mirrored intervals.

<a id="member-cd628c54065f"></a>
### Repeat

```csharp
public const Electron2D.RenderingDevice.SamplerRepeatMode Repeat = 0
```

Repeats the image in every normalized coordinate interval.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
