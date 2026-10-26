namespace Electron2D.Editor;

internal sealed record EmptyArguments;
internal sealed record SceneQuery(int Offset = 0, int Limit = 64);
internal sealed record CodeQuery(int Start = 0, int Length = 8192);
internal sealed record CodeReplacement(Guid DocumentID, ulong ExpectedRevision, int Start, int Length, string Text, string Label);
internal sealed record SceneChanges(Guid DocumentID, string Label, SceneChange[] Edits);
internal sealed record SceneChange(string Kind, Guid NodeID, ulong ExpectedRevision, float X = 0, float Y = 0,
    float XX = 1, float XY = 0, float YX = 0, float YY = 1, string? Name = null, Guid? ParentID = null, bool KeepGlobalTransform = true);
internal sealed record HistoryChange(Guid OperationID);
internal sealed record DocumentPublication(SessionDocumentKind Kind, ulong ExpectedRevision);
internal sealed record DocumentRefresh(SessionDocumentKind Kind, ulong ExpectedRevision, SessionExternalResolution Resolution = SessionExternalResolution.Reject);
internal sealed record SelectionChange(Guid NodeID, ulong ExpectedSceneRevision, ulong ExpectedSelectionRevision);
internal sealed record JobQuery(Guid JobID);
internal sealed record JobArtifactQuery(Guid JobID, int Artifact);
internal sealed record JobDataQuery(Guid JobID, string Kind, int Offset = 0, int Limit = 64);
internal sealed record JobDataPage(string Kind, int Offset, int Total, System.Text.Json.JsonElement Items);
internal sealed record SnapshotSummary(Guid ID, string SHA256, ulong SceneRevision, ulong CodeRevision, int Files);
internal sealed record JobSubmission(ulong ExpectedSceneRevision, ulong ExpectedCodeRevision,
    SessionJobKind Kind = SessionJobKind.Run, SessionRunMode Mode = SessionRunMode.Headless, int Steps = 60, int FPS = 60,
    int Width = 320, int Height = 180, string Backend = "gpu", ulong Seed = 1, int DeadlineMilliseconds = 300000,
    int[]? CaptureSteps = null, SessionInput[]? Inputs = null);
internal sealed record DocumentSummary(Guid ID, string Path, ulong Revision, bool IsDirty, bool HasExternalChange);
internal sealed record OperationContext(Guid OwnerID, string ProjectName, DocumentSummary Scene, DocumentSummary Code,
    SessionSelection Selection, SessionNode? SelectedNode, string[] Capabilities);
internal sealed record ScenePage(DocumentSummary Document, int Offset, int Total, IReadOnlyList<SessionNode> Nodes, SessionSelection Selection);
internal sealed record CodePage(DocumentSummary Document, int Start, int Next, int Total, string Text);
internal sealed record OperationArtifact(int Index, string Kind, int Step, string Path, string SHA256);
internal sealed record JobInspection(Guid ID, Guid SnapshotID, SessionJobStatus Status, string Stage, bool IsStale,
    SnapshotSummary? Snapshot, int? ExitCode, OperationArtifact[] Artifacts, IReadOnlyList<SessionObservation> Observations,
    IReadOnlyList<SessionAssertion> Assertions, IReadOnlyList<SessionJobDiagnostic> Diagnostics, int ObservationCount, int AssertionCount);
internal sealed record ArtifactContent(string MIMEType, string Base64, string SHA256, Guid JobID, Guid SnapshotID, string SnapshotSHA256, ulong SceneRevision, ulong CodeRevision, bool IsStale, SessionJobStatus Status);
