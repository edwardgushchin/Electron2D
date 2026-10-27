# OS.LinuxMemoryStatus

Internal native-memory implementation detail; no public/protected surface.

Sequential sysinfo output, with pointer-sized native long fields and sufficient trailing storage for both 32-bit and 64-bit ABI output.

**Source:** [OS.Memory.cs](../../src/Core/OS/OS.Memory.cs).
**Contract:** [Host memory](../components/host-memory.md).
