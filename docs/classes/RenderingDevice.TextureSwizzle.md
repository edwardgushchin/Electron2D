# RenderingDevice.TextureSwizzle

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.TextureSwizzle`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Source-channel selectors for sampled views. Identity and the matching output channel preserve current pixels. Constant components or channel reordering require a native-view adapter; TextureCreate rejects those requests explicitly.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.TextureSwizzle.A;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.TextureSwizzle A = 6` | Selects the alpha component. |
| `public const Electron2D.RenderingDevice.TextureSwizzle B = 5` | Selects the blue component. |
| `public const Electron2D.RenderingDevice.TextureSwizzle G = 4` | Selects the green component. |
| `public const Electron2D.RenderingDevice.TextureSwizzle Identity = 0` | Preserves the component owning this swizzle. |
| `public const Electron2D.RenderingDevice.TextureSwizzle Max = 7` | Nonselectable upper bound; DataFormat.Max also means no view override. |
| `public const Electron2D.RenderingDevice.TextureSwizzle One = 2` | Selects constant one; requires a view adapter. |
| `public const Electron2D.RenderingDevice.TextureSwizzle R = 3` | Selects the red component. |
| `public const Electron2D.RenderingDevice.TextureSwizzle Zero = 1` | Selects constant zero; requires a view adapter. |

## Member descriptions

<a id="member-1a87494f93ab"></a>
### A

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle A = 6
```

Selects the alpha component.

<a id="member-40f70dce15c8"></a>
### B

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle B = 5
```

Selects the blue component.

<a id="member-b3f626f71539"></a>
### G

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle G = 4
```

Selects the green component.

<a id="member-be5ea892024f"></a>
### Identity

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle Identity = 0
```

Preserves the component owning this swizzle.

<a id="member-ddc4a9063f00"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle Max = 7
```

Nonselectable upper bound; DataFormat.Max also means no view override.

<a id="member-dca932c4b075"></a>
### One

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle One = 2
```

Selects constant one; requires a view adapter.

<a id="member-198146b49141"></a>
### R

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle R = 3
```

Selects the red component.

<a id="member-c9b9e1ccf13a"></a>
### Zero

```csharp
public const Electron2D.RenderingDevice.TextureSwizzle Zero = 1
```

Selects constant zero; requires a view adapter.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
