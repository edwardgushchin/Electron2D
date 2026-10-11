# RenderingDevice.RenderPrimitive

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.RenderPrimitive`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies RenderPrimitive values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.RenderPrimitive LineStrips = 3` | Selects LineStrips. |
| `public const Electron2D.RenderingDevice.RenderPrimitive Lines = 1` | Selects Lines. |
| `public const Electron2D.RenderingDevice.RenderPrimitive Max = 11` | Marks the exclusive numeric extent; it is not an executable selection. |
| `public const Electron2D.RenderingDevice.RenderPrimitive Points = 0` | Selects Points. |
| `public const Electron2D.RenderingDevice.RenderPrimitive TriangleStrips = 7` | Selects TriangleStrips. |
| `public const Electron2D.RenderingDevice.RenderPrimitive TriangleStripsWithRestartIndex = 9` | Selects TriangleStripsWithRestartIndex. |
| `public const Electron2D.RenderingDevice.RenderPrimitive Triangles = 5` | Selects Triangles. |

## Member descriptions

<a id="member-e87a6e95b1f3"></a>
### LineStrips

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive LineStrips = 3
```

Selects LineStrips.


<a id="member-2561347ba437"></a>
### Lines

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive Lines = 1
```

Selects Lines.


<a id="member-72fbc55f8d7f"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive Max = 11
```

Marks the exclusive numeric extent; it is not an executable selection.


<a id="member-a3700fbd7353"></a>
### Points

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive Points = 0
```

Selects Points.


<a id="member-b1d825d18c17"></a>
### TriangleStrips

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive TriangleStrips = 7
```

Selects TriangleStrips.


<a id="member-2398200aec2d"></a>
### TriangleStripsWithRestartIndex

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive TriangleStripsWithRestartIndex = 9
```

Selects TriangleStripsWithRestartIndex.


<a id="member-e8da0ceaff76"></a>
### Triangles

```csharp
public const Electron2D.RenderingDevice.RenderPrimitive Triangles = 5
```

Selects Triangles.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
