# RenderingDevice.Features

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.Features`. **Source:** [RenderingDevice.Addresses.cs](../../src/Servers/Rendering/Device/RenderingDevice.Addresses.cs). **Component:** [Buffer device addresses](../components/buffer-device-addresses.md).

## Description

Selectors for `RenderingDevice.HasFeature`. Each result describes an integrated executing path on that device. A numeric selector does not implement an absent adapter.

## API

| Value | Contract |
| --- | --- |
| `SupportsMetalFXSpatial = 3` | Spatial MetalFX scaling; unavailable without its native adapter. |
| `SupportsMetalFXTemporal = 4` | Temporal MetalFX scaling; unavailable without its native adapter. |
| `SupportsBufferDeviceAddress = 6` | Enabled stable allocations and physical-storage pointer execution. |
| `SupportsImageAtomic32Bit = 7` | Both signed and unsigned R32 native storage-image atomics. |
| `SupportsRayQuery = 11` | Inline acceleration queries; unavailable without its native adapter. |
| `SupportsHDROutput = 13` | Native main renderer output availability; false on local devices. |

## Example

Query `device.HasFeature(RenderingDevice.Features.SupportsBufferDeviceAddress)` before choosing a pointer shader. Unsupported creation still rejects explicitly. See the [public consumer](../../tests/DeviceGraphicsStorage.Consumer/Addresses.cs) for executed buffer and shader paths.

## Verification and limits

Address execution and image formats are device capabilities. HDR reports the existing native presentation path and does not certify a physical HDR display. Scaling and acceleration-query execution remain separate coverage prerequisites.
