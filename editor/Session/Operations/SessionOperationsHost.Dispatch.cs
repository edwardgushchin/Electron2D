using System.Text.Json;

namespace Electron2D.Editor;

public sealed partial class SessionOperationsHost
{
    private JsonElement Execute(SessionClient client, OperationCall call, HashSet<Guid> ownedJobs, Capability scope)
    {
        if (call.Arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException("Operation arguments require an object.");
        switch (call.Method)
        {
            case "context":
            {
                OperationWire.Model<EmptyArguments>(call.Arguments);
                var scene = client.InspectScene(); var code = client.InspectCode(); var selected = client.InspectSelection();
                return OperationWire.Element(new OperationContext(OwnerID, _project.Name, Summary(scene), Summary(code), selected,
                    scene.Nodes.SingleOrDefault(n => n.ID == selected.NodeID), OperationCatalog.Names));
            }
            case "scene_read":
            {
                var query = OperationWire.Model<SceneQuery>(call.Arguments); var scene = client.InspectScene();
                if (query.Offset < 0 || query.Offset > scene.Nodes.Count || query.Limit is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(query));
                return OperationWire.Element(new ScenePage(Summary(scene), query.Offset, scene.Nodes.Count, scene.Nodes.Skip(query.Offset).Take(query.Limit).ToArray(), client.InspectSelection()));
            }
            case "code_read":
            {
                var query = OperationWire.Model<CodeQuery>(call.Arguments); var code = client.InspectCode();
                if (query.Length is < 1 or > 8192) throw new ArgumentOutOfRangeException(nameof(query));
                Boundary(code.Text, query.Start); var end = Math.Min(code.Text.Length, checked(query.Start + query.Length));
                if (end < code.Text.Length && char.IsLowSurrogate(code.Text[end])) end = end == query.Start + 1 ? end + 1 : end - 1;
                return OperationWire.Element(new CodePage(Summary(code), query.Start, end, code.Text.Length, code.Text[query.Start..end]));
            }
            case "code_edit":
            {
                var edit = OperationWire.Model<CodeReplacement>(call.Arguments); var code = client.InspectCode();
                ArgumentNullException.ThrowIfNull(edit.Text);
                if (edit.Text.Length > 65536 || edit.Length < 0) throw new ArgumentOutOfRangeException(nameof(edit));
                Boundary(code.Text, edit.Start); var end = checked(edit.Start + edit.Length); Boundary(code.Text, end);
                var next = code.Text[..edit.Start] + edit.Text + code.Text[end..];
                return OperationWire.Element(client.Apply(edit.Label, new SessionCodeEdit(edit.DocumentID, edit.ExpectedRevision, next)));
            }
            case "scene_apply":
            {
                var edit = OperationWire.Model<SceneChanges>(call.Arguments); var scene = client.InspectScene();
                if (edit.DocumentID != scene.ID || edit.Edits is null || edit.Edits.Length is < 1 or > 64) throw new ArgumentException("Choose the observed scene and 1..64 typed edits.");
                var requests = new List<SessionEdit>(edit.Edits.Length);
                foreach (var item in edit.Edits)
                {
                    if (item is null) throw new ArgumentException("A scene edit cannot be null.");
                    SessionEdit request = item.Kind switch
                    {
                        "position" => Position(item, scene),
                        "transform" => new SessionTransformEdit(item.NodeID, item.ExpectedRevision, new(item.XX, item.XY, item.YX, item.YY, item.X, item.Y)),
                        "rename" => new SessionRenameEdit(item.NodeID, item.ExpectedRevision, item.Name ?? throw new ArgumentException("A name is required.")),
                        "reparent" => new SessionReparentEdit(item.NodeID, item.ExpectedRevision, item.ParentID ?? throw new ArgumentException("A parent identity is required."), item.KeepGlobalTransform),
                        _ => throw new ArgumentException("Unknown typed scene operation.")
                    };
                    requests.Add(request);
                }
                return OperationWire.Element(client.Apply(edit.Label, requests.ToArray()));
            }
            case "history": OperationWire.Model<EmptyArguments>(call.Arguments); return OperationWire.Element(client.InspectHistory());
            case "undo": return OperationWire.Element(client.Undo(OperationWire.Model<HistoryChange>(call.Arguments).OperationID));
            case "redo": return OperationWire.Element(client.Redo(OperationWire.Model<HistoryChange>(call.Arguments).OperationID));
            case "selection":
            {
                var change = OperationWire.Model<SelectionChange>(call.Arguments);
                return OperationWire.Element(client.SelectNode(change.NodeID, change.ExpectedSceneRevision, change.ExpectedSelectionRevision));
            }
            case "save":
            {
                if (!scope.FileWrites) throw new UnauthorizedAccessException("File publication is outside this client capability.");
                var request = OperationWire.Model<DocumentPublication>(call.Arguments);
                return OperationWire.Element(client.Save(request.Kind, request.ExpectedRevision));
            }
            case "refresh":
            {
                var request = OperationWire.Model<DocumentRefresh>(call.Arguments);
                if (request.Resolution == SessionExternalResolution.UseDisk && !scope.Discard) throw new UnauthorizedAccessException("Discarding unsaved documents is outside this client capability.");
                return OperationWire.Element(client.Refresh(request.Kind, request.ExpectedRevision, request.Resolution));
            }
            case "job_start":
            {
                var request = OperationWire.Model<JobSubmission>(call.Arguments);
                var job = client.StartJob(new(request.Kind, _project.ProjectFile, _sdk, request.Mode, request.Steps, request.FPS, request.Width, request.Height,
                    request.Backend, request.Seed, request.DeadlineMilliseconds, request.CaptureSteps, request.Inputs), request.ExpectedSceneRevision, request.ExpectedCodeRevision);
                ownedJobs.Add(job.ID); return OperationWire.Element(Inspect(job));
            }
            case "jobs": OperationWire.Model<EmptyArguments>(call.Arguments); return OperationWire.Element(client.InspectJobs().Select(Inspect).ToArray());
            case "job_status": return OperationWire.Element(Inspect(Find(client, OperationWire.Model<JobQuery>(call.Arguments).JobID)));
            case "job_cancel":
            {
                var job = Find(client, OperationWire.Model<JobQuery>(call.Arguments).JobID); job.Cancel(); return OperationWire.Element(Inspect(job));
            }
            case "job_data":
            {
                var query = OperationWire.Model<JobDataQuery>(call.Arguments); var job = Find(client, query.JobID);
                if (query.Offset < 0 || query.Limit is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(query));
                var result = job.Completion.IsCompleted ? job.Completion.GetAwaiter().GetResult() : null;
                return OperationWire.Element(query.Kind switch
                {
                    "inputs" => Page(query, job.Snapshot?.Files ?? []),
                    "observations" => Page(query, result?.Observations ?? []),
                    "assertions" => Page(query, result?.Assertions ?? []),
                    "diagnostics" => Page(query, result?.Diagnostics ?? []),
                    _ => throw new ArgumentException("Choose inputs, observations, assertions or diagnostics.")
                });
            }
            case "job_artifact":
            {
                var query = OperationWire.Model<JobArtifactQuery>(call.Arguments); var job = Find(client, query.JobID);
                if (!job.Completion.IsCompleted) throw new InvalidOperationException("Wait for terminal artifact validation.");
                var artifacts = job.Completion.GetAwaiter().GetResult().Artifacts;
                if ((uint)query.Artifact >= artifacts.Count) throw new ArgumentOutOfRangeException(nameof(query));
                var artifact = artifacts[query.Artifact];
                if (artifact.Kind != "image") throw new NotSupportedException("Only validated native PNG images are returned as binary tool content.");
                var path = JobProtocol.ChildPath(job.OutputPath, IOPath.GetRelativePath(job.OutputPath, artifact.Path));
                if (new FileInfo(path).Length > 4 * 1024 * 1024 || JobProtocol.Hash(path) != artifact.SHA256) throw new InvalidDataException("Artifact changed or exceeds the four-MiB transport budget.");
                var snapshot = job.Snapshot ?? throw new InvalidDataException("Artifact has no input provenance.");
                return OperationWire.Element(new ArtifactContent("image/png", Convert.ToBase64String(File.ReadAllBytes(path)), artifact.SHA256, job.ID, snapshot.ID, snapshot.SHA256, snapshot.SceneRevision, snapshot.CodeRevision, job.IsStale, job.Status));
            }
            default: throw new MissingMethodException("Unknown operation; inspect the advertised capability catalog.");
        }
    }
    private static SessionTransformEdit Position(SceneChange item, SessionScene scene)
    {
        var node = scene.Nodes.SingleOrDefault(n => n.ID == item.NodeID) ?? throw new ArgumentException("The scene object no longer exists.");
        var transform = node.Transform ?? throw new NotSupportedException("This object has no Entity transform."); transform.Origin = new(item.X, item.Y);
        return new(item.NodeID, item.ExpectedRevision, transform);
    }
    private static void Boundary(string text, int index)
    {
        if (index < 0 || index > text.Length || index > 0 && index < text.Length && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]))
            throw new ArgumentOutOfRangeException(nameof(index), "Source offsets are UTF-16 boundaries and cannot split a surrogate pair.");
    }
    private static DocumentSummary Summary(SessionScene scene) => new(scene.ID, scene.Path, scene.Revision, scene.IsDirty, scene.HasExternalChange);
    private static DocumentSummary Summary(SessionCode code) => new(code.ID, code.Path, code.Revision, code.IsDirty, code.HasExternalChange);
    private static SessionJob Find(SessionClient client, Guid id) => client.InspectJobs().SingleOrDefault(j => j.ID == id) ?? throw new ArgumentException("The owned job is absent or its generated result was removed.");
    private static JobDataPage Page<T>(JobDataQuery query, IReadOnlyList<T> data)
    {
        if (query.Offset > data.Count) throw new ArgumentOutOfRangeException(nameof(query));
        return new(query.Kind, query.Offset, data.Count, OperationWire.Element(data.Skip(query.Offset).Take(query.Limit).ToArray()));
    }
    private JobInspection Inspect(SessionJob job)
    {
        var result = job.Completion.IsCompleted ? job.Completion.GetAwaiter().GetResult() : null;
        return new(job.ID, job.SnapshotID, job.Status, job.Stage, job.IsStale, job.Snapshot is { } snapshot ? new(snapshot.ID, snapshot.SHA256, snapshot.SceneRevision, snapshot.CodeRevision, snapshot.Files.Count) : null, result?.ExitCode,
            result?.Artifacts.Select((a, i) => new OperationArtifact(i, a.Kind, a.Step, IOPath.GetRelativePath(_project.Root, a.Path), a.SHA256)).ToArray() ?? [],
            result?.Observations.Take(16).ToArray() ?? [], result?.Assertions.Take(16).ToArray() ?? [], result?.Diagnostics ?? [], result?.Observations.Count ?? 0, result?.Assertions.Count ?? 0);
    }
}
