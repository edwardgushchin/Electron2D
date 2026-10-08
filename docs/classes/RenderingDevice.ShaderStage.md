# RenderingDevice.ShaderStage

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.ShaderStage`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Identifies the currently executable Compute stage. The complete stage family remains partial until the corresponding device pipelines execute.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.ShaderStage Compute = 4` | A general-purpose compute shader. |

## Member descriptions

<a id="member-3bc8cc7a1449"></a>
### Compute

```csharp
public const Electron2D.RenderingDevice.ShaderStage Compute = 4
```

A general-purpose compute shader.

## Verification and limits

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous transfer APIs and additional platforms are not established by this slice.
