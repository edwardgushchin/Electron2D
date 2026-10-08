# RDUniform

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDUniform`. **Source:** [RDUniform.cs](../../src/Servers/Rendering/Device/RDUniform.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A copied binding description with a descriptor category, nonnegative binding number and an ordered list of borrowed RIDs. UniformSetCreate validates it against a shader and copies its contents, so later descriptor edits do not alter an existing set. GetIDs returns an independent array. The default category is Image; the current device compute slice executes UniformBuffer and StorageBuffer descriptors only. Other enum values describe categories and are rejected at set creation when unsupported.

## Example

The snippet assumes the shown shader bindings and caller-provided compiled bytecode/buffer identities. The complete executable consumer is [WaterSimulation.GPU.cs](../../examples/PhysicsSandbox/WaterSimulation.GPU.cs).

```csharp
using var descriptor = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer };
descriptor.AddID(buffer);
var set = device.UniformSetCreate([descriptor], shader, 0);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDUniform()` | Creates an empty image binding. |
| `public System.Void AddID(Electron2D.RID id)` | Appends a borrowed resource identity. |
| `public System.Void ClearIDs()` | Removes all borrowed identities. |
| `public Electron2D.RID[] GetIDs()` | Copies the binding's resource identities. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public System.Int32 Binding { get; set; }` | Gets or sets the descriptor binding number. |
| `public Electron2D.RenderingDevice.UniformType UniformType { get; set; }` | Gets or sets the resource category. |

## Member descriptions

<a id="member-1e53f8bd7d75"></a>
### RDUniform()

```csharp
public RDUniform()
```

Creates an empty image binding.

<a id="member-cc1d69568f19"></a>
### AddID(Electron2D.RID)

```csharp
public System.Void AddID(Electron2D.RID id)
```

Appends a borrowed resource identity.

`id`: The resource, validated against the owning device when the set is created.

<a id="member-2a45ddfe6cae"></a>
### ClearIDs()

```csharp
public System.Void ClearIDs()
```

Removes all borrowed identities.

<a id="member-38cd4c5860d3"></a>
### GetIDs()

```csharp
public Electron2D.RID[] GetIDs()
```

Copies the binding's resource identities.

An independent array in insertion order.

<a id="member-d44e6be4255e"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-6cdf472c8ec2"></a>
### Binding

```csharp
public System.Int32 Binding { get; set; }
```

Gets or sets the descriptor binding number.

A nonnegative shader binding, initially zero.

<a id="member-10133c6f5f45"></a>
### UniformType

```csharp
public Electron2D.RenderingDevice.UniformType UniformType { get; set; }
```

Gets or sets the resource category.

The image category initially; set a supported buffer category before creating a compute uniform set.

## Verification and limits

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous transfer APIs and additional platforms are not established by this slice.
