# RenderingDevice.MemoryType

Last updated: 2026-10-27

**Declaration:** `public enum RenderingDevice.MemoryType` · **Source:**
[device memory](../../src/Servers/Rendering/Device/RenderingDevice.Memory.cs).

| Value | Identity | Meaning |
| --- | ---: | --- |
| Textures | 0 | Actual texture allocation regions |
| Buffers | 1 | Actual buffer regions, including uniform and transfer storage |
| Total | 2 | Committed allocation blocks, including retained free space |

`RenderingDevice.GetMemoryUsage` returns checked Int64 byte values for the shared
physical payload under ordinary device owner/lifetime guards. Undefined values
reject. See [native render memory](../components/render-memory.md) for source,
sharing, retirement, unsupported adapters and verification boundaries.
