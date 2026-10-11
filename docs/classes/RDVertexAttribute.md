# RDVertexAttribute

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDVertexAttribute`. **Source:** [RDVertexAttribute.cs](../../src/Servers/Rendering/Device/RDVertexAttribute.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned description of a vertex input location, format and record layout. VertexFormatCreate validates at most sixteen unique locations and contiguous buffer bindings, with positive byte stride and consistent frequency per binding. Invalid/default layouts fail before native allocation. The device copies values; later mutation or disposal cannot change a created layout.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var attribute = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 8, Format = RenderingDevice.DataFormat.R32G32SFLOAT };
var layout = device.VertexFormatCreate([attribute]);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDVertexAttribute()` | Creates the default configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the caller-owned configuration through inherited typed property metadata. |
| `public System.UInt32 Binding { get; set; }` | Gets or sets the zero-based source buffer binding. |
| `public Electron2D.RenderingDevice.DataFormat Format { get; set; }` | Gets or sets the encoded vertex field format. |
| `public Electron2D.RenderingDevice.VertexFrequency Frequency { get; set; }` | Gets or sets per-vertex or per-instance record advancement. |
| `public System.UInt32 Location { get; set; }` | Gets or sets the shader input location. |
| `public System.UInt32 Offset { get; set; }` | Gets or sets the field byte offset within a record. |
| `public System.UInt32 Stride { get; set; }` | Gets or sets the byte distance between successive records. |

## Member descriptions

<a id="member-aa27b8c0fe6d"></a>
### RDVertexAttribute()

```csharp
public RDVertexAttribute()
```

Creates the default configuration.


<a id="member-c81f9acae58b"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the caller-owned configuration through inherited typed property metadata.

Inherited descriptors followed by this description's properties; configuration is copied when consumed.


<a id="member-a2626b00202d"></a>
### Binding

```csharp
public System.UInt32 Binding { get; set; }
```

Gets or sets the zero-based source buffer binding.

Initially uint.MaxValue; validated and copied by its device consumer.


<a id="member-182b8f57f932"></a>
### Format

```csharp
public Electron2D.RenderingDevice.DataFormat Format { get; set; }
```

Gets or sets the encoded vertex field format.

Initially RenderingDevice.DataFormat.Max; validated and copied by its device consumer.


<a id="member-fd360908360f"></a>
### Frequency

```csharp
public Electron2D.RenderingDevice.VertexFrequency Frequency { get; set; }
```

Gets or sets per-vertex or per-instance record advancement.

Initially RenderingDevice.VertexFrequency.Vertex; validated and copied by its device consumer.


<a id="member-8aeda2b9a1e3"></a>
### Location

```csharp
public System.UInt32 Location { get; set; }
```

Gets or sets the shader input location.

Initially 0; validated and copied by its device consumer.


<a id="member-6a55019748da"></a>
### Offset

```csharp
public System.UInt32 Offset { get; set; }
```

Gets or sets the field byte offset within a record.

Initially 0; validated and copied by its device consumer.


<a id="member-832ff64d2b47"></a>
### Stride

```csharp
public System.UInt32 Stride { get; set; }
```

Gets or sets the byte distance between successive records.

Initially 0; validated and copied by its device consumer.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDVertexAttribute.md) for absent roles.
