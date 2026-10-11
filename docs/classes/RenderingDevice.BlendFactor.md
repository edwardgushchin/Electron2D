# RenderingDevice.BlendFactor

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.BlendFactor`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies BlendFactor values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.BlendFactor ConstantColor = 10` | Selects ConstantColor. |
| `public const Electron2D.RenderingDevice.BlendFactor DstAlpha = 8` | Selects DstAlpha. |
| `public const Electron2D.RenderingDevice.BlendFactor DstColor = 4` | Selects DstColor. |
| `public const Electron2D.RenderingDevice.BlendFactor Max = 19` | Marks the exclusive numeric extent; it is not an executable selection. |
| `public const Electron2D.RenderingDevice.BlendFactor One = 1` | Selects One. |
| `public const Electron2D.RenderingDevice.BlendFactor OneMinusConstantColor = 11` | Selects OneMinusConstantColor. |
| `public const Electron2D.RenderingDevice.BlendFactor OneMinusDstAlpha = 9` | Selects OneMinusDstAlpha. |
| `public const Electron2D.RenderingDevice.BlendFactor OneMinusDstColor = 5` | Selects OneMinusDstColor. |
| `public const Electron2D.RenderingDevice.BlendFactor OneMinusSrcAlpha = 7` | Selects OneMinusSrcAlpha. |
| `public const Electron2D.RenderingDevice.BlendFactor OneMinusSrcColor = 3` | Selects OneMinusSrcColor. |
| `public const Electron2D.RenderingDevice.BlendFactor SrcAlpha = 6` | Selects SrcAlpha. |
| `public const Electron2D.RenderingDevice.BlendFactor SrcAlphaSaturate = 14` | Selects SrcAlphaSaturate. |
| `public const Electron2D.RenderingDevice.BlendFactor SrcColor = 2` | Selects SrcColor. |
| `public const Electron2D.RenderingDevice.BlendFactor Zero = 0` | Selects Zero. |

## Member descriptions

<a id="member-c800858d3479"></a>
### ConstantColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor ConstantColor = 10
```

Selects ConstantColor.


<a id="member-35ecc4b4996a"></a>
### DstAlpha

```csharp
public const Electron2D.RenderingDevice.BlendFactor DstAlpha = 8
```

Selects DstAlpha.


<a id="member-1495343ac919"></a>
### DstColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor DstColor = 4
```

Selects DstColor.


<a id="member-6f28cae8d59f"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.BlendFactor Max = 19
```

Marks the exclusive numeric extent; it is not an executable selection.


<a id="member-e7d7e26b416d"></a>
### One

```csharp
public const Electron2D.RenderingDevice.BlendFactor One = 1
```

Selects One.


<a id="member-5d8e4b2b7810"></a>
### OneMinusConstantColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor OneMinusConstantColor = 11
```

Selects OneMinusConstantColor.


<a id="member-96d21a7e2cc1"></a>
### OneMinusDstAlpha

```csharp
public const Electron2D.RenderingDevice.BlendFactor OneMinusDstAlpha = 9
```

Selects OneMinusDstAlpha.


<a id="member-48691c68f6a4"></a>
### OneMinusDstColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor OneMinusDstColor = 5
```

Selects OneMinusDstColor.


<a id="member-ff251855a9d2"></a>
### OneMinusSrcAlpha

```csharp
public const Electron2D.RenderingDevice.BlendFactor OneMinusSrcAlpha = 7
```

Selects OneMinusSrcAlpha.


<a id="member-2dec27a66f40"></a>
### OneMinusSrcColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor OneMinusSrcColor = 3
```

Selects OneMinusSrcColor.


<a id="member-007f89d1149c"></a>
### SrcAlpha

```csharp
public const Electron2D.RenderingDevice.BlendFactor SrcAlpha = 6
```

Selects SrcAlpha.


<a id="member-20161340ddf4"></a>
### SrcAlphaSaturate

```csharp
public const Electron2D.RenderingDevice.BlendFactor SrcAlphaSaturate = 14
```

Selects SrcAlphaSaturate.


<a id="member-8c5828c2c0c2"></a>
### SrcColor

```csharp
public const Electron2D.RenderingDevice.BlendFactor SrcColor = 2
```

Selects SrcColor.


<a id="member-c39884f473a1"></a>
### Zero

```csharp
public const Electron2D.RenderingDevice.BlendFactor Zero = 0
```

Selects Zero.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
