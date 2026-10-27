# OS.AppleMemoryStatus

Internal native-memory implementation detail; no public/protected surface.

vm_statistics64 output including the used/reclaimable page counters. The returned word count is checked before consuming those counters.

**Source:** [OS.Memory.cs](../../src/Core/OS/OS.Memory.cs).
**Contract:** [Host memory](../components/host-memory.md).
