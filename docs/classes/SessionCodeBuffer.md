# SessionCodeBuffer

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionCodeBuffer.cs](../../editor/Session/SessionCodeBuffer.cs).
- **Declaration:** `public sealed class SessionCodeBuffer : IDisposable`

One local code view's pending-input bridge, created by SessionClient.BindCodeBuffer. A native widget can defer its change notification; the bridge ensures visible manual text enters shared operations before an agent query, mutation or snapshot. The owner supports one bridge. It owns callbacks, not a second source document or widget.

## Example

```csharp
using var binding = client.BindCodeBuffer(ReadPendingEdit, KeepRejectedDraft);
binding.Flush();
```

Partial snippet: the host supplies synchronous Func&lt;SessionCodeEdit?&gt; and Action&lt;SessionCodeEdit, Exception&gt; callbacks and a live client. [ProjectWorkspace](../../editor/Workspace/ProjectWorkspace.cs) is the complete native consumer.

## Public methods

| Signature | Contract |
| --- | --- |
| `public void Flush()` | Publish pending source through the shared owner. |
| `public void Dispose()` | Unregister the reader/rejection callback. |

## Method descriptions

### Flush

Owner-thread, synchronous. Calls the bounded side-effect-free reader and submits any returned proposal as an attributed Edit source group. A null result is a no-op. Reads/mutations/jobs also flush automatically, except internal committed-state observations during an existing transition. Readers cannot mutate/dispose/rebind the owner; reentry rejects. Stale/invalid input calls the rejection callback then propagates the original error; callback failure aggregates both errors. Committed source remains unchanged after rejection. A disposed binding throws ObjectDisposedException.

### Dispose

Unregisters this bridge and releases callbacks. A live binding requires the owner thread and cannot dispose from inside its reader. Repeated disposal is safe after unbinding/owner retirement. It does not commit a pending proposal: Flush before intentionally retiring a view. Client disposal also unregisters its bridge. Normal owner disposal first flushes pending input; a failed flush leaves that owner open for resolution.

## Verification and limits

[Managed contracts](../../tests/ProjectJobs/JobContracts.cs) cover singleton binding, pending query ordering, no-op, stale rejection and mutation reentry. [Native UI checks](../../tests/ProjectWorkspace/Program.cs) edit before deferred TextChanged delivery, then issue programmatic operations and run jobs. This is a trusted local callback boundary, not IPC/authentication or a language-service text merge.
