# OS.AppleMemoryHost

Internal native-memory implementation detail; no public/protected surface.

Lazily initialized process-owned mach_host_self send right. Type initialization serializes first use; retaining one right avoids leaking a new right on each query.

**Source:** [OS.Memory.cs](../../src/Core/OS/OS.Memory.cs).
**Contract:** [Host memory](../components/host-memory.md).
