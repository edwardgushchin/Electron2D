# RenderingDevice.ShaderStage

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.ShaderStage`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies executable shader stages. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.ShaderStage Compute = 4` | A general-purpose compute shader. |
| `public const Electron2D.RenderingDevice.ShaderStage Fragment = 1` | A fragment processing stage. |
| `public const Electron2D.RenderingDevice.ShaderStage Max = 10` | Marks the exclusive stage numeric extent; it is not an executable stage. |
| `public const Electron2D.RenderingDevice.ShaderStage Vertex = 0` | A vertex processing stage. |

## Member descriptions

<a id="member-3bc8cc7a1449"></a>
### Compute

```csharp
public const Electron2D.RenderingDevice.ShaderStage Compute = 4
```

A general-purpose compute shader.


<a id="member-ed902864e367"></a>
### Fragment

```csharp
public const Electron2D.RenderingDevice.ShaderStage Fragment = 1
```

A fragment processing stage.


<a id="member-09324d4ffc04"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.ShaderStage Max = 10
```

Marks the exclusive stage numeric extent; it is not an executable stage.


<a id="member-fd2fc61bc46b"></a>
### Vertex

```csharp
public const Electron2D.RenderingDevice.ShaderStage Vertex = 0
```

A vertex processing stage.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
