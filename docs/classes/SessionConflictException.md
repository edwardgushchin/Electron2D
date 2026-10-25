# SessionConflictException

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed class SessionConflictException : InvalidOperationException`

Reports a stale overlapping edit or independently changed file. It never authorizes a retry over newer state. Inspect current buffers, then deliberately reconcile/recompute before submitting new tokens. See [SessionClient](SessionClient.md) and [the component](../components/project-session.md).

## Constructor and properties

| Signature | Contract |
| --- | --- |
| `public SessionConflictException(Guid documentID, Guid objectID, ulong expectedRevision, ulong actualRevision, string message)` | Creates copied conflict evidence and the inherited exception message. |
| `public Guid DocumentID { get; }` | Affected document. |
| `public Guid ObjectID { get; }` | Affected node, or empty for a document conflict. |
| `public ulong ExpectedRevision { get; }` | Caller's token. |
| `public ulong ActualRevision { get; }` | Current token. |

## Property descriptions

### DocumentID

The stable scene/code identity from the session snapshot.

### ObjectID

The stable node identity for an object conflict; Guid.Empty for source/document/base-file conflicts.

### ExpectedRevision

The token submitted by the caller; no history cursor is used.

### ActualRevision

Current affected content token, or zero for a missing object. An external file can conflict while the live content revision remains equal to ExpectedRevision; inspect HasExternalChange and use explicit reconciliation.

## Example and verification

```csharp
try { client.Apply("move", new SessionTransformEdit(node.ID, node.Revision, pose)); }
catch (SessionConflictException conflict)
{
    Console.WriteLine($"{conflict.ObjectID}: {conflict.ExpectedRevision} -> {conflict.ActualRevision}");
}
```

Partial snippet: client, node and pose come from the [session example](ProjectSession.md#example). [ProjectSession.Tests](../../tests/ProjectSession/Program.cs) verifies actual stale/overlap/file conflicts without data loss. This exception is an in-process tooling result, not a transport authentication boundary.
