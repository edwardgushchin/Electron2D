# OS.ResourceLimit

Internal native-memory implementation detail; no public/protected surface.

Two unsigned 64-bit soft/hard limits, read through getrlimit64 on Linux/Android and getrlimit on Apple. Unlimited sentinel handling follows each platform.

**Source:** [OS.Memory.cs](../../src/Core/OS/OS.Memory.cs).
**Contract:** [Host memory](../components/host-memory.md).
