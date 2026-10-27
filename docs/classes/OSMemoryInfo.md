# OSMemoryInfo

**Namespace:** `Electron2D`. **Declaration:** `public readonly struct OSMemoryInfo`.
**Source:** [OSMemoryInfo.cs](../../src/Core/OS/OSMemoryInfo.cs).
**Component:** [Host memory](../components/host-memory.md).

An immutable value returned by `OS.GetMemoryInfo()`. There are no public setters;
retaining a snapshot does not retain native resources. Each property is a `long`
byte count with -1 for unknown/unrepresentable information. A default-constructed
value is the ordinary zero-initialized C# struct, not a native query.

| Property | Meaning |
| --- | --- |
| `Physical` | Total usable host physical memory. |
| `Free` | Currently free physical bytes from the platform's native query. |
| `Available` | Linux/Android free physical plus free swap; macOS native free plus available swap; Apple mobile/TV free bytes; Windows total commit limit including existing commitments. |
| `Stack` | Unix process soft stack limit, or Windows caller's native stack address range. Unlimited/unknown is -1. |

Counts are advisory, independent and immediately stale; they do not reserve memory
or represent managed heap usage or container quotas. Do not use `Available` as a
promise that an allocation will succeed. See [ADR 0113](../decisions/host-platform.md#native-memory-snapshots).
