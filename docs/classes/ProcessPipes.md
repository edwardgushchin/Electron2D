# ProcessPipes

Last updated: 2026-10-12

**Declaration:** public sealed class ProcessPipes : IDisposable. **Source:** [ProcessPipes.cs](../../src/Core/OS/ProcessPipes.cs). **Implements:** System.IDisposable. **Factory:** OS.ExecuteWithPipe.

## Description

Owns two FileAccess streams and immutable process identity. StandardIO writes stdin and reads stdout; StandardError only reads stderr. Disposal closes both streams once and never terminates the independent child. PID and the stream references remain available after disposal; the FileAccess objects themselves are disposed. There is no public constructor. Result fields are the operation-specific typed projection of the fixed process-pipe dictionary.

## Members

| Declaration | Contract |
| --- | --- |
| `public int PID { get; }` | Positive native process ID. IDs may be reused after exit. |
| `public FileAccess StandardIO { get; }` | Owned bidirectional non-seekable stdin/stdout stream. |
| `public FileAccess StandardError { get; }` | Owned independent non-seekable read-only stderr stream. |
| `public void Dispose()` | Closes both streams even if the first close fails; child termination is explicit through OS.Kill. |

## Example and verification

```csharp
using var child = OS.ExecuteWithPipe(executable, arguments, blocking: false);
var pid = child.PID;
var available = child.StandardIO.ReadBytes(4096);
if (available.Length == 0 && !child.StandardIO.EOFReached) { /* retry later */ }
```

Requires a desktop process host and known executable. [The component](../components/processes.md) records request/response, process exit, native stream lifetime, retry, GC and foreign-host limits. ADRs 0004/0020/0095 own the typed projection and service lifetime.
