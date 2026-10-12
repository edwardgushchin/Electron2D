# PipeWriteException

Last updated: 2026-10-12

**Declaration:** public sealed class PipeWriteException : System.IO.IOException. **Source:** [PipeWriteException.cs](../../src/Core/IO/PipeWriteException.cs). Internal constructor; inherits ordinary IOException behavior.

## Description and member

`public int BytesWritten { get; }` is the exact nonnegative prefix length consumed by the failed process-pipe WriteBytes operation. A full nonblocking pipe is temporary; closed/failed streams cannot accept further input. The exception projects native partial-write error state without public backend handles or a shared last-error field. It does not roll back bytes delivered to the child. Retry only the remainder and impose application-specific deadlines/error policy.

```csharp
try { child.StandardIO.WriteBytes(payload.AsSpan(offset)); offset = payload.Length; }
catch (PipeWriteException error) { offset += error.BytesWritten; }
```

This fragment needs outer readiness/deadline/termination policy; it is not a complete retry loop. [OSProcessTests](../../tests/Electron2D.Tests/OSProcessTests.cs) verifies partial writes and SHA256 integrity after exact-prefix retries. [Processes](../components/processes.md) owns lifecycle/platform limits under ADR 0020.
