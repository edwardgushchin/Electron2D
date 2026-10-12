# RenderingDevice.TextureSamples

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.TextureSamples`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies native raster sample counts. Numeric identities are retained. Unknown or extent selectors reject when consumed. One sample executes directly; two/four/eight are queried per format and reject when unavailable. Sixteen/thirty-two/sixty-four require an additional backend adapter and reject without downgrading. Raster targets and pipelines must agree; resolves use single-sample destinations.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.TextureSamples Max = 7` | Marks the exclusive numeric extent; it is not an executable selection. |
| `public const Electron2D.RenderingDevice.TextureSamples Samples1 = 0` | One sample per pixel. |
| `public const Electron2D.RenderingDevice.TextureSamples Samples2 = 1` | Two samples per pixel. |
| `public const Electron2D.RenderingDevice.TextureSamples Samples4 = 2` | Four samples per pixel. |
| `public const Electron2D.RenderingDevice.TextureSamples Samples8 = 3` | Eight samples per pixel. |
| [`public const Electron2D.RenderingDevice.TextureSamples Samples16 = 4`](#member-b88c86091bfd) | Sixteen samples; requires an available native sample-count adapter. |
| [`public const Electron2D.RenderingDevice.TextureSamples Samples32 = 5`](#member-2bfff3ab0867) | Thirty-two samples; requires an available native sample-count adapter. |
| [`public const Electron2D.RenderingDevice.TextureSamples Samples64 = 6`](#member-0c89ae1e8a7e) | Sixty-four samples; requires an available native sample-count adapter. |

## Member descriptions

<a id="member-aa93d81c8f81"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.TextureSamples Max = 7
```

Marks the exclusive numeric extent; it is not an executable selection.


<a id="member-d4147d565da4"></a>
### Samples1

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples1 = 0
```

One sample per pixel.


<a id="member-f07d7105a574"></a>
### Samples2

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples2 = 1
```

Two samples per pixel.


<a id="member-ac2c9c526ce0"></a>
### Samples4

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples4 = 2
```

Four samples per pixel.


<a id="member-a5802e33cfb1"></a>
### Samples8

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples8 = 3
```

Eight samples per pixel.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.

<a id="member-b88c86091bfd"></a>
### Samples16

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples16 = 4
```

Sixteen samples; requires an available native sample-count adapter.


<a id="member-2bfff3ab0867"></a>
### Samples32

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples32 = 5
```

Thirty-two samples; requires an available native sample-count adapter.


<a id="member-0c89ae1e8a7e"></a>
### Samples64

```csharp
public const Electron2D.RenderingDevice.TextureSamples Samples64 = 6
```

Sixty-four samples; requires an available native sample-count adapter.
