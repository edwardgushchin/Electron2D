# RenderingDevice.StorageBufferUsage

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum RenderingDevice.StorageBufferUsage` with `[Flags]`. **Source:** [RenderingDevice.Transfers.cs](../../src/Servers/Rendering/Device/RenderingDevice.Transfers.cs). **Component:** [Local compute](../components/local-compute.md).

## Description and example

Additional executable roles for buffers created by [RenderingDevice](RenderingDevice.md). Zero selects ordinary read/write storage. Unknown flag bits throw before allocation.

```csharp
var commands = device.StorageBufferCreate(12,
    usage: RenderingDevice.StorageBufferUsage.DispatchIndirect);
```

## Enumeration descriptions

| Declaration | Value | Contract |
| --- | --- | --- |
| `DispatchIndirect` | 1 | Permits the buffer to supply three consecutive unsigned 32-bit X/Y/Z group counts to ComputeListDispatchIndirect. The offset is four-byte aligned and twelve bytes must fit. Zero groups skip work; GPU producers must keep counts within device limits. |

## Verification and limits

[RenderingDeviceTests.Transfers](../../tests/Electron2D.Tests/RenderingDeviceTests.Transfers.cs) executes GPU-produced commands, command copies and indirect consumers, and rejects missing roles, unknown flags, invalid offsets and stale identities. Linux x64/Vulkan is verified; other platforms and driver allocations remain unverified.
