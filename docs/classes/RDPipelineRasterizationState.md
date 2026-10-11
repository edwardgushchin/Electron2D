# RDPipelineRasterizationState

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineRasterizationState`. **Source:** [RDPipelineRasterizationState.cs](../../src/Servers/Rendering/Device/RDPipelineRasterizationState.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned description copied by RenderPipelineCreate. Face culling, front-facing winding, wireframe and clip-depth clamping execute in the native rasterizer. Invalid selectors fail before native pipeline creation. Device capability errors propagate. Depth bias and other raster states remain precise attachment/backend prerequisites.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var raster = new RDPipelineRasterizationState { CullMode = RenderingDevice.PolygonCullMode.Back };
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDPipelineRasterizationState()` | Creates the default configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the caller-owned configuration through inherited typed property metadata. |
| `public Electron2D.RenderingDevice.PolygonCullMode CullMode { get; set; }` | Gets or sets the face culling selection. |
| `public System.Boolean EnableDepthClamp { get; set; }` | Gets or sets depth clamping rather than clip-depth rejection. |
| `public Electron2D.RenderingDevice.PolygonFrontFace FrontFace { get; set; }` | Gets or sets the winding treated as front-facing. |
| `public System.Boolean Wireframe { get; set; }` | Gets or sets outline rather than filled rasterization. |

## Member descriptions

<a id="member-35065472c003"></a>
### RDPipelineRasterizationState()

```csharp
public RDPipelineRasterizationState()
```

Creates the default configuration.


<a id="member-2603f8905045"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the caller-owned configuration through inherited typed property metadata.

Inherited descriptors followed by this description's properties; configuration is copied when consumed.


<a id="member-52a815bc9638"></a>
### CullMode

```csharp
public Electron2D.RenderingDevice.PolygonCullMode CullMode { get; set; }
```

Gets or sets the face culling selection.

Initially RenderingDevice.PolygonCullMode.Disabled; validated and copied by its device consumer.


<a id="member-0249a8f655fc"></a>
### EnableDepthClamp

```csharp
public System.Boolean EnableDepthClamp { get; set; }
```

Gets or sets depth clamping rather than clip-depth rejection.

Initially false; validated and copied by its device consumer.


<a id="member-ecfde3f806ff"></a>
### FrontFace

```csharp
public Electron2D.RenderingDevice.PolygonFrontFace FrontFace { get; set; }
```

Gets or sets the winding treated as front-facing.

Initially RenderingDevice.PolygonFrontFace.Clockwise; validated and copied by its device consumer.


<a id="member-b33a02d6143b"></a>
### Wireframe

```csharp
public System.Boolean Wireframe { get; set; }
```

Gets or sets outline rather than filled rasterization.

Initially false; validated and copied by its device consumer.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDPipelineRasterizationState.md) for absent roles.
