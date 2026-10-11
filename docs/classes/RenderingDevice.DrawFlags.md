# RenderingDevice.DrawFlags

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.DrawFlags`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs).

## Description

Identifies DrawFlags values for executable graphics operations.



## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
var flags = RenderingDevice.DrawFlags.ClearAll;
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearAll = 327935`](#member-047f952f6e3d) | Clears every color, depth and stencil aspect; absent aspects are ignored. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor0 = 1`](#member-75fbee59625b) | Selects ClearColor0. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor1 = 2`](#member-bfb195345dfd) | Selects ClearColor1. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor2 = 4`](#member-c787290c5683) | Selects ClearColor2. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor3 = 8`](#member-df1512a32cd4) | Selects ClearColor3. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor4 = 16`](#member-0d21a408fbe6) | Selects ClearColor4. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor5 = 32`](#member-0021eb6ec792) | Selects ClearColor5. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor6 = 64`](#member-a4418e178377) | Selects ClearColor6. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColor7 = 128`](#member-1f7d3a79e85a) | Selects ClearColor7. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColorAll = 255`](#member-7188212b3585) | Selects ClearColorAll. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearColorMask = 255`](#member-918b95e7f21b) | Masks the per-color clear flags. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearDepth = 65536`](#member-2d63434fe6da) | Clears the complete depth aspect. |
| [`public const Electron2D.RenderingDevice.DrawFlags ClearStencil = 262144`](#member-154953cb1919) | Clears the complete stencil aspect. |
| [`public const Electron2D.RenderingDevice.DrawFlags DefaultAll = 0`](#member-43f8831b8ef1) | Selects DefaultAll. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreAll = 720640`](#member-3ee01722b432) | Discards every color, depth and stencil aspect; absent aspects are ignored. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor0 = 256`](#member-2ecd4b28b086) | Selects IgnoreColor0. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor1 = 512`](#member-75103d97dd4e) | Selects IgnoreColor1. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor2 = 1024`](#member-6b6c5e6ec03b) | Selects IgnoreColor2. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor3 = 2048`](#member-cde070718775) | Selects IgnoreColor3. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor4 = 4096`](#member-94f13e670c27) | Selects IgnoreColor4. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor5 = 8192`](#member-310ab3cd335a) | Selects IgnoreColor5. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor6 = 16384`](#member-7a0344aa268f) | Selects IgnoreColor6. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColor7 = 32768`](#member-9735de6cdd8d) | Selects IgnoreColor7. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColorAll = 65280`](#member-893cab40c205) | Selects IgnoreColorAll. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreColorMask = 65280`](#member-77223ccf4045) | Masks the per-color discard flags. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreDepth = 131072`](#member-7afa66c8ac6e) | Discards existing depth before the pass. |
| [`public const Electron2D.RenderingDevice.DrawFlags IgnoreStencil = 524288`](#member-9161c88f9150) | Discards existing stencil before the pass. |

## Member descriptions

<a id="member-047f952f6e3d"></a>
### ClearAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearAll = 327935
```

Clears every color, depth and stencil aspect; absent aspects are ignored.

<a id="member-75fbee59625b"></a>
### ClearColor0

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor0 = 1
```

Selects ClearColor0.

<a id="member-bfb195345dfd"></a>
### ClearColor1

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor1 = 2
```

Selects ClearColor1.

<a id="member-c787290c5683"></a>
### ClearColor2

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor2 = 4
```

Selects ClearColor2.

<a id="member-df1512a32cd4"></a>
### ClearColor3

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor3 = 8
```

Selects ClearColor3.

<a id="member-0d21a408fbe6"></a>
### ClearColor4

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor4 = 16
```

Selects ClearColor4.

<a id="member-0021eb6ec792"></a>
### ClearColor5

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor5 = 32
```

Selects ClearColor5.

<a id="member-a4418e178377"></a>
### ClearColor6

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor6 = 64
```

Selects ClearColor6.

<a id="member-1f7d3a79e85a"></a>
### ClearColor7

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor7 = 128
```

Selects ClearColor7.

<a id="member-7188212b3585"></a>
### ClearColorAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColorAll = 255
```

Selects ClearColorAll.

<a id="member-918b95e7f21b"></a>
### ClearColorMask

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColorMask = 255
```

Masks the per-color clear flags.

<a id="member-2d63434fe6da"></a>
### ClearDepth

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearDepth = 65536
```

Clears the complete depth aspect.

<a id="member-154953cb1919"></a>
### ClearStencil

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearStencil = 262144
```

Clears the complete stencil aspect.

<a id="member-43f8831b8ef1"></a>
### DefaultAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags DefaultAll = 0
```

Selects DefaultAll.

<a id="member-3ee01722b432"></a>
### IgnoreAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreAll = 720640
```

Discards every color, depth and stencil aspect; absent aspects are ignored.

<a id="member-2ecd4b28b086"></a>
### IgnoreColor0

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor0 = 256
```

Selects IgnoreColor0.

<a id="member-75103d97dd4e"></a>
### IgnoreColor1

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor1 = 512
```

Selects IgnoreColor1.

<a id="member-6b6c5e6ec03b"></a>
### IgnoreColor2

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor2 = 1024
```

Selects IgnoreColor2.

<a id="member-cde070718775"></a>
### IgnoreColor3

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor3 = 2048
```

Selects IgnoreColor3.

<a id="member-94f13e670c27"></a>
### IgnoreColor4

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor4 = 4096
```

Selects IgnoreColor4.

<a id="member-310ab3cd335a"></a>
### IgnoreColor5

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor5 = 8192
```

Selects IgnoreColor5.

<a id="member-7a0344aa268f"></a>
### IgnoreColor6

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor6 = 16384
```

Selects IgnoreColor6.

<a id="member-9735de6cdd8d"></a>
### IgnoreColor7

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor7 = 32768
```

Selects IgnoreColor7.

<a id="member-893cab40c205"></a>
### IgnoreColorAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColorAll = 65280
```

Selects IgnoreColorAll.

<a id="member-77223ccf4045"></a>
### IgnoreColorMask

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColorMask = 65280
```

Masks the per-color discard flags.

<a id="member-7afa66c8ac6e"></a>
### IgnoreDepth

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreDepth = 131072
```

Discards existing depth before the pass.

<a id="member-9161c88f9150"></a>
### IgnoreStencil

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreStencil = 524288
```

Discards existing stencil before the pass.

## Lifecycle, errors and verification

These values execute in the owning pipeline, sampler or draw pass. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
