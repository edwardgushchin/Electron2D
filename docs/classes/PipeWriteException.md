# PipeWriteException

Last updated: 2026-10-12

**Declaration:** public sealed class PipeWriteException : System.IO.IOException. **Source:** [PipeWriteException.cs](../../src/Core/IO/PipeWriteException.cs). Internal constructor; inherits ordinary IOException behavior.

## Description and member

`public int BytesWritten { get; }` gives the number of bytes accepted by the pipe before the operation failed. For framed writes the count includes framing bytes, such as the Pascal string length prefix or the LF appended by `WriteLine`. A full nonblocking pipe is temporary. Closed or failed streams cannot accept further input. When retrying a temporary failure, send only the unconsumed suffix of the complete byte sequence.

```csharp
try { child.StandardIO.WriteBytes(payload.AsSpan(offset)); offset = payload.Length; }
catch (PipeWriteException error) { offset += error.BytesWritten; }
```

This fragment needs outer readiness/deadline/termination policy; it is not a complete retry loop. [OSProcessTests](../../tests/Electron2D.Tests/OSProcessTests.cs) verifies partial writes and SHA256 integrity after exact-prefix retries. [Processes](../components/processes.md) owns lifecycle/platform limits under ADR 0020.
