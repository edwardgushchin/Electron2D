# Independent processes and pipes

Last updated: 2026-10-13

## Scope and flow

OS process operations use the retained static service. CreateProcess starts a literal executable/argv independently; CreateInstance restarts the current application, including the entry assembly for a framework-dependent host. Execute blocks and appends one captured string after exit, preserving existing collection items. POSIX stdout/stderr capture merges into one native stream in write order; nonzero exit codes remain ordinary results. Arguments never enter an implicit shell. GetExecutablePath returns the actual native host path, GetCommandLineArgs copies application arguments, and GetCommandLineUserArgs copies the suffix after the first -- or ++ separator.

ExecuteWithPipe returns ProcessPipes with PID, owned bidirectional StandardIO and read-only StandardError FileAccess streams. Pipes are non-seekable. Blocking I/O waits on the caller; nonblocking ReadBytes returns an available prefix immediately without mistaking NotReady for EOF. Exact reads can consume an incomplete prefix before throwing IOException; use byte buffers for incremental framing. A partial write throws PipeWriteException.BytesWritten so retries send only the remainder. Closing/disposal releases streams without killing the independent process. Native SafeHandles also release forgotten streams. IsProcessRunning queries native IDs, GetProcessExitCode retains the most recently owned child status, and Kill terminates only one process, never its descendants. IDs may be reused by the OS. Completed children release native/BCL handles; a small exit-history record remains in the service.

This is cold, blocking/allocating process and I/O work. It is unsuitable for real-time callbacks. The runtime does not impose application-specific deadlines or kill policy. Tooling hosts still own cancellation, deadlines, diagnostics and project orchestration under ADR 0090. Linux/.NET10 with SDL3.4.18 is exercised. Mobile launch adapters, macOS .app selection and Windows shared-output/explicit-new-console launch remain exact dependencies. Arbitrary named pipe:// paths remain unavailable; these pipe://PID/channel values identify already-owned streams and do not create a named-pipe connector.

## Owned types

[OS](../classes/OS.md) supplies static operations, [ProcessPipes](../classes/ProcessPipes.md) owns the typed dictionary projection, and [PipeWriteException](../classes/PipeWriteException.md) projects consumed-prefix error state. Internal [OSProcess](../classes/OSProcess.md), [ProcessPipeStream](../classes/ProcessPipeStream.md) and [ProcessPipeHandle](../classes/ProcessPipeHandle.md) serialize native lifetime and nonblocking I/O; FileAccess retains one operation lock for seekable files and separate read/write direction locks for process pipes, with existing strict encodings and binary methods. Separate threads may independently drain stdin/stdout and stderr; reads and writes on StandardIO may drain concurrently; each direction remains serialized. Native blocking reads release the process gate between polls so another thread can stop the child.

## Example

This desktop fragment requires a known executable that reads one line, returns stdout/stderr lines and exits. OSProcessTests compiles and exercises the same public path.

```csharp
using var child = OS.ExecuteWithPipe(executable, arguments);
child.StandardIO.WriteLine("request");
child.StandardIO.Flush();
var response = child.StandardIO.ReadLine();
var error = child.StandardError.ReadLine();
var pid = child.PID;
// Dispose streams when finished; OS.Kill(pid) is explicit process policy.
```

## Verification

[OSProcessTests](../../tests/Electron2D.Tests/OSProcessTests.cs) launches fresh child executables and verifies literal empty/Unicode/quote/shell-looking arguments and rejected invalid UTF16, current executable/argv/separators, independent instances and completed work after launching-application exit, retained actual exit codes, missing programs, native live/kill state, 4MiB output, stdout/stderr order, bidirectional UTF8/CR pushback, blocking/nonblocking readiness and EOF, concurrent blocking duplex beyond capacity, read-only/non-seekable guards, large partial-write retries with SHA256 integrity, disposal and finalized-handle independence. FileAccess filesystem/encoding/lifecycle regressions execute separately. POSIX status observation retains signal/core information before the native owner reaps the child; normal exit137 remains distinct from SIGKILL9. Native/all-thread allocation totals, throughput, UI console behavior and foreign hosts are unmeasured; no realtime zero-allocation claim is made for these cold APIs.

## Dependencies and decisions

Uses BCL Windows process launch and the approved internal SDL process/IO backend with POSIX status observation, without new packages or vendor edits. ADRs 0004/0020/0095 define typed result/error, stream and service ownership. ADR 0090 separates the runtime primitives from a complete agent/editor workflow. Native SDL pipe streams must retain their process until closure; their native cleanup removes process stream properties before process destruction. [SDL process creation](https://wiki.libsdl.org/SDL3/SDL_CreateProcessWithProperties) and [input](https://wiki.libsdl.org/SDL3/SDL_GetProcessInput) describe native stream readiness/partial writes.

Process-local environment changes through OS.SetEnvironment/UnsetEnvironment are inherited by subsequent launches through both managed and SDL native paths. See [environment ownership and directory queries](os-environment.md).

A process launched with ExecuteWithPipe can receive its request through OS.ReadBufferFromSTDIN/ReadStringFromSTDIN and inspect its actual standard handles. The [standard-input component](standard-input.md) verifies this linked public workflow with an independent child.

OS restart-on-exit configuration is a process-wide, desktop-only request with a copied literal argument list. SetRestartOnExit atomically replaces both values; IsRestartOnExitSet reads the flag and GetRestartOnExitArguments returns an independent array, including while disabled. Invalid Unicode, NUL or null elements fail before publication. A single normal managed process-shutdown handler captures the final request and uses CreateInstance. A standalone host must finish its loop cleanup and return; stopping a borrowed loop or returning from Engine.Run alone does not exit its host. The replacement process starts with disabled restart state. Failed native launch reports to standard error and preserves the original exit code; crashes and forced termination do not guarantee shutdown callbacks.

A restart attempt clears its retained flag and arguments before subsequent managed shutdown handlers run. Shutdown capture rejects later setters. A controlled Linux apphost fixture removes only its copied executable and verifies actual launch-failure reporting without changing the original exit status.

CreateInstance relaunches an application executable directly. For a dotnet host it resolves the named entry DLL from AppContext.BaseDirectory and verifies the file before launching, without requiring Assembly.Location. Literal argument and process ownership contracts remain unchanged.
