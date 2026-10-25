namespace Electron2D.Editor;

/// <summary>A trusted local client of the shared owner; all methods require the session thread.</summary>
/// <remarks>Disposal releases this capability, not the session or its dirty documents. IDs and revisions come
/// from InspectScene/InspectCode. Apply never saves. Save publishes one file; Refresh requires explicit discard
/// when a live buffer is dirty. Undo/Redo reject later overlapping edits. Hierarchy groups conservatively guard
/// the complete scene revision; property groups allow independent objects and code to keep changing.</remarks>
public sealed class SessionClient : IDisposable
{
    private ProjectSession? _session;
    internal SessionClient(ProjectSession session, string origin) { _session = session; Origin = origin; }
    /// <summary>Gets the attribution attached to every operation submitted by this client.</summary>
    /// <value>The immutable nonblank origin supplied by the host.</value>
    public string Origin { get; }
    private ProjectSession Session => _session ?? throw new ObjectDisposedException(nameof(SessionClient));
    /// <summary>Copies current scene identities, poses and persistence/conflict state.</summary>
    /// <returns>A copied read-only snapshot, including disk-change observation.</returns>
    public SessionScene InspectScene() => Session.Scene();
    /// <summary>Copies the current unsaved source buffer and its persistence/conflict state.</summary>
    /// <returns>The current source text, identity, revision and persistence state.</returns>
    public SessionCode InspectCode() => Session.Code();
    /// <summary>Copies retained group identities and their applied state, including recovered history.</summary>
    /// <returns>A read-only history in original publication order, at most 32 entries.</returns>
    public IReadOnlyList<SessionOperation> InspectHistory() => Session.InspectHistory();
    /// <summary>Validates and commits a bounded typed group, or leaves published documents unchanged on failure.</summary>
    /// <param name="label">Nonblank history label, at most 128 characters.</param>
    /// <param name="edits">One through 64 typed requests against observed revisions.</param>
    /// <returns>One committed group identity and resulting revisions, or empty identity for a no-op.</returns>
    /// <exception cref="SessionConflictException">An affected revision is stale.</exception>
    /// <exception cref="ArgumentException">A request, name, transform, identity or buffer is invalid.</exception>
    /// <exception cref="Exception">Trusted reconstruction, validation or recovery fails before publication.</exception>
    public SessionResult Apply(string label, params SessionEdit[] edits) => Session.Apply(Origin, label, edits);
    /// <summary>Applies a guarded inverse of a retained operation without overwriting later overlapping work.</summary>
    /// <param name="operationID">Retained applied group identity returned by Apply.</param>
    /// <returns>The guarded inverse with fresh revisions and observer diagnostics.</returns>
    /// <exception cref="SessionConflictException">Later overlapping work prevents safe inversion.</exception>
    public SessionResult Undo(Guid operationID) => Session.History(Origin, operationID, redo: false);
    /// <summary>Replays a safely undone retained operation at a new monotonic revision.</summary>
    /// <param name="operationID">Retained safely undone group identity.</param>
    /// <returns>The guarded replay with fresh revisions and observer diagnostics.</returns>
    /// <exception cref="SessionConflictException">Later overlapping work prevents replay.</exception>
    public SessionResult Redo(Guid operationID) => Session.History(Origin, operationID, redo: true);
    /// <summary>Publishes one document at its observed revision; rejects unreconciled external writes.</summary>
    /// <remarks>Inspect FileWritten and Diagnostics: a post-save/recovery error can follow an already written file.</remarks>
    /// <param name="kind">The scene or code document to publish.</param>
    /// <param name="expectedRevision">Fresh document revision.</param>
    /// <returns>Whether the file was published and any post-write/observer diagnostics.</returns>
    /// <exception cref="SessionConflictException">The document token is stale or disk changed independently.</exception>
    /// <exception cref="Exception">Pre-save validation, recovery or file writing fails before publication.</exception>
    public SessionResult Save(SessionDocumentKind kind, ulong expectedRevision) => Session.Save(Origin, kind, expectedRevision);
    /// <summary>Reconciles a clean external file, or explicitly resolves a dirty buffer against its fresh revision.</summary>
    /// <remarks>UseDisk scene refresh replaces object identities and retires affected history; document identity remains stable.
    /// This bounded slice reports overlapping source edits instead of automatically merging text.</remarks>
    /// <param name="kind">The document whose persisted base changed.</param>
    /// <param name="expectedRevision">Fresh document revision, also required for explicit discard.</param>
    /// <param name="resolution">Reject dirty state, use disk, or keep the live buffer against the new base.</param>
    /// <returns>The resulting revisions; this operation does not write a project document.</returns>
    /// <exception cref="SessionConflictException">The revision is stale or a dirty buffer lacks explicit resolution.</exception>
    public SessionResult Refresh(SessionDocumentKind kind, ulong expectedRevision, SessionExternalResolution resolution = SessionExternalResolution.Reject) => Session.Refresh(Origin, kind, expectedRevision, resolution);
    /// <summary>Releases this client handle.</summary>
    public void Dispose() => _session = null;
}
