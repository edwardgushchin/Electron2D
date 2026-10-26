using Electron2D;

namespace Electron2D.Editor;

/// <summary>The two document roles in the first authoring session.</summary>
public enum SessionDocumentKind
{
    /// <summary>A typed packed scene.</summary>
    Scene,
    /// <summary>A strict UTF-8 C# source buffer.</summary>
    Code
}

/// <summary>How an explicit file reconciliation treats an unsaved live buffer.</summary>
public enum SessionExternalResolution
{
    /// <summary>Refresh a clean buffer; reject a dirty buffer.</summary>
    Reject,
    /// <summary>Replace live content with the current disk file, explicitly discarding unsaved changes.</summary>
    UseDisk,
    /// <summary>Keep live content while acknowledging the current disk file as its new persisted base.</summary>
    KeepLive
}

/// <summary>A published session transition.</summary>
public enum SessionChangeKind
{
    /// <summary>A new grouped edit.</summary>
    Applied,
    /// <summary>A guarded inverse of an earlier group.</summary>
    Undone,
    /// <summary>A guarded replay of an earlier group.</summary>
    Redone,
    /// <summary>One document's persistence boundary.</summary>
    Saved,
    /// <summary>An explicitly reconciled external file.</summary>
    Refreshed,
    /// <summary>Owner-local selection context changed without authoring or persistence.</summary>
    Selected
}

/// <summary>An immutable description of one scene object; IDs survive session rename/reparent and recovery.</summary>
/// <param name="ID">Stable object identity.</param><param name="ParentID">Parent identity, or empty for the root.</param>
/// <param name="Name">Current node name.</param><param name="TypeName">Concrete CLR type identity.</param>
/// <param name="Revision">Monotonic object revision.</param><param name="CanEdit">Whether this root-owned object is editable in this slice.</param>
/// <param name="Transform">Local transform for an Entity; null for other nodes.</param>
public sealed record SessionNode(Guid ID, Guid ParentID, string Name, string TypeName, ulong Revision, bool CanEdit, Transform? Transform);

/// <summary>A copied scene observation with no mutable runtime node references.</summary>
/// <param name="ID">Stable document identity.</param><param name="Path">Project-relative path.</param>
/// <param name="Revision">Monotonic content revision.</param><param name="IsDirty">Whether the live document differs from its persisted base.</param>
/// <param name="HasExternalChange">Whether the disk fingerprint differs from the persisted base.</param>
/// <param name="Nodes">Read-only copied node descriptions.</param>
public sealed record SessionScene(Guid ID, string Path, ulong Revision, bool IsDirty, bool HasExternalChange, IReadOnlyList<SessionNode> Nodes);

/// <summary>A copied source observation preserving line endings and the original UTF-8 BOM policy.</summary>
/// <param name="ID">Stable document identity.</param><param name="Path">Project-relative path.</param>
/// <param name="Revision">Monotonic content revision.</param><param name="IsDirty">Whether the buffer differs from its persisted base.</param>
/// <param name="HasExternalChange">Whether disk content changed independently.</param><param name="Text">Complete current source text.</param>
public sealed record SessionCode(Guid ID, string Path, ulong Revision, bool IsDirty, bool HasExternalChange, string Text);

/// <summary>A copied retained history entry, usable after owner recovery.</summary>
/// <param name="ID">Stable operation identity accepted by Undo/Redo.</param><param name="Label">Authored group label.</param>
/// <param name="Origin">Original client attribution.</param><param name="IsApplied">Whether the group's forward change is currently applied.</param>
public sealed record SessionOperation(Guid ID, string Label, string Origin, bool IsApplied);

/// <summary>A typed request in an atomic authoring group.</summary>
public abstract record SessionEdit;

/// <summary>Replaces one root-owned Entity's finite local transform.</summary>
/// <param name="NodeID">Stable object identity.</param><param name="ExpectedRevision">Object revision observed by the caller.</param><param name="Value">Requested local transform.</param>
public sealed record SessionTransformEdit(Guid NodeID, ulong ExpectedRevision, Transform Value) : SessionEdit;

/// <summary>Renames one root-owned node without changing its identity.</summary>
/// <param name="NodeID">Stable object identity.</param><param name="ExpectedRevision">Observed object revision.</param><param name="Name">Nonempty sibling-unique node name.</param>
public sealed record SessionRenameEdit(Guid NodeID, ulong ExpectedRevision, string Name) : SessionEdit;

/// <summary>Moves a root-owned node inside the same scene, preserving stable identity.</summary>
/// <param name="NodeID">Object identity.</param><param name="ExpectedSceneRevision">Fresh whole-hierarchy revision.</param>
/// <param name="ParentID">New parent identity.</param><param name="KeepGlobalTransform">Whether to preserve the Entity's global pose.</param>
public sealed record SessionReparentEdit(Guid NodeID, ulong ExpectedSceneRevision, Guid ParentID, bool KeepGlobalTransform = true) : SessionEdit;

/// <summary>Replaces the code buffer only at the exact observed document revision.</summary>
/// <param name="DocumentID">Stable code-document identity.</param><param name="ExpectedRevision">Observed revision.</param><param name="Text">Valid bounded UTF-8 source text.</param>
public sealed record SessionCodeEdit(Guid DocumentID, ulong ExpectedRevision, string Text) : SessionEdit;

/// <summary>A post-commit observer or retirement failure; it does not hide an already committed edit.</summary>
/// <param name="Code">Stable diagnostic category.</param><param name="Message">Readable failure detail.</param>
public sealed record SessionDiagnostic(string Code, string Message);

/// <summary>The committed transition and resulting content revisions.</summary>
/// <param name="OperationID">Stable history identity, or empty for a no-op/persistence transition.</param>
/// <param name="Kind">Transition kind.</param><param name="Origin">Client attribution.</param>
/// <param name="SceneRevision">Current scene revision.</param><param name="CodeRevision">Current code revision.</param>
/// <param name="FileWritten">Whether this persistence call published the requested file.</param>
/// <param name="Diagnostics">Post-commit failures, if any.</param>
public sealed record SessionResult(Guid OperationID, SessionChangeKind Kind, string Origin, ulong SceneRevision, ulong CodeRevision, bool FileWritten, IReadOnlyList<SessionDiagnostic> Diagnostics);

/// <summary>An overlapping stale edit or unreconciled external-file conflict.</summary>
public sealed class SessionConflictException : InvalidOperationException
{
    /// <summary>Creates a conflict with the affected identity and observed revisions.</summary>
    /// <param name="documentID">Stable affected document identity.</param><param name="objectID">Affected object, or empty for a document conflict.</param>
    /// <param name="expectedRevision">Caller's observed token.</param><param name="actualRevision">Current token; external disk conflicts can retain the same content token.</param>
    /// <param name="message">Readable conflict detail.</param>
    public SessionConflictException(Guid documentID, Guid objectID, ulong expectedRevision, ulong actualRevision, string message) : base(message)
    { DocumentID = documentID; ObjectID = objectID; ExpectedRevision = expectedRevision; ActualRevision = actualRevision; }
    /// <summary>Gets the affected document.</summary>
    public Guid DocumentID { get; }
    /// <summary>Gets the affected object, or empty for a document conflict.</summary>
    public Guid ObjectID { get; }
    /// <summary>Gets the caller's revision.</summary>
    public ulong ExpectedRevision { get; }
    /// <summary>Gets the current revision.</summary>
    public ulong ActualRevision { get; }
}
