# RenderingDevice.StencilOperation

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.StencilOperation`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RenderingDevice.DepthStencil.cs](../../src/Servers/Rendering/Device/RenderingDevice.DepthStencil.cs).

## Description

Selects the eight-bit stencil update after comparison.



## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
var operation = RenderingDevice.StencilOperation.Replace;
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.RenderingDevice.StencilOperation DecrementAndClamp = 4`](#member-63d3843a3d81) | Decrements with saturation. |
| [`public const Electron2D.RenderingDevice.StencilOperation DecrementAndWrap = 7`](#member-a777874fbe8b) | Decrements modulo 256. |
| [`public const Electron2D.RenderingDevice.StencilOperation IncrementAndClamp = 3`](#member-162fc1d8e11f) | Increments with saturation. |
| [`public const Electron2D.RenderingDevice.StencilOperation IncrementAndWrap = 6`](#member-710609c58c46) | Increments modulo 256. |
| [`public const Electron2D.RenderingDevice.StencilOperation Invert = 5`](#member-45bc130703b6) | Inverts all bits. |
| [`public const Electron2D.RenderingDevice.StencilOperation Keep = 0`](#member-1623156146b0) | Preserves the value. |
| [`public const Electron2D.RenderingDevice.StencilOperation Max = 8`](#member-ca09fb94d47f) | Exclusive nonselectable bound. |
| [`public const Electron2D.RenderingDevice.StencilOperation Replace = 2`](#member-0ad75a47b64b) | Writes the reference. |
| [`public const Electron2D.RenderingDevice.StencilOperation Zero = 1`](#member-55bf70561080) | Writes zero. |

## Member descriptions

<a id="member-63d3843a3d81"></a>
### DecrementAndClamp

```csharp
public const Electron2D.RenderingDevice.StencilOperation DecrementAndClamp = 4
```

Decrements with saturation.

<a id="member-a777874fbe8b"></a>
### DecrementAndWrap

```csharp
public const Electron2D.RenderingDevice.StencilOperation DecrementAndWrap = 7
```

Decrements modulo 256.

<a id="member-162fc1d8e11f"></a>
### IncrementAndClamp

```csharp
public const Electron2D.RenderingDevice.StencilOperation IncrementAndClamp = 3
```

Increments with saturation.

<a id="member-710609c58c46"></a>
### IncrementAndWrap

```csharp
public const Electron2D.RenderingDevice.StencilOperation IncrementAndWrap = 6
```

Increments modulo 256.

<a id="member-45bc130703b6"></a>
### Invert

```csharp
public const Electron2D.RenderingDevice.StencilOperation Invert = 5
```

Inverts all bits.

<a id="member-1623156146b0"></a>
### Keep

```csharp
public const Electron2D.RenderingDevice.StencilOperation Keep = 0
```

Preserves the value.

<a id="member-ca09fb94d47f"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.StencilOperation Max = 8
```

Exclusive nonselectable bound.

<a id="member-0ad75a47b64b"></a>
### Replace

```csharp
public const Electron2D.RenderingDevice.StencilOperation Replace = 2
```

Writes the reference.

<a id="member-55bf70561080"></a>
### Zero

```csharp
public const Electron2D.RenderingDevice.StencilOperation Zero = 1
```

Writes zero.

## Lifecycle, errors and verification

These values execute in the owning pipeline, sampler or draw pass. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
